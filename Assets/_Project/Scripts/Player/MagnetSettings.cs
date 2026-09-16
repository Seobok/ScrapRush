using UnityEngine;

namespace ScrapRush.Player
{
    [CreateAssetMenu(menuName = "Scrap Rush/Magnet Settings")]
    public sealed class MagnetSettings : ScriptableObject
    {
        [Header("Magnetic Pull and Cluster")]
        [Min(1f)] public float absorbRangeMultiplier = 1.4f;
        [Min(0.1f)] public float clusterRange = 2.5f;

        [Header("Gravity Engine")]
        [Min(1)] public int gravityRequiredAbsorbs = 20;
        [Min(0.1f)] public float gravityRadius = 5f;
        [Min(0.01f)] public float gravityDuration = 2f;
        [Min(1)] public int gravityDamage = 1;

        [Header("Singularity")]
        [Min(1)] public int singularityRequiredAbsorbs = 12;
        [Min(0.1f)] public float singularityRadius = 8f;
        [Min(0.01f)] public float singularitySecondPulseTime = 1f;
        [Min(1)] public int singularityMassAbsorbs = 15;

        [Header("Magnet feedback")]
        [Min(0.01f)] public float fieldLineWidth = 0.08f;
        public Color fieldColor = new Color(0.75f, 0.35f, 1f, 0.9f);
        public AudioClip fieldSound;
        [Range(0f, 1f)] public float fieldVolume = 0.5f;
    }
}
