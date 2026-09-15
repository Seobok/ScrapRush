using UnityEngine;

namespace ScrapRush.Core
{
    [CreateAssetMenu(menuName = "Scrap Rush/Core Play Settings")]
    public sealed class CorePlaySettings : ScriptableObject
    {
        [Min(0.1f)] public float moveSpeed = 7f;
        [Min(0.1f)] public float playerRadius = 0.45f;
        [Min(1f)] public float cameraSize = 8f;
        [Min(0f)] public float cameraSmoothTime = 0.15f;
        [Range(0, 6)] public int startingElectricCount = 6;
    }
}
