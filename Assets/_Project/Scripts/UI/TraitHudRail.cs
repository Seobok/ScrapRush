using System.Collections;
using TMPro;
using UnityEngine;

namespace ScrapRush.UI
{
    public sealed class TraitHudRail : MonoBehaviour
    {
        private static readonly int[] Thresholds = { 2, 4, 6, 8 };

        [SerializeField] private TraitHudRow[] rows = new TraitHudRow[7];
        [SerializeField] private CanvasGroup toastGroup;
        [SerializeField] private TMP_Text toastText;

        private Coroutine toastRoutine;

        public void SetTraitState(TraitId traitId, int baseCount, int virtualCount = 0, bool emphasizeChange = false,
            string thresholdName = null)
        {
            TraitHudRow row = FindRow(traitId);
            if (row == null) return;

            int previousCount = row.EffectiveCount;
            row.SetState(baseCount, virtualCount, emphasizeChange);

            int currentCount = row.EffectiveCount;
            if (emphasizeChange && currentCount > previousCount && IsThreshold(currentCount))
                ShowThresholdToast(traitId, currentCount, thresholdName);
        }

        public void ShowThresholdToast(TraitId traitId, int threshold, string thresholdName = null)
        {
            if (toastGroup == null || toastText == null) return;
            if (toastRoutine != null) StopCoroutine(toastRoutine);

            string displayName = GetDisplayName(traitId);
            toastText.text = string.IsNullOrWhiteSpace(thresholdName)
                ? $"{displayName} {threshold}"
                : $"{displayName} {threshold}  ·  {thresholdName}";
            toastRoutine = StartCoroutine(ShowToast());
        }

        private IEnumerator ShowToast()
        {
            toastGroup.gameObject.SetActive(true);
            float elapsed = 0f;
            const float duration = 1.2f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float fadeIn = Mathf.Clamp01(elapsed / 0.12f);
                float fadeOut = Mathf.Clamp01((duration - elapsed) / 0.18f);
                toastGroup.alpha = Mathf.Min(fadeIn, fadeOut);
                yield return null;
            }

            toastGroup.alpha = 0f;
            toastGroup.gameObject.SetActive(false);
            toastRoutine = null;
        }

        private TraitHudRow FindRow(TraitId traitId)
        {
            foreach (TraitHudRow row in rows)
            {
                if (row != null && row.TraitId == traitId) return row;
            }

            return null;
        }

        private static bool IsThreshold(int count)
        {
            foreach (int threshold in Thresholds)
            {
                if (count == threshold) return true;
            }

            return false;
        }

        private static string GetDisplayName(TraitId traitId)
        {
            return traitId switch
            {
                TraitId.Magnet => "MAGNET",
                TraitId.Electric => "ELECTRIC",
                TraitId.Industry => "INDUSTRY",
                TraitId.Recycle => "RECYCLE",
                TraitId.Explosion => "EXPLOSION",
                TraitId.Luck => "LUCK",
                TraitId.Overload => "OVERLOAD",
                _ => traitId.ToString()
            };
        }
    }
}
