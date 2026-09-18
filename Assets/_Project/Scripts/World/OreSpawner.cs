using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrapRush.World
{
    public sealed class OreSpawner : MonoBehaviour
    {
        public readonly struct SectorSupplyMetrics
        {
            public readonly SectorResourceProfile Profile;
            public readonly int Spawned;
            public readonly int Destroyed;
            public readonly int Current;
            public readonly int Target;
            public readonly int SpawnCap;
            public readonly float EmptyTime;

            public SectorSupplyMetrics(SectorResourceProfile profile, int spawned, int destroyed,
                int current, int target, int spawnCap, float emptyTime)
            {
                Profile = profile;
                Spawned = spawned;
                Destroyed = destroyed;
                Current = current;
                Target = target;
                SpawnCap = spawnCap;
                EmptyTime = emptyTime;
            }
        }

        private sealed class SectorState
        {
            public OreSpawnSettings.ResourceProfile Profile;
            public Transform Root;
            public Rect Bounds;
            public System.Random Random;
            public readonly List<Vector2> Anchors = new List<Vector2>();
            public int Spawned;
            public int Destroyed;
            public int Current;
            public float EmptyTime;
            public float RefillElapsed;
        }

        private readonly List<OreNode> nodes = new List<OreNode>();
        private readonly Dictionary<OreNode, int> nodeSectors = new Dictionary<OreNode, int>();
        private readonly SectorState[] sectors = new SectorState[SectorWorld.GridSize * SectorWorld.GridSize];
        private SectorWorld world;
        private OreNode prefab;
        private OreSpawnSettings settings;
        public IReadOnlyList<OreNode> Nodes => nodes;
        public int SpawnedCount { get; private set; }
        public int BrokenCount { get; private set; }
        public int SkippedCount { get; private set; }
        public float EmptyTime { get; private set; }
        public event Action<OreBreakInfo> OreBroken;

        public void Initialize(SectorWorld sectorWorld, OreNode orePrefab, OreSpawnSettings configuration)
        {
            if (sectorWorld == null || orePrefab == null || configuration == null ||
                configuration.ores == null || configuration.ores.Length == 0 ||
                configuration.profiles == null || configuration.profiles.Length < 3 ||
                configuration.sectorProfiles == null || configuration.sectorProfiles.Length != sectors.Length ||
                configuration.minClusters < 1 || configuration.maxClusters < configuration.minClusters ||
                configuration.minOresPerCluster < 1 || configuration.maxOresPerCluster < configuration.minOresPerCluster ||
                configuration.clusterRadius <= 0 || configuration.placementAttempts < 1)
                throw new ArgumentException("Invalid ore spawn settings.");
            int totalWeight = 0;
            foreach (var entry in configuration.ores)
            {
                if (entry.definition == null || entry.definition.sprite == null || entry.weight < 0 ||
                    entry.definition.maxHealth < 1 || entry.definition.collisionRadius <= 0)
                    throw new ArgumentException("Every ore entry needs a valid definition, sprite and weight.");
                totalWeight += entry.weight;
            }
            if (totalWeight <= 0) throw new ArgumentException("Ore spawn weights must have a positive total.");
            var profileKinds = new HashSet<SectorResourceProfile>();
            foreach (var profile in configuration.profiles)
            {
                if (profile.targetOreCount < 1 || profile.spawnCap < profile.targetOreCount ||
                    profile.refillInterval <= 0f || profile.highValueWeightMultiplier < 1 ||
                    !profileKinds.Add(profile.kind))
                    throw new ArgumentException("Ore resource profiles must be unique and have valid target, cap and refill values.");
            }
            foreach (SectorResourceProfile required in Enum.GetValues(typeof(SectorResourceProfile)))
                if (!profileKinds.Contains(required)) throw new ArgumentException("STABLE, DENSE and HIGH_VALUE profiles are required.");

            world = sectorWorld;
            prefab = orePrefab;
            settings = configuration;
            SpawnAll(totalWeight);
        }

        private void Update()
        {
            if (!Application.isFocused || Time.timeScale == 0f) return;
            Tick(Time.deltaTime);
        }

        public void ResetStage()
        {
            if (world == null || prefab == null || settings == null)
                throw new InvalidOperationException("OreSpawner must be initialized before a stage reset.");
            int totalWeight = 0;
            foreach (var entry in settings.ores) totalWeight += entry.weight;
            SpawnAll(totalWeight);
        }

        private void SpawnAll(int totalWeight)
        {
            Clear();
            for (int row = 0; row < SectorWorld.GridSize; row++)
            for (int col = 0; col < SectorWorld.GridSize; col++)
            {
                int sectorIndex = row * SectorWorld.GridSize + col;
                var root = new GameObject(SectorWorld.GetSectorName(col, row) + " Ores").transform;
                root.SetParent(transform, false);
                var bounds = new Rect(world.Bounds.xMin + col * world.SectorSize,
                    world.Bounds.yMax - (row + 1) * world.SectorSize, world.SectorSize, world.SectorSize);
                var state = new SectorState
                {
                    Profile = GetProfile(settings.sectorProfiles[sectorIndex]),
                    Root = root,
                    Bounds = bounds,
                    Random = new System.Random(unchecked(settings.seed * 397 ^ sectorIndex * 7919))
                };
                sectors[sectorIndex] = state;
                int initialClusters = state.Random.Next(settings.minClusters, settings.maxClusters + 1);
                for (int cluster = 0; cluster < initialClusters && state.Current < state.Profile.targetOreCount; cluster++)
                    SpawnCluster(sectorIndex, totalWeight, true);
                int guard = settings.placementAttempts;
                while (state.Current < state.Profile.targetOreCount && guard-- > 0)
                    if (SpawnCluster(sectorIndex, totalWeight, true) == 0) break;
            }
        }

        internal void Tick(float deltaTime)
        {
            if (settings == null || deltaTime <= 0f) return;
            if (nodes.Count == 0) EmptyTime += deltaTime;
            int totalWeight = TotalWeight();
            for (int sectorIndex = 0; sectorIndex < sectors.Length; sectorIndex++)
            {
                SectorState state = sectors[sectorIndex];
                if (state == null) continue;
                if (state.Current == 0) state.EmptyTime += deltaTime;
                state.RefillElapsed += deltaTime;
                while (state.RefillElapsed >= state.Profile.refillInterval)
                {
                    state.RefillElapsed -= state.Profile.refillInterval;
                    if (state.Current < state.Profile.targetOreCount)
                        SpawnCluster(sectorIndex, totalWeight, false);
                }
            }
        }

        public SectorSupplyMetrics GetSectorMetrics(int col, int row)
        {
            if (col < 0 || col >= SectorWorld.GridSize || row < 0 || row >= SectorWorld.GridSize)
                throw new ArgumentOutOfRangeException();
            SectorState state = sectors[row * SectorWorld.GridSize + col];
            return new SectorSupplyMetrics(state.Profile.kind, state.Spawned, state.Destroyed,
                state.Current, state.Profile.targetOreCount, state.Profile.spawnCap, state.EmptyTime);
        }

        private int SpawnCluster(int sectorIndex, int totalWeight, bool initial)
        {
            SectorState state = sectors[sectorIndex];
            int remaining = state.Profile.spawnCap - state.Current;
            if (remaining <= 0) return 0;
            Vector2 center;
            if (!initial && state.Anchors.Count > 0)
                center = state.Anchors[state.Random.Next(state.Anchors.Count)];
            else
                center = CreateAnchor(state);
            int requested = Mathf.Min(remaining,
                state.Random.Next(settings.minOresPerCluster, settings.maxOresPerCluster + 1));
            int spawned = SpawnAtAnchor(sectorIndex, center, requested, totalWeight);
            if (spawned == 0 && !initial && settings.allowNewRefillAnchors)
            {
                center = CreateAnchor(state);
                spawned = SpawnAtAnchor(sectorIndex, center, requested, totalWeight);
            }
            return spawned;
        }

        private Vector2 CreateAnchor(SectorState state)
        {
            float inset = Mathf.Min(settings.sectorMargin + settings.clusterRadius, world.SectorSize * 0.45f);
            var center = new Vector2(Range(state.Random, state.Bounds.xMin + inset, state.Bounds.xMax - inset),
                Range(state.Random, state.Bounds.yMin + inset, state.Bounds.yMax - inset));
            state.Anchors.Add(center);
            return center;
        }

        private int SpawnAtAnchor(int sectorIndex, Vector2 center, int requested, int totalWeight)
        {
            SectorState state = sectors[sectorIndex];
            int spawned = 0;
            int weightedTotal = ProfileWeightTotal(settings, state.Profile, totalWeight);
            for (int i = 0; i < requested && state.Current < state.Profile.spawnCap; i++)
            {
                OreDefinition definition = Pick(settings, state.Profile, state.Random.Next(weightedTotal));
                bool placed = false;
                for (int attempt = 0; attempt < settings.placementAttempts; attempt++)
                {
                    float angle = Range(state.Random, 0, Mathf.PI * 2);
                    float radius = Mathf.Sqrt((float)state.Random.NextDouble()) * settings.clusterRadius;
                    Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    float margin = settings.sectorMargin + definition.collisionRadius;
                    if (position.x < state.Bounds.xMin + margin || position.x > state.Bounds.xMax - margin ||
                        position.y < state.Bounds.yMin + margin || position.y > state.Bounds.yMax - margin ||
                        !CanPlace(position, definition.collisionRadius, settings.spacing)) continue;
                    var node = Instantiate(prefab, position, Quaternion.identity, state.Root);
                    node.name = definition.kind.ToString();
                    node.Initialize(definition);
                    node.Broken += HandleBroken;
                    nodes.Add(node);
                    nodeSectors.Add(node, sectorIndex);
                    state.Current++;
                    state.Spawned++;
                    SpawnedCount++;
                    spawned++;
                    placed = true;
                    break;
                }
                if (!placed) SkippedCount++;
            }
            return spawned;
        }

        // P0 has a few hundred ores. A world-owned list avoids scene searches and fixed-buffer overflow.
        public OreNode FindNearest(Vector2 position, float range)
            => FindNearest(position, range, null);

        public OreNode FindNearest(Vector2 position, float range, ISet<OreNode> excluded)
        {
            OreNode nearest = null;
            float best = range * range;
            foreach (var node in nodes)
            {
                if (node == null || !node.IsAlive || (excluded != null && excluded.Contains(node))) continue;
                float distance = ((Vector2)node.transform.position - position).sqrMagnitude;
                if (distance > best || (nearest != null && distance == best)) continue;
                best = distance;
                nearest = node;
            }
            return nearest;
        }

        public void CollectAliveWithin(Vector2 position, float range, List<OreNode> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            if (range <= 0f) return;
            float rangeSquared = range * range;
            foreach (var node in nodes)
            {
                if (node == null || !node.IsAlive) continue;
                if (((Vector2)node.transform.position - position).sqrMagnitude <= rangeSquared)
                    results.Add(node);
            }
        }

        public void Clear()
        {
            foreach (var node in nodes)
                if (node != null) node.Broken -= HandleBroken;
            nodes.Clear();
            nodeSectors.Clear();
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            BrokenCount = 0;
            SpawnedCount = 0;
            SkippedCount = 0;
            EmptyTime = 0f;
            Array.Clear(sectors, 0, sectors.Length);
        }

        private bool CanPlace(Vector2 position, float radius, float spacing)
        {
            foreach (var node in nodes)
            {
                float separation = radius + node.Definition.collisionRadius + spacing;
                if (((Vector2)node.transform.position - position).sqrMagnitude < separation * separation) return false;
            }
            return true;
        }

        private void HandleBroken(OreNode node, OreBreakInfo info)
        {
            node.Broken -= HandleBroken;
            nodes.Remove(node);
            if (nodeSectors.TryGetValue(node, out int sectorIndex))
            {
                sectors[sectorIndex].Current--;
                sectors[sectorIndex].Destroyed++;
                nodeSectors.Remove(node);
            }
            BrokenCount++;
            OreBroken?.Invoke(info);
        }

        private void OnDestroy()
        {
            foreach (var node in nodes)
                if (node != null) node.Broken -= HandleBroken;
        }

        private static float Range(System.Random random, float min, float max) => min + (max - min) * (float)random.NextDouble();
        private int TotalWeight()
        {
            int total = 0;
            foreach (var entry in settings.ores) total += entry.weight;
            return total;
        }

        private OreSpawnSettings.ResourceProfile GetProfile(SectorResourceProfile kind)
        {
            foreach (var profile in settings.profiles)
                if (profile.kind == kind) return profile;
            throw new InvalidOperationException("Missing ore resource profile: " + kind);
        }

        private static int ProfileWeightTotal(OreSpawnSettings settings,
            OreSpawnSettings.ResourceProfile profile, int baseTotal)
        {
            if (profile.kind != SectorResourceProfile.HighValue || profile.highValueWeightMultiplier == 1)
                return baseTotal;
            int highValueWeight = 0;
            foreach (var entry in settings.ores)
                if (entry.definition.kind == OreKind.Gold || entry.definition.kind == OreKind.Core)
                    highValueWeight += entry.weight;
            return baseTotal + highValueWeight * (profile.highValueWeightMultiplier - 1);
        }

        private static OreDefinition Pick(OreSpawnSettings settings, OreSpawnSettings.ResourceProfile profile, int roll)
        {
            foreach (var entry in settings.ores)
            {
                int weight = entry.weight;
                if (profile.kind == SectorResourceProfile.HighValue &&
                    (entry.definition.kind == OreKind.Gold || entry.definition.kind == OreKind.Core))
                    weight *= profile.highValueWeightMultiplier;
                if (roll < weight) return entry.definition;
                roll -= weight;
            }
            throw new InvalidOperationException("Invalid ore weight roll.");
        }
    }
}
