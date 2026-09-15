using System;
using System.Collections.Generic;
using ScrapRush.World;
using UnityEngine;

namespace ScrapRush.Player
{
    public readonly struct ElectricDischargeInfo
    {
        public readonly int RootEffectId;
        public readonly int DischargeIndex;
        public readonly int HitCount;
        public readonly int BrokenCount;

        public ElectricDischargeInfo(int rootEffectId, int dischargeIndex, int hitCount, int brokenCount)
        {
            RootEffectId = rootEffectId;
            DischargeIndex = dischargeIndex;
            HitCount = hitCount;
            BrokenCount = brokenCount;
        }
    }

    public sealed class ElectricSystem : MonoBehaviour
    {
        private readonly HashSet<OreNode> hitThisDischarge = new HashSet<OreNode>();
        private readonly List<Vector3> arcPoints = new List<Vector3>();
        private OreSpawner ores;
        private Transform player;
        private ElectricSettings settings;
        private int traitCount;
        private int nextRootEffectId;
        private float cooldownRemaining;
        private bool ready;
        private int stormShotsRemaining;
        private float stormShotTimer;
        private bool stormUsesChain;
        private float stormValueModifier;
        private int stormRootEffectId;
        private int stormDischargeIndex;

        public int TraitCount => traitCount;
        public int ActiveTier => traitCount >= 6 ? 6 : traitCount >= 4 ? 4 : traitCount >= 2 ? 2 : 0;
        public float CooldownRemaining => cooldownRemaining;
        public bool IsReady => ready;
        public int StormShotsRemaining => stormShotsRemaining;
        public int StaticTriggerCount { get; private set; }
        public int StormCount { get; private set; }
        public int DischargeCount { get; private set; }
        public int HitCount { get; private set; }
        public int ChainHitCount { get; private set; }
        public int BrokenCount { get; private set; }
        public float ReadyHeldSeconds { get; private set; }
        public event Action<ElectricDischargeInfo> Discharged;
        public event Action<int> StormStarted;

        public void Initialize(OreSpawner spawner, Transform target, ElectricSettings configuration, int startingTraitCount)
        {
            if (spawner == null || target == null || configuration == null || configuration.cooldown <= 0 ||
                configuration.targetRange <= 0 || configuration.damage < 1 || configuration.chainRange <= 0 ||
                configuration.maxChainTargets < 1 || configuration.stormExtraDischarges < 0 ||
                configuration.stormInterval <= 0 || configuration.arcDuration <= 0 || configuration.arcWidth <= 0)
                throw new ArgumentException("ElectricSystem requires valid source, player and settings.");
            ores = spawner;
            player = target;
            settings = configuration;
            traitCount = 0;
            nextRootEffectId = 0;
            StaticTriggerCount = StormCount = DischargeCount = HitCount = ChainHitCount = BrokenCount = 0;
            ReadyHeldSeconds = 0f;
            SetTraitCount(startingTraitCount);
        }

        public void SetTraitCount(int value)
        {
            int previousTier = ActiveTier;
            traitCount = Mathf.Clamp(value, 0, 8);
            int newTier = ActiveTier;
            if (newTier == 0)
            {
                ready = false;
                cooldownRemaining = 0f;
                stormShotsRemaining = 0;
                return;
            }
            if (previousTier == 0)
            {
                ready = false;
                cooldownRemaining = settings.cooldown;
            }
        }

        private void LateUpdate()
        {
            if (ores == null || !Application.isFocused || Time.timeScale == 0) return;
            Tick(Time.deltaTime);
        }

        internal void Tick(float deltaTime)
        {
            if (ActiveTier == 0 || deltaTime <= 0f) return;
            AdvanceStorm(deltaTime);

            float chargingTime = Mathf.Min(cooldownRemaining, deltaTime);
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
            if (cooldownRemaining <= 0f) ready = true;
            if (!ready) return;

            if (!TryStartStaticDischarge())
                ReadyHeldSeconds += Mathf.Max(0f, deltaTime - chargingTime);
        }

        private bool TryStartStaticDischarge()
        {
            OreNode target = ores.FindNearest(player.position, settings.targetRange);
            if (target == null) return false;

            ready = false;
            cooldownRemaining = settings.cooldown;
            StaticTriggerCount++;
            int rootEffectId = ++nextRootEffectId;
            bool usesChain = ActiveTier >= 4;
            float valueModifier = usesChain && settings.enableElectricValueBonus ? settings.electricValueBonus : 0f;

            if (ActiveTier >= 6 && settings.stormExtraDischarges > 0)
            {
                stormShotsRemaining = settings.stormExtraDischarges;
                stormShotTimer = settings.stormInterval;
                stormUsesChain = usesChain;
                stormValueModifier = valueModifier;
                stormRootEffectId = rootEffectId;
                stormDischargeIndex = 1;
                StormCount++;
                StormStarted?.Invoke(rootEffectId);
            }

            ExecuteDischarge(target, usesChain, valueModifier, rootEffectId, 0);
            return true;
        }

        private void AdvanceStorm(float deltaTime)
        {
            if (stormShotsRemaining <= 0) return;
            stormShotTimer -= deltaTime;
            while (stormShotsRemaining > 0 && stormShotTimer <= 0f)
            {
                ExecuteDischarge(null, stormUsesChain, stormValueModifier, stormRootEffectId, stormDischargeIndex++);
                stormShotsRemaining--;
                stormShotTimer += settings.stormInterval;
            }
        }

        private void ExecuteDischarge(OreNode firstTarget, bool usesChain, float valueModifier,
            int rootEffectId, int dischargeIndex)
        {
            OreNode target = firstTarget != null && firstTarget.IsAlive
                ? firstTarget
                : ores.FindNearest(player.position, settings.targetRange);
            if (target == null) return;

            hitThisDischarge.Clear();
            arcPoints.Clear();
            arcPoints.Add(player.position);
            int hitCount = 0;
            int brokenCount = 0;
            int targetLimit = usesChain ? settings.maxChainTargets : 1;

            while (target != null && hitCount < targetLimit)
            {
                Vector3 hitPosition = target.transform.position;
                hitThisDischarge.Add(target);
                arcPoints.Add(hitPosition);
                bool wasAlive = target.IsAlive;
                target.ApplyDamage(settings.damage, new MiningDamageContext(MiningSource.Electric,
                    rootEffectId, dischargeIndex, valueModifier));
                hitCount++;
                if (wasAlive && !target.IsAlive) brokenCount++;
                if (!usesChain || hitCount >= targetLimit) break;
                target = ores.FindNearest(hitPosition, settings.chainRange, hitThisDischarge);
            }

            if (hitCount <= 0) return;
            DischargeCount++;
            HitCount += hitCount;
            ChainHitCount += Mathf.Max(0, hitCount - 1);
            BrokenCount += brokenCount;
            ElectricArcEffect.Play(arcPoints, settings, gameObject.scene);
            ScrapRush.Core.SfxPlayer.Play(settings.dischargeSound, settings.dischargeVolume);
            Discharged?.Invoke(new ElectricDischargeInfo(rootEffectId, dischargeIndex, hitCount, brokenCount));
        }

        private void OnDrawGizmosSelected()
        {
            if (settings == null) return;
            Gizmos.color = settings.arcColor;
            Gizmos.DrawWireSphere(transform.position, settings.targetRange);
        }
    }
}
