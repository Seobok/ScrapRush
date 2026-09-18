using UnityEngine;

namespace ScrapRush.Core
{
    [CreateAssetMenu(menuName = "Scrap Rush/Stage Settings")]
    public sealed class StageSettings : ScriptableObject
    {
        [Min(1f)] public float duration = 30f;
        [Min(1)] public int quota = 300;
        [Min(0)] public int quotaIncreasePerStage = 300;
        [Range(1, 10)] public int capacity = 4;
        [Range(0, 9)] public int capacityIncreasePerStage = 1;
        [Range(1, 10)] public int maximumCapacity = 10;
    }
}
