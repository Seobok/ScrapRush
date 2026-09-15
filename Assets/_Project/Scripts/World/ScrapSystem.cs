using System;
using System.Collections.Generic;
using UnityEngine;
namespace ScrapRush.World
{
    public sealed class ScrapSystem : MonoBehaviour
    {
        private readonly List<ScrapDrop> drops = new List<ScrapDrop>();
        private readonly Queue<ScrapDrop> pool = new Queue<ScrapDrop>();
        private readonly Queue<(float time, int value)> income = new Queue<(float, int)>();
        private readonly List<OreBreakInfo> completed = new List<OreBreakInfo>();
        private readonly List<ScrapAbsorbEffect> activeEffects = new List<ScrapAbsorbEffect>();
        private readonly Queue<ScrapAbsorbEffect> effectPool = new Queue<ScrapAbsorbEffect>();
        private OreSpawner ores;
        private Transform player;
        private ScrapSettings settings;
        private Material trailMaterial;
        private float contactRadius, elapsed;
        public IReadOnlyList<ScrapDrop> Drops => drops;
        public long Credits { get; private set; }
        public long RecentCredits { get; private set; }
        public float CreditsPerSecond => RecentCredits / 30f;
        public int GeneratedCount { get; private set; }
        public int AbsorbedCount { get; private set; }
        public int ClearedCount { get; private set; }
        public int PeakActiveCount { get; private set; }
        public int PooledCount => pool.Count;
        public event Action<OreBreakInfo> ScrapGenerated;
        public event Action<OreBreakInfo> Absorbed;

        public void Initialize(OreSpawner spawner, Transform target, ScrapSettings configuration, float radius)
        {
            if (spawner == null || target == null || configuration == null || configuration.sprite == null ||
                configuration.absorbRange <= 0 || configuration.initialSpeed <= 0 || configuration.acceleration <= 0 ||
                configuration.maxSpeed < configuration.initialSpeed || configuration.spawnDuration <= 0 ||
                configuration.visualSize <= 0 || configuration.trailTime <= 0 || configuration.trailWidth <= 0 ||
                configuration.trailMinVertexDistance <= 0 || configuration.absorbContactFrames == null ||
                configuration.absorbContactFrames.Length == 0 || Array.Exists(configuration.absorbContactFrames, frame => frame == null) ||
                configuration.absorbContactFramesPerSecond <= 0 || configuration.absorbContactSize <= 0 ||
                configuration.absorbSound == null || radius <= 0)
                throw new ArgumentException("ScrapSystem requires valid source, player and settings.");
            if (ores != null) ores.OreBroken -= Spawn;
            ClearStage();
            ores = spawner; player = target; settings = configuration; contactRadius = radius;
            CreateTrailMaterial();
            Credits = RecentCredits = 0;
            GeneratedCount = AbsorbedCount = ClearedCount = PeakActiveCount = 0;
            elapsed = 0; income.Clear();
            ores.OreBroken += Spawn;
        }

        private void Spawn(OreBreakInfo info)
        {
            ScrapDrop drop;
            if (pool.Count > 0)
            {
                drop = pool.Dequeue();
                drop.gameObject.SetActive(true);
            }
            else
            {
                var obj = new GameObject();
                obj.transform.SetParent(transform, false);
                drop = obj.AddComponent<ScrapDrop>();
            }

            drop.name = "Scrap " + info.Definition.kind;
            drop.transform.position = info.Position;
            drop.Initialize(info, settings, trailMaterial);
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
            completed.Clear();
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var drop = drops[i];
                if (!drop.Advance(deltaTime, player.position, contactRadius)) continue;
                var info = drop.Origin;
                Vector3 contactPosition = drop.transform.position;
                drops.RemoveAt(i);
                Release(drop);
                PlayContactEffect(contactPosition);
                ScrapRush.Core.SfxPlayer.Play(settings.absorbSound, settings.absorbVolume);
                int value = info.FinalValue;
                Credits += value;
                RecentCredits += value;
                income.Enqueue((elapsed, value));
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
                Release(drop);
            }
            ClearedCount += drops.Count;
            drops.Clear();

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                activeEffects.RemoveAt(i);
                effect.Deactivate();
                effectPool.Enqueue(effect);
            }
        }

        private void OnDestroy()
        {
            if (ores != null) ores.OreBroken -= Spawn;
            if (trailMaterial != null) Destroy(trailMaterial);
        }

        private void Release(ScrapDrop drop)
        {
            drop.Release();
            pool.Enqueue(drop);
        }

        private void CreateTrailMaterial()
        {
            if (trailMaterial != null) Destroy(trailMaterial);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                throw new InvalidOperationException("A transparent shader is required for Scrap trails.");
            trailMaterial = new Material(shader) { name = "Scrap Trail (Runtime)" };
        }

        private void PlayContactEffect(Vector3 position)
        {
            ScrapAbsorbEffect effect;
            if (effectPool.Count > 0)
            {
                effect = effectPool.Dequeue();
                effect.gameObject.SetActive(true);
            }
            else
            {
                var obj = new GameObject("Scrap Absorb Contact");
                obj.transform.SetParent(transform, false);
                effect = obj.AddComponent<ScrapAbsorbEffect>();
            }

            effect.transform.position = position;
            activeEffects.Add(effect);
            effect.Play(settings.absorbContactFrames, settings.absorbContactFramesPerSecond,
                settings.absorbContactSize, ReleaseContactEffect);
        }

        private void ReleaseContactEffect(ScrapAbsorbEffect effect)
        {
            activeEffects.Remove(effect);
            effect.Deactivate();
            effectPool.Enqueue(effect);
        }
    }
}
