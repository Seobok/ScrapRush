using UnityEngine;
using UnityEngine.Rendering;

namespace ScrapRush.World
{
    public sealed class ScrapDrop : MonoBehaviour
    {
        public OreBreakInfo Origin { get; private set; }
        public bool IsTracking { get; private set; }
        public ScrapAcquireCause AcquireCause { get; private set; }
        public int AcquireRootEffectId { get; private set; }
        private SpriteRenderer visual;
        private TrailRenderer trail;
        private ScrapSettings settings;
        private float age, speed, scale;
        private Vector3 center;

        public void Initialize(OreBreakInfo origin, ScrapSettings configuration, Material trailMaterial)
        {
            Origin = origin;
            settings = configuration;
            age = 0f;
            speed = settings.initialSpeed;
            IsTracking = false;
            AcquireCause = ScrapAcquireCause.Natural;
            AcquireRootEffectId = 0;

            if (visual == null)
            {
                var child = new GameObject("Visual");
                child.transform.SetParent(transform, false);
                visual = child.AddComponent<SpriteRenderer>();
                visual.sortingOrder = 3;
            }

            visual.sprite = settings.sprite;
            visual.color = Color.white;
            scale = settings.visualSize / Mathf.Max(settings.sprite.bounds.size.x, settings.sprite.bounds.size.y);
            center = settings.sprite.bounds.center;

            if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();
            ConfigureTrail(trailMaterial);
            UpdateVisual();
        }

        // Once acquired, keep following even if the player leaves the initial range.
        public bool Advance(float deltaTime, Vector2 target, float contactRadius, float acquireRange,
            out bool beganTracking)
        {
            beganTracking = false;
            if (deltaTime <= 0) return false;
            age += deltaTime;
            UpdateVisual();
            if (age < settings.spawnDuration) return false;
            Vector2 position = transform.position;
            if (!IsTracking && (position - target).sqrMagnitude <= acquireRange * acquireRange)
            {
                BeginTracking(ScrapAcquireCause.Natural, 0);
                beganTracking = true;
            }
            if (!IsTracking) return false;
            speed = Mathf.Min(settings.maxSpeed, speed + settings.acceleration * deltaTime);
            position = Vector2.MoveTowards(position, target, speed * deltaTime);
            transform.position = position;
            visual.color = new Color(0.65f, 1f, 1f);
            return (position - target).sqrMagnitude <= contactRadius * contactRadius;
        }

        private void UpdateVisual()
        {
            float pop = 1f + Mathf.Sin(Mathf.Clamp01(age / settings.spawnDuration) * Mathf.PI) * 0.4f;
            visual.transform.localScale = Vector3.one * (scale * pop);
            visual.transform.localPosition = -center * (scale * pop);
        }

        public void Release()
        {
            IsTracking = false;
            AcquireCause = ScrapAcquireCause.Natural;
            AcquireRootEffectId = 0;
            trail.emitting = false;
            trail.Clear();
            trail.enabled = false;
            gameObject.SetActive(false);
        }

        public bool ForceAcquire(ScrapAcquireCause cause, int rootEffectId)
        {
            bool beganTracking = !IsTracking;
            if (beganTracking || cause == ScrapAcquireCause.GravityField)
            {
                AcquireCause = cause;
                AcquireRootEffectId = rootEffectId;
            }
            if (beganTracking) BeginTracking(cause, rootEffectId);
            return beganTracking;
        }

        private void BeginTracking(ScrapAcquireCause cause, int rootEffectId)
        {
            IsTracking = true;
            AcquireCause = cause;
            AcquireRootEffectId = rootEffectId;
            trail.Clear();
            trail.enabled = true;
            trail.emitting = true;
        }

        private void ConfigureTrail(Material trailMaterial)
        {
            trail.sharedMaterial = trailMaterial;
            trail.time = settings.trailTime;
            trail.startWidth = settings.trailWidth;
            trail.endWidth = 0f;
            trail.minVertexDistance = settings.trailMinVertexDistance;
            trail.startColor = settings.trailStartColor;
            trail.endColor = settings.trailEndColor;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.numCornerVertices = 0;
            trail.numCapVertices = 0;
            trail.generateLightingData = false;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.lightProbeUsage = LightProbeUsage.Off;
            trail.reflectionProbeUsage = ReflectionProbeUsage.Off;
            trail.sortingOrder = 2;
            trail.autodestruct = false;
            trail.emitting = false;
            trail.Clear();
            trail.enabled = false;
        }
    }
}
