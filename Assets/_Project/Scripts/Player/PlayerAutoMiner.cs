using ScrapRush.World;
using UnityEngine;

namespace ScrapRush.Player
{
    public sealed class PlayerAutoMiner : MonoBehaviour
    {
        [SerializeField] private MiningSettings settings;
        private OreSpawner ores;
        private float cooldown;
        public OreNode Target { get; private set; }
        public int TargetChanges { get; private set; }
        public int HitCount { get; private set; }

        public void Initialize(OreSpawner spawner)
        {
            if (settings == null || settings.range <= 0 || settings.interval <= 0 || settings.damage < 1 ||
                settings.hitSound == null)
                throw new System.InvalidOperationException("PlayerAutoMiner requires valid MiningSettings.");
            ores = spawner;
            cooldown = 0;
            TargetChanges = HitCount = 0;
        }

        // PlayerMotor moves in Update; select from the resulting position in LateUpdate.
        private void LateUpdate()
        {
            if (ores == null || !Application.isFocused || Time.timeScale == 0) return;
            Tick(Time.deltaTime);
        }

        internal void Tick(float deltaTime)
        {
            cooldown = Mathf.Max(0, cooldown - deltaTime);
            var nearest = ores.FindNearest(transform.position, settings.range);
            if (nearest != Target)
            {
                if (Target != null) Target.SetSelected(false);
                Target = nearest;
                if (Target != null) Target.SetSelected(true);
                TargetChanges++;
            }
            if (Target == null || cooldown > 0) return;
            Vector3 hitPosition = Target.transform.position;
            float hitSize = Target.Definition.visualSize;
            if (Target.ApplyDamage(settings.damage, MiningSource.Basic))
            {
                ScrapRush.Core.SfxPlayer.Play(settings.hitSound, settings.hitVolume);
                MiningHitEffect.Play(settings, hitPosition, hitSize, gameObject.scene);
                HitCount++;
                // No backlog of attacks while idle, and switching never resets this timer.
                cooldown = settings.interval;
            }
        }

        private void OnDisable()
        {
            if (Target != null) Target.SetSelected(false);
            Target = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (settings == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, settings.range);
            if (Target != null) Gizmos.DrawLine(transform.position, Target.transform.position);
        }
    }
}
