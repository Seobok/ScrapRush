using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapRush.UI
{
    public enum TraitId
    {
        Magnet,
        Electric,
        Industry,
        Recycle,
        Explosion,
        Luck,
        Overload
    }

    public sealed class TraitHudRow : MonoBehaviour
    {
        private static readonly int[] Thresholds = { 2, 4, 6, 8 };

        [SerializeField] private TraitId traitId;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text traitName;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject virtualCountRoot;
        [SerializeField] private TMP_Text virtualCountText;
        [SerializeField] private Image[] thresholdDots = new Image[4];
        [SerializeField] private Sprite thresholdEmpty;
        [SerializeField] private Sprite thresholdFilled;
        [SerializeField] private Sprite thresholdFocus;
        [SerializeField] private Image rowChangedGlow;
        [SerializeField] private Image thresholdReachedRing;

        private int effectiveCount;
        private float emphasisUntil;

        public TraitId TraitId => traitId;
        public int EffectiveCount => effectiveCount;

        public void SetIdentity(TraitId id, string displayName, Sprite traitIcon)
        {
            traitId = id;
            if (traitName != null) traitName.text = displayName;
            if (icon != null) icon.sprite = traitIcon;
        }

        public void SetState(int baseCount, int virtualCount, bool emphasizeChange = false)
        {
            baseCount = Mathf.Max(0, baseCount);
            virtualCount = Mathf.Max(0, virtualCount);
            effectiveCount = Mathf.Min(8, baseCount + virtualCount);

            if (canvasGroup != null) canvasGroup.alpha = effectiveCount == 0 ? 0.55f : 1f;
            if (countText != null) countText.text = FormatCount(effectiveCount);

            bool showVirtual = virtualCount > 0;
            if (virtualCountRoot != null) virtualCountRoot.SetActive(showVirtual);
            if (virtualCountText != null) virtualCountText.text = $"+{virtualCount}";

            for (int i = 0; i < thresholdDots.Length && i < Thresholds.Length; i++)
            {
                Image dot = thresholdDots[i];
                if (dot == null) continue;

                int threshold = Thresholds[i];
                dot.sprite = effectiveCount >= threshold
                    ? thresholdFilled
                    : threshold == NextThreshold(effectiveCount) ? thresholdFocus : thresholdEmpty;
                dot.color = Color.white;
            }

            if (emphasizeChange)
            {
                emphasisUntil = Time.unscaledTime + 0.45f;
                if (rowChangedGlow != null) rowChangedGlow.gameObject.SetActive(true);
                if (thresholdReachedRing != null)
                    thresholdReachedRing.gameObject.SetActive(IsThreshold(effectiveCount));
            }
            else
            {
                HideEmphasis();
            }
        }

        private void Update()
        {
            if (emphasisUntil > 0f && Time.unscaledTime >= emphasisUntil) HideEmphasis();
        }

        private void HideEmphasis()
        {
            emphasisUntil = 0f;
            if (rowChangedGlow != null) rowChangedGlow.gameObject.SetActive(false);
            if (thresholdReachedRing != null) thresholdReachedRing.gameObject.SetActive(false);
        }

        private static string FormatCount(int count)
        {
            if (count >= 8) return "8 MAX";
            return $"{count}/{NextThreshold(count)}";
        }

        private static int NextThreshold(int count)
        {
            foreach (int threshold in Thresholds)
            {
                if (count < threshold) return threshold;
            }

            return 8;
        }

        private static bool IsThreshold(int count)
        {
            foreach (int threshold in Thresholds)
            {
                if (count == threshold) return true;
            }

            return false;
        }
    }
}
