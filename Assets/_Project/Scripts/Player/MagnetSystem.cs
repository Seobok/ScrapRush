using System;
using System.Collections.Generic;
using ScrapRush.World;
using UnityEngine;

namespace ScrapRush.Player
{
    public readonly struct GravityPulseInfo
    {
        public readonly int FieldId;
        public readonly int PulseIndex;
        public readonly int HitCount;
        public readonly int BrokenCount;

        public GravityPulseInfo(int fieldId, int pulseIndex, int hitCount, int brokenCount)
        {
            FieldId = fieldId;
            PulseIndex = pulseIndex;
            HitCount = hitCount;
            BrokenCount = brokenCount;
        }
    }

    public sealed class MagnetSystem : MonoBehaviour
    {
        private readonly List<OreNode> pulseTargets = new List<OreNode>();
        private ScrapSystem scraps;
        private OreSpawner ores;
        private Transform player;
        private MagnetSettings settings;
        private MagnetFieldEffect fieldEffect;
        private int traitCount;
        private int pendingAbsorbs;
        private int nextFieldId;
        private int activeFieldId;
        private int activeFieldTier;
        private float activeFieldRadius;
        private float fieldElapsed;
        private int fieldAbsorbedCount;
        private int fieldPulseIndex;
        private bool secondPulseDone;
        private bool bonusPulseDone;

        public int TraitCount => traitCount;
        public int ActiveTier => traitCount >= 8 ? 8 : traitCount >= 6 ? 6 : traitCount >= 4 ? 4 : traitCount >= 2 ? 2 : 0;
        public int PendingAbsorbs => pendingAbsorbs;
        public int RequiredAbsorbs => ActiveTier >= 8 ? settings.singularityRequiredAbsorbs : settings.gravityRequiredAbsorbs;
        public bool IsFieldActive => activeFieldId != 0;
        public int ActiveFieldId => activeFieldId;
        public float FieldRemaining => IsFieldActive ? Mathf.Max(0f, settings.gravityDuration - fieldElapsed) : 0f;
        public int FieldAbsorbedCount => fieldAbsorbedCount;
        public int TotalAbsorbedCount { get; private set; }
        public int ClusterTriggerCount { get; private set; }
        public int ClusterAcquiredCount { get; private set; }
        public int FieldCount { get; private set; }
        public int PulseCount { get; private set; }
        public int PulseHitCount { get; private set; }
        public int PulseBrokenCount { get; private set; }
        public event Action<int> FieldStarted;
        public event Action<int> FieldEnded;
        public event Action<GravityPulseInfo> Pulsed;

        public void Initialize(ScrapSystem scrapSystem, OreSpawner spawner, Transform target,
            MagnetSettings configuration, int startingTraitCount)
        {
            if (scrapSystem == null || spawner == null || target == null || configuration == null ||
                configuration.absorbRangeMultiplier < 1f || configuration.clusterRange <= 0f ||
                configuration.gravityRequiredAbsorbs < 1 || configuration.gravityRadius <= 0f ||
                configuration.gravityDuration <= 0f || configuration.gravityDamage < 1 ||
                configuration.singularityRequiredAbsorbs < 1 || configuration.singularityRadius <= 0f ||
                configuration.singularitySecondPulseTime <= 0f ||
                configuration.singularitySecondPulseTime >= configuration.gravityDuration ||
                configuration.singularityMassAbsorbs < 1 || configuration.fieldLineWidth <= 0f)
                throw new ArgumentException("MagnetSystem requires valid sources and settings.");

            Unsubscribe();
            scraps = scrapSystem;
            ores = spawner;
            player = target;
            settings = configuration;
            scraps.AbsorbedDetailed += OnAbsorbed;
            scraps.AcquisitionStarted += OnAcquisitionStarted;
            scraps.DropGenerated += OnDropGenerated;
            scraps.StageCleared += OnStageCleared;
            traitCount = 0;
            pendingAbsorbs = nextFieldId = activeFieldId = 0;
            TotalAbsorbedCount = ClusterTriggerCount = ClusterAcquiredCount = 0;
            FieldCount = PulseCount = PulseHitCount = PulseBrokenCount = 0;
            SetTraitCount(startingTraitCount);
        }

        public void SetTraitCount(int value)
        {
            traitCount = Mathf.Clamp(value, 0, 8);
            scraps?.SetAbsorbRangeMultiplier(ActiveTier >= 2 ? settings.absorbRangeMultiplier : 1f);
            if (ActiveTier == 0)
            {
                pendingAbsorbs = 0;
                CancelField();
                return;
            }
            TryStartField();
        }

        private void LateUpdate()
        {
            if (scraps == null || !Application.isFocused || Time.timeScale == 0f) return;
            Tick(Time.deltaTime);
        }

        internal void Tick(float deltaTime)
        {
            if (!IsFieldActive || deltaTime <= 0f) return;
            fieldElapsed += deltaTime;
            if (activeFieldTier >= 8 && !secondPulseDone && fieldElapsed >= settings.singularitySecondPulseTime)
            {
                secondPulseDone = true;
                ExecutePulse();
            }
            if (fieldElapsed >= settings.gravityDuration) EndField();
        }

