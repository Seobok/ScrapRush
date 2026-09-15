using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrapRush.World
{
    public sealed class OreSpawner : MonoBehaviour
    {
        private readonly List<OreNode> nodes = new List<OreNode>();
        public IReadOnlyList<OreNode> Nodes => nodes;
        public int BrokenCount { get; private set; }
        public int SkippedCount { get; private set; }
        public event Action<OreBreakInfo> OreBroken;

        public void Initialize(SectorWorld world, OreNode prefab, OreSpawnSettings settings)
        {
            Clear();
            if (settings.ores == null || settings.ores.Length == 0 ||
                settings.minClusters < 1 || settings.maxClusters < settings.minClusters ||
                settings.minOresPerCluster < 1 || settings.maxOresPerCluster < settings.minOresPerCluster ||
                settings.clusterRadius <= 0 || settings.placementAttempts < 1)
                throw new ArgumentException("Invalid ore spawn settings.");
            int totalWeight = 0;
            foreach (var entry in settings.ores)
            {
                if (entry.definition == null || entry.definition.sprite == null || entry.weight < 0 ||
                    entry.definition.maxHealth < 1 || entry.definition.collisionRadius <= 0)
                    throw new ArgumentException("Every ore entry needs a valid definition, sprite and weight.");
                totalWeight += entry.weight;
            }
            if (totalWeight <= 0) throw new ArgumentException("Ore spawn weights must have a positive total.");

            var random = new System.Random(settings.seed);
            for (int row = 0; row < SectorWorld.GridSize; row++)
            for (int col = 0; col < SectorWorld.GridSize; col++)
            {
                var root = new GameObject(SectorWorld.GetSectorName(col, row) + " Ores").transform;
                root.SetParent(transform, false);
                var bounds = new Rect(world.Bounds.xMin + col * world.SectorSize,
                    world.Bounds.yMax - (row + 1) * world.SectorSize, world.SectorSize, world.SectorSize);
                int clusters = random.Next(settings.minClusters, settings.maxClusters + 1);
                for (int cluster = 0; cluster < clusters; cluster++)
                {
                    float inset = Mathf.Min(settings.sectorMargin + settings.clusterRadius, world.SectorSize * 0.45f);
                    Vector2 center = new Vector2(Range(random, bounds.xMin + inset, bounds.xMax - inset),
                        Range(random, bounds.yMin + inset, bounds.yMax - inset));
                    int count = random.Next(settings.minOresPerCluster, settings.maxOresPerCluster + 1);
                    for (int i = 0; i < count; i++)
                    {
                        OreDefinition definition = Pick(settings, random.Next(totalWeight));
                        bool placed = false;
                        for (int attempt = 0; attempt < settings.placementAttempts; attempt++)
                        {
                            float angle = Range(random, 0, Mathf.PI * 2);
                            float radius = Mathf.Sqrt((float)random.NextDouble()) * settings.clusterRadius;
                            Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                            float margin = settings.sectorMargin + definition.collisionRadius;
                            if (position.x < bounds.xMin + margin || position.x > bounds.xMax - margin ||
                                position.y < bounds.yMin + margin || position.y > bounds.yMax - margin ||
                                !CanPlace(position, definition.collisionRadius, settings.spacing)) continue;
                            var node = Instantiate(prefab, position, Quaternion.identity, root);
                            node.name = definition.kind.ToString();
                            node.Initialize(definition);
                            node.Broken += HandleBroken;
                            nodes.Add(node);
                            placed = true;
                            break;
                        }
                        if (!placed) SkippedCount++;
                    }
                }
            }
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

        public void Clear()
        {
            foreach (var node in nodes)
                if (node != null) node.Broken -= HandleBroken;
            nodes.Clear();
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            BrokenCount = 0;
            SkippedCount = 0;
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
            BrokenCount++;
            OreBroken?.Invoke(info);
        }

        private void OnDestroy()
        {
            foreach (var node in nodes)
                if (node != null) node.Broken -= HandleBroken;
        }

        private static float Range(System.Random random, float min, float max) => min + (max - min) * (float)random.NextDouble();
        private static OreDefinition Pick(OreSpawnSettings settings, int roll)
        {
            foreach (var entry in settings.ores)
            {
                if (roll < entry.weight) return entry.definition;
                roll -= entry.weight;
            }
            throw new InvalidOperationException("Invalid ore weight roll.");
        }
    }
}
