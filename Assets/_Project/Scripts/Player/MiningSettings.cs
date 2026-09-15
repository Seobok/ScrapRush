using UnityEngine;

namespace ScrapRush.Player
{
    [CreateAssetMenu(menuName = "Scrap Rush/Mining Settings")]
    public sealed class MiningSettings : ScriptableObject
    {
        [Min(0.1f)] public float range = 1.4f;
        [Min(0.01f)] public float interval = 0.5f;
        [Min(1)] public int damage = 1;
        [Header("Mining hit sound")]
        public AudioClip hitSound;
        [Range(0f, 1f)] public float hitVolume = 0.35f;
        [Header("Mining hit effect")]
        public Sprite[] hitFrames;
        [Min(1f)] public float hitFramesPerSecond = 24f;
        [Min(0.1f)] public float hitSizeMultiplier = 1.6f;
    }
}
