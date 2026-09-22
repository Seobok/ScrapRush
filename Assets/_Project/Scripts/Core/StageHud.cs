using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapRush.Core
{
    public sealed class StageHud : MonoBehaviour
    {
        private const float WarningTime = 10f;
        private const float CriticalTime = 5f;

        private static readonly Color WarningColor = new(0.95f, 0.24f, 0.14f, 1f);
        private static readonly Color SecuredColor = new(0.25f, 0.9f, 0.52f, 1f);

        [SerializeField] private TMP_Text stageValue = null;
        [SerializeField] private TMP_Text timerText = null;
        [SerializeField] private TMP_Text currentCreditText = null;
        [SerializeField] private TMP_Text requiredText = null;
        [SerializeField] private Image timerFill = null;

        private StageController stage;
        private Color timerColor;
        private Color requiredColor;
        private Vector3 timerScale = Vector3.one;
        private GUIStyle titleStyle;
        private GUIStyle centerStyle;

        public void Initialize(StageController controller)
        {
            stage = controller;
            ResolveReferences();

            if (timerText != null)
            {
                timerColor = timerText.color;
                timerScale = timerText.rectTransform.localScale;
            }

            if (requiredText != null) requiredColor = requiredText.color;
            if (timerFill != null)
            {
                timerFill.type = Image.Type.Filled;
                timerFill.fillMethod = Image.FillMethod.Horizontal;
                timerFill.fillOrigin = 0;
            }

            Refresh();
        }

        private void Update()
        {
            if (stage == null) return;
            Refresh();
        }

        private void Refresh()
        {
            if (stageValue == null || timerText == null || currentCreditText == null ||
                requiredText == null || timerFill == null)
                return;

            stageValue.text = stage.StageNumber == 8
                ? "OVERLOAD"
                : stage.StageNumber.ToString(CultureInfo.InvariantCulture);

            int totalSeconds = Mathf.CeilToInt(stage.RemainingTime);
            timerText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            currentCreditText.text = FormatCredits(stage.CurrentCredits, stage.StageNumber == 8);

            bool secured = stage.CurrentCredits >= stage.Quota;
            requiredText.text = secured
                ? $"{FormatCredits(stage.Quota, stage.StageNumber == 8)}"
                : stage.RemainingTime <= WarningTime
                    ? $"{FormatCredits(stage.Quota, stage.StageNumber == 8)}"
                    : FormatCredits(stage.Quota, stage.StageNumber == 8);
            requiredText.color = secured ? SecuredColor :
                stage.RemainingTime <= WarningTime ? WarningColor : requiredColor;

            timerFill.fillAmount = stage.Duration <= 0f
                ? 0f
                : Mathf.Clamp01(stage.RemainingTime / stage.Duration);

            bool warning = !secured && stage.State == StageState.Playing && stage.RemainingTime <= WarningTime;
            timerText.color = warning ? WarningColor : timerColor;
            if (!warning)
            {
                timerText.rectTransform.localScale = timerScale;
                return;
            }

            float speed = stage.RemainingTime <= CriticalTime ? 12f : 7f;
            float pulse = 1f + (Mathf.Sin(Time.unscaledTime * speed) * 0.04f + 0.04f);
            timerText.rectTransform.localScale = timerScale * pulse;
        }

        private void ResolveReferences()
        {
            if (stageValue == null) stageValue = FindComponent<TMP_Text>("StageValue");
            if (timerText == null) timerText = FindComponent<TMP_Text>("TimerText");
            if (currentCreditText == null) currentCreditText = FindComponent<TMP_Text>("CurrentCreditText");
            if (requiredText == null) requiredText = FindComponent<TMP_Text>("RequiredText");
            if (timerFill == null) timerFill = FindComponent<Image>("TimerFill");

            // The scene copy predates the prefab child's rename to RequiredText.
            if (requiredText == null)
            {
                Transform creditArea = FindChild("CreditArea");
                if (creditArea != null)
                {
                    TMP_Text[] labels = creditArea.GetComponentsInChildren<TMP_Text>(true);
                    foreach (TMP_Text label in labels)
                    {
                        if (label != currentCreditText && label.name != "SplitText") requiredText = label;
                    }
                }
            }

            if (stageValue == null || timerText == null || currentCreditText == null ||
                requiredText == null || timerFill == null)
                Debug.LogError("HUD_TopPressureBar is missing one or more required UI elements.", this);
        }

        private T FindComponent<T>(string objectName) where T : Component
        {
            Transform child = FindChild(objectName);
            return child == null ? null : child.GetComponent<T>();
        }

        private Transform FindChild(string objectName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == objectName) return child;
            }

            return null;
        }

        private static string FormatCredits(long value, bool compact)
        {
            if (!compact || value < 1000000) return value.ToString("N0", CultureInfo.InvariantCulture);
            if (value >= 1000000000) return $"{value / 1000000000d:0.#}B";
            return $"{value / 1000000d:0.#}M";
        }

        private void OnGUI()
        {
            if (stage == null || stage.State == StageState.Playing) return;
            EnsureStyles();

            string heading = stage.State == StageState.Success ? "STAGE CLEAR" : "RUN FAILED";
            string detail = stage.State == StageState.Success
                ? $"Quota reached with {stage.CurrentCredits:N0} C"
                : $"Needed {Mathf.Max(0, stage.Quota - stage.CurrentCredits):N0} more C";
            const float panelWidth = 440f;
            const float panelHeight = 150f;
            float panelLeft = (Screen.width - panelWidth) * 0.5f;
            float panelTop = (Screen.height - panelHeight) * 0.5f;
            GUI.Box(new Rect(panelLeft, panelTop, panelWidth, panelHeight), GUIContent.none);
            GUI.Label(new Rect(panelLeft, panelTop + 24f, panelWidth, 32f), heading, titleStyle);
            GUI.Label(new Rect(panelLeft, panelTop + 64f, panelWidth, 24f), detail, centerStyle);
            string controls = stage.State == StageState.Success
                ? "Press Enter for next Stage  |  R to restart Run"
                : "Press R to restart Run";
            GUI.Label(new Rect(panelLeft, panelTop + 104f, panelWidth, 24f), controls, centerStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            centerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
        }
    }
}
