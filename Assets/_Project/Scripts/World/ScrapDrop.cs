using UnityEngine;
namespace ScrapRush.World
{
    public sealed class ScrapDrop : MonoBehaviour
    {
        public OreBreakInfo Origin { get; private set; }
        public bool IsTracking { get; private set; }
        private SpriteRenderer visual;
        private ScrapSettings settings;
        private float age, speed, scale;
        private Vector3 center;

        public void Initialize(OreBreakInfo origin, ScrapSettings configuration)
        {
            Origin = origin;
            settings = configuration;
            var child = new GameObject("Visual");
            child.transform.SetParent(transform, false);
            visual = child.AddComponent<SpriteRenderer>();
            visual.sprite = settings.sprite;
            visual.sortingOrder = 3;
            scale = settings.visualSize / Mathf.Max(settings.sprite.bounds.size.x, settings.sprite.bounds.size.y);
            center = settings.sprite.bounds.center;
            speed = settings.initialSpeed;
            UpdateVisual();
        }

        // Once acquired, keep following even if the player leaves the initial range.
        public bool Advance(float deltaTime, Vector2 target, float contactRadius)
        {
            if (deltaTime <= 0) return false;
            age += deltaTime;
            UpdateVisual();
            if (age < settings.spawnDuration) return false;
            Vector2 position = transform.position;
            if (!IsTracking && (position - target).sqrMagnitude <= settings.absorbRange * settings.absorbRange)
                IsTracking = true;
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
    }
}
