using UnityEngine;
namespace ScrapRush.World
{
    [CreateAssetMenu(menuName = "Scrap Rush/Scrap Settings")]
    public sealed class ScrapSettings : ScriptableObject
    {
        public Sprite sprite;
        [Min(0.01f)] public float absorbRange = 1.75f;
        [Min(0.01f)] public float initialSpeed = 2f;
        [Min(0.01f)] public float acceleration = 24f;
        [Min(0.01f)] public float maxSpeed = 18f;
        [Min(0.01f)] public float visualSize = 0.38f;
        [Min(0.01f)] public float spawnDuration = 0.12f;

        [Header("Absorb Trail")]
        [Min(0.01f)] public float trailTime = 0.18f;
        [Min(0.01f)] public float trailWidth = 0.16f;
        [Min(0.01f)] public float trailMinVertexDistance = 0.12f;
        public Color trailStartColor = new Color(0.55f, 1f, 1f, 0.9f);
        public Color trailEndColor = new Color(0.15f, 0.65f, 1f, 0f);

        [Header("Absorb Contact Effect")]
        public Sprite[] absorbContactFrames;
        [Min(1f)] public float absorbContactFramesPerSecond = 24f;
        [Min(0.1f)] public float absorbContactSize = 1.4f;

        [Header("Absorb Sound")]
        public AudioClip absorbSound;
        [Range(0f, 1f)] public float absorbVolume = 0.4f;
    }
}
