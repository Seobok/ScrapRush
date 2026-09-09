using UnityEngine;

namespace ScrapRush.World
{
    public enum OreKind { Iron, Copper, Gold, Core }

    [CreateAssetMenu(menuName = "Scrap Rush/Ore Definition")]
    public sealed class OreDefinition : ScriptableObject
    {
        public OreKind kind;
        [Min(1)] public int maxHealth = 1;
        [Min(0)] public int baseValue = 10;
        public Sprite sprite;
        [Min(0.1f)] public float visualSize = 1.1f;
        [Min(0.05f)] public float collisionRadius = 0.45f;
    }
}