        private void OnAcquisitionStarted(ScrapAcquisitionInfo info)
        {
            if (ActiveTier < 4 || info.Cause != ScrapAcquireCause.Natural) return;
            int acquired = scraps.ForceAcquireWithin(info.Position, settings.clusterRange,
                ScrapAcquireCause.MagneticCluster, 0);
            ClusterTriggerCount++;
            ClusterAcquiredCount += acquired;
        }

        private void OnAbsorbed(ScrapAbsorbInfo info)
        {
            if (ActiveTier == 0) return;
            TotalAbsorbedCount++;
            pendingAbsorbs++;
            if (IsFieldActive && activeFieldTier >= 8 && info.AcquireCause == ScrapAcquireCause.GravityField &&
                info.AcquireRootEffectId == activeFieldId)
            {
                fieldAbsorbedCount++;
                if (!bonusPulseDone && fieldAbsorbedCount >= settings.singularityMassAbsorbs)
                {
                    bonusPulseDone = true;
                    ExecutePulse();
                }
            }
            TryStartField();
        }

        private void OnDropGenerated(ScrapDrop drop)
        {
            if (!IsFieldActive || drop == null) return;
            if (((Vector2)drop.transform.position - (Vector2)player.position).sqrMagnitude <=
                activeFieldRadius * activeFieldRadius)
                scraps.ForceAcquire(drop, ScrapAcquireCause.GravityField, activeFieldId);
        }

        private void OnStageCleared()
        {
            pendingAbsorbs = 0;
            CancelField();
        }

        private void TryStartField()
        {
            if (IsFieldActive || ActiveTier < 6) return;
            int required = RequiredAbsorbs;
            if (pendingAbsorbs < required) return;
            pendingAbsorbs -= required;
            activeFieldId = ++nextFieldId;
            activeFieldTier = ActiveTier;
            activeFieldRadius = activeFieldTier >= 8 ? settings.singularityRadius : settings.gravityRadius;
            fieldElapsed = 0f;
            fieldAbsorbedCount = 0;
            fieldPulseIndex = 0;
            secondPulseDone = activeFieldTier < 8;
            bonusPulseDone = activeFieldTier < 8;
            FieldCount++;
            scraps.ForceAcquireWithin(player.position, activeFieldRadius,
                ScrapAcquireCause.GravityField, activeFieldId);
            fieldEffect = MagnetFieldEffect.Play(player, activeFieldRadius, settings.gravityDuration,
                settings.fieldLineWidth, settings.fieldColor, gameObject.scene);
            ScrapRush.Core.SfxPlayer.Play(settings.fieldSound, settings.fieldVolume);
            FieldStarted?.Invoke(activeFieldId);
            ExecutePulse();
        }

        private void ExecutePulse()
        {
            if (!IsFieldActive) return;
            ores.CollectAliveWithin(player.position, activeFieldRadius, pulseTargets);
            int hitCount = 0;
            int brokenCount = 0;
            int pulseIndex = fieldPulseIndex++;
            for (int i = 0; i < pulseTargets.Count; i++)
            {
                OreNode target = pulseTargets[i];
                bool wasAlive = target != null && target.IsAlive;
                if (!wasAlive || !target.ApplyDamage(settings.gravityDamage,
                    new MiningDamageContext(MiningSource.Gravity, activeFieldId, pulseIndex))) continue;
                hitCount++;
                if (!target.IsAlive) brokenCount++;
            }
            PulseCount++;
            PulseHitCount += hitCount;
            PulseBrokenCount += brokenCount;
            Pulsed?.Invoke(new GravityPulseInfo(activeFieldId, pulseIndex, hitCount, brokenCount));
        }

        private void EndField()
        {
            int endedFieldId = activeFieldId;
            activeFieldId = 0;
            activeFieldTier = 0;
            activeFieldRadius = 0f;
            fieldEffect = null;
            FieldEnded?.Invoke(endedFieldId);
            TryStartField();
        }

        private void CancelField()
        {
            if (fieldEffect != null) fieldEffect.Stop();
            fieldEffect = null;
            activeFieldId = 0;
            activeFieldTier = 0;
            activeFieldRadius = 0f;
            fieldElapsed = 0f;
        }

        private void Unsubscribe()
        {
            if (scraps == null) return;
            scraps.AbsorbedDetailed -= OnAbsorbed;
            scraps.AcquisitionStarted -= OnAcquisitionStarted;
            scraps.DropGenerated -= OnDropGenerated;
            scraps.StageCleared -= OnStageCleared;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnDrawGizmosSelected()
        {
            if (settings == null) return;
            Gizmos.color = settings.fieldColor;
            Gizmos.DrawWireSphere(transform.position,
                ActiveTier >= 8 ? settings.singularityRadius : settings.gravityRadius);
        }
    }
}
