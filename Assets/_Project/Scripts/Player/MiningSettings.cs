using UnityEngine;

namespace ScrapRush.Player
{
    [CreateAssetMenu(menuName = "Scrap Rush/Mining Settings")]
    public sealed class MiningSettings : ScriptableObject
    {
        [Min(0.1f)] public float range = 1.4f;
        [Min(0.01f)] public float interval = 0.5f;
        [Min(1)] public int damage = 1;
    }
}
