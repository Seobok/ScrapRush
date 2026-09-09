using System;
using UnityEngine;

namespace ScrapRush.World
{
    public enum MiningSource { Basic, Gravity, Electric }

    public readonly struct OreBreakInfo
    {
        public readonly OreDefinition Definition;
        public readonly Vector2 Position;
        public readonly int BaseValue;
        public readonly MiningSource Source;

        public OreBreakInfo(OreDefinition definition, Vector2 position, MiningSource source)
        {
            Definition = definition;
            Position = position;
            BaseValue = definition.baseValue;
            Source = source;
        }
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class OreNode : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        public OreDefinition Definition { get; private set; }
        public int Health { get; private set; }
        public bool IsAlive => isActiveAndEnabled && Health > 0;
        public event Action<OreNode, OreBreakInfo> Broken;
        private CircleCollider2D shape;
        private Vector3 restScale;
        private Vector3 restPosition;
        private float hitTime;
        private bool selected;

        public void Initialize(OreDefinition definition)
        {
            Definition = definition;
            Health = definition.maxHealth;
            shape = GetComponent<CircleCollider2D>();
            shape.radius = definition.collisionRadius;
            shape.enabled = true;
            visual.sprite = definition.sprite;
            float scale = definition.visualSize / Mathf.Max(definition.sprite.bounds.size.x, definition.sprite.bounds.size.y);
            restScale = Vector3.one * scale;
            restPosition = -definition.sprite.bounds.center * scale;
            visual.transform.localScale = restScale;
            visual.transform.localPosition = restPosition;
            visual.color = Color.white;
            selected = false;
            hitTime = 0;
        }

        public void SetSelected(bool value) => selected = value;

        public bool ApplyDamage(int damage, MiningSource source)
        {
            if (!IsAlive || damage <= 0) return false;
            Health = Mathf.Max(0, Health - damage);
            hitTime = 0.13f;
            if (Health > 0) return true;

            // Invalidate before notifying: another effect must never break this ore twice.
            shape.enabled = false;
            var info = new OreBreakInfo(Definition, transform.position, source);
            gameObject.SetActive(false);
            try { Broken?.Invoke(this, info); }
            finally { Destroy(gameObject); }
            return true;
        }

        private void LateUpdate()
        {
            if (Definition == null) return;
            hitTime = Mathf.Max(0, hitTime - Time.deltaTime);
            float hit = hitTime / 0.13f;
            visual.color = hit > 0 ? new Color(1f, 0.55f, 0.3f) : selected ? new Color(0.6f, 1f, 1f) : Color.white;
            visual.transform.localScale = restScale * (1f + hit * 0.12f);
            visual.transform.localPosition = restPosition + Vector3.right * (Mathf.Sin(hit * 22f) * hit * 0.06f);
        }
    }
}
