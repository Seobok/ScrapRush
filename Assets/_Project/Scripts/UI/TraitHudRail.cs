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

        private RectTransform railRect;
        private UnityEngine.UI.Image frame;
        private Transform header;
        private float verticalPadding = -1f;

        private void Awake()
        {
            foreach (TraitHudRow row in rows)
            {
                if (row != null) row.SetState(0, 0);
            }
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (railRect == null) railRect = GetComponent<RectTransform>();
            if (frame == null) frame = GetComponent<UnityEngine.UI.Image>();
            if (header == null) header = transform.Find("Header");
            if (railRect == null) return;

            if (verticalPadding < 0f)
            {
                // Capture the authored full rail before its height changes with active traits.
                float fullContentHeight = 0f;
                int rowCount = 0;
                foreach (TraitHudRow row in rows)
                {
                    if (row == null) continue;
                    fullContentHeight += ((RectTransform)row.transform).rect.height;
                    if (rowCount++ > 0) fullContentHeight += 2f;
                }
                verticalPadding = Mathf.Max(0f, (railRect.rect.height - fullContentHeight) * 0.5f);
            }
            float padding = verticalPadding;
            const float spacing = 2f;
            float height = padding * 2f;
            int visibleCount = 0;
            foreach (TraitHudRow row in rows)
            {
                if (row == null) continue;
                bool visible = row.IsEffectActive;
                row.gameObject.SetActive(visible);
                if (!visible) continue;
                height += ((RectTransform)row.transform).rect.height;
                if (visibleCount++ > 0) height += spacing;
            }

            bool hasActiveTraits = visibleCount > 0;
            if (frame != null) frame.enabled = hasActiveTraits;
            if (header != null)
            {
                header.gameObject.SetActive(hasActiveTraits);
                RectTransform headerRect = (RectTransform)header;
                headerRect.anchorMin = headerRect.anchorMax = new Vector2(0.5f, 1f);
                headerRect.anchoredPosition = new Vector2(0f, 12f);
            }
            railRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, hasActiveTraits ? height : 0f);

            float offset = padding;
            foreach (TraitHudRow row in rows)
            {
                if (row == null || !row.IsEffectActive) continue;
                RectTransform rect = (RectTransform)row.transform;
                float rowHeight = rect.rect.height;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -offset - rowHeight * 0.5f);
                offset += rowHeight + spacing;
            }
        }
        public void SetTraitState(TraitId traitId, int baseCount, int virtualCount = 0, bool emphasizeChange = false,
            string thresholdName = null)
        {
            TraitHudRow row = FindRow(traitId);
            if (row == null) return;

            int previousCount = row.EffectiveCount;
            row.SetState(baseCount, virtualCount, emphasizeChange);
            RefreshLayout();

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
