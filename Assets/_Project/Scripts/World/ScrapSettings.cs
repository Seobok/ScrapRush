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
    }
}
