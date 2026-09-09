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
        [Header("Ore break effect")]
        [SerializeField] private Sprite[] breakFrames;
        [SerializeField, Min(1f)] private float breakFramesPerSecond = 24f;
        [SerializeField, Min(0.1f)] private float breakSizeMultiplier = 2f;
        [Header("Mining target effect")]
        [SerializeField] private Sprite[] targetSequence;
        [SerializeField] private Sprite targetLoop;
        [SerializeField, Min(1f)] private float targetFramesPerSecond = 24f;
        [SerializeField, Min(0.1f)] private float targetSizeMultiplier = 2f;
        private SpriteRenderer targetVisual;
        private float targetTime;
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
            SetSelected(false);
            hitTime = 0;
        }

        public void SetSelected(bool value)
        {
            value &= IsAlive;
            if (selected == value) return;
            selected = value;
            targetTime = 0;
            if (!value)
            {
                if (targetVisual != null) targetVisual.enabled = false;
                return;
            }
            if (targetVisual == null)
            {
                var effect = new GameObject("MiningTarget");
                effect.transform.SetParent(transform, false);
                targetVisual = effect.AddComponent<SpriteRenderer>();
                targetVisual.sharedMaterial = visual.sharedMaterial;
                targetVisual.sortingLayerID = visual.sortingLayerID;
                targetVisual.sortingOrder = visual.sortingOrder + 1;
            }
            UpdateTargetEffect(0);
        }

        private void UpdateTargetEffect(float deltaTime)
        {
            if (!selected || targetVisual == null) return;
            targetTime += deltaTime;
            int frameCount = targetSequence == null ? 0 : targetSequence.Length;
            float sequenceDuration = frameCount / Mathf.Max(1f, targetFramesPerSecond);
            bool acquiring = targetTime < sequenceDuration;
            Sprite sprite = acquiring
                ? targetSequence[Mathf.Min(frameCount - 1, Mathf.FloorToInt(targetTime * Mathf.Max(1f, targetFramesPerSecond)))]
                : targetLoop;
            targetVisual.sprite = sprite;
            targetVisual.enabled = sprite != null;
            if (sprite == null) return;
            float pulse = acquiring ? 0 : (1f - Mathf.Cos((targetTime - sequenceDuration) * Mathf.PI * 2f / 0.9f)) * 0.5f;
            // Use the full canvas so the acquisition frames retain their authored size and center.
            float canvasSize = Mathf.Max(sprite.rect.width, sprite.rect.height) / sprite.pixelsPerUnit;
            float scale = Definition.visualSize * targetSizeMultiplier / canvasSize;
            targetVisual.transform.localScale = Vector3.one * (scale * (1f + pulse * 0.04f));
            targetVisual.color = new Color(1f, 1f, 1f, 1f - pulse * 0.18f);
        }

        private void OnDisable() => SetSelected(false);

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
            ScrapRush.Player.MiningHitEffect.Play(breakFrames, breakFramesPerSecond,
                Definition.visualSize * breakSizeMultiplier, transform.position, gameObject.scene, "OreBreak", 21);
            try { Broken?.Invoke(this, info); }
            finally { Destroy(gameObject); }
            return true;
        }

        private void LateUpdate()
        {
            if (Definition == null) return;
            hitTime = Mathf.Max(0, hitTime - Time.deltaTime);
            float hit = hitTime / 0.13f;
            UpdateTargetEffect(Time.deltaTime);
            visual.color = hit > 0 ? new Color(1f, 0.55f, 0.3f) : Color.white;
            visual.transform.localScale = restScale * (1f + hit * 0.12f);
            visual.transform.localPosition = restPosition + Vector3.right * (Mathf.Sin(hit * 22f) * hit * 0.06f);
        }
    }
}
