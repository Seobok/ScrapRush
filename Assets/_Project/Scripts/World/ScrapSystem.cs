using System;
using System.Collections.Generic;
using UnityEngine;
namespace ScrapRush.World
{
    public sealed class ScrapSystem : MonoBehaviour
    {
        private readonly List<ScrapDrop> drops = new List<ScrapDrop>();
        private readonly Queue<(float time, int value)> income = new Queue<(float, int)>();
        private OreSpawner ores;
        private Transform player;
        private ScrapSettings settings;
        private float contactRadius, elapsed;
        public IReadOnlyList<ScrapDrop> Drops => drops;
        public long Credits { get; private set; }
        public long RecentCredits { get; private set; }
        public float CreditsPerSecond => RecentCredits / 30f;
        public int GeneratedCount { get; private set; }
        public int AbsorbedCount { get; private set; }
        public int ClearedCount { get; private set; }
        public int PeakActiveCount { get; private set; }
        public event Action<OreBreakInfo> ScrapGenerated;
        public event Action<OreBreakInfo> Absorbed;

        public void Initialize(OreSpawner spawner, Transform target, ScrapSettings configuration, float radius)
        {
            if (spawner == null || target == null || configuration == null || configuration.sprite == null ||
                configuration.absorbRange <= 0 || configuration.initialSpeed <= 0 || configuration.acceleration <= 0 ||
                configuration.maxSpeed < configuration.initialSpeed || configuration.spawnDuration <= 0 ||
                configuration.visualSize <= 0 || radius <= 0)
                throw new ArgumentException("ScrapSystem requires valid source, player and settings.");
            if (ores != null) ores.OreBroken -= Spawn;
            ClearStage();
            ores = spawner; player = target; settings = configuration; contactRadius = radius;
            Credits = RecentCredits = 0;
            GeneratedCount = AbsorbedCount = ClearedCount = PeakActiveCount = 0;
            elapsed = 0; income.Clear();
            ores.OreBroken += Spawn;
        }

        private void Spawn(OreBreakInfo info)
        {
            var obj = new GameObject("Scrap " + info.Definition.kind);
            obj.transform.SetParent(transform, false);
            obj.transform.position = info.Position;
            var drop = obj.AddComponent<ScrapDrop>();
            drop.Initialize(info, settings);
            drops.Add(drop);
            GeneratedCount++;
            PeakActiveCount = Mathf.Max(PeakActiveCount, drops.Count);
            ScrapGenerated?.Invoke(info);
        }

        private void LateUpdate()
        {
            if (!Application.isFocused || Time.timeScale == 0) return;
            Tick(Time.deltaTime);
        }

        internal void Tick(float deltaTime)
        {
            if (player == null || deltaTime <= 0) return;
            elapsed += deltaTime;
            // Commit removal and payment before callbacks; callbacks may spawn or clear drops.
            var completed = new List<OreBreakInfo>();
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var drop = drops[i];
                if (!drop.Advance(deltaTime, player.position, contactRadius)) continue;
                var info = drop.Origin;
                drops.RemoveAt(i);
                drop.gameObject.SetActive(false);
                Destroy(drop.gameObject);
                Credits += info.BaseValue;
                RecentCredits += info.BaseValue;
                income.Enqueue((elapsed, info.BaseValue));
                AbsorbedCount++;
                completed.Add(info);
            }
            while (income.Count > 0 && income.Peek().time <= elapsed - 30f)
                RecentCredits -= income.Dequeue().value;
            foreach (var info in completed) Absorbed?.Invoke(info);
        }

        // Stage transitions discard uncollected value, while keeping session C and telemetry.
        public void ClearStage()
        {
            foreach (var drop in drops)
            {
                if (drop == null) continue;
                drop.gameObject.SetActive(false);
                Destroy(drop.gameObject);
            }
            ClearedCount += drops.Count;
            drops.Clear();
        }

        private void OnDestroy()
        {
            if (ores != null) ores.OreBroken -= Spawn;
        }
    }
}
