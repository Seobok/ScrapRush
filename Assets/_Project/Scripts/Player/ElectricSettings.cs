using UnityEngine;

namespace ScrapRush.Player
{
    [CreateAssetMenu(menuName = "Scrap Rush/Electric Settings")]
    public sealed class ElectricSettings : ScriptableObject
    {
        [Min(0.01f)] public float cooldown = 6f;
        [Min(0.1f)] public float targetRange = 6f;
        [Min(1)] public int damage = 1;
        [Min(0.1f)] public float chainRange = 3f;
        [Range(1, 16)] public int maxChainTargets = 4;
        [Range(0, 16)] public int stormExtraDischarges = 4;
        [Min(0.01f)] public float stormInterval = 0.5f;
        public bool enableElectricValueBonus = true;
        [Min(0f)] public float electricValueBonus = 0.25f;
        [Header("Electric feedback")]
        public AudioClip dischargeSound;
        [Range(0f, 1f)] public float dischargeVolume = 0.45f;
        [Min(0.01f)] public float arcDuration = 0.12f;
        [Min(0.005f)] public float arcWidth = 0.07f;
        public Color arcColor = new Color(0.35f, 0.95f, 1f, 1f);
    }
}
