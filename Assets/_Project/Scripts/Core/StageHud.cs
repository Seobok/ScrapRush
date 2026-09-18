using UnityEngine;

namespace ScrapRush.Core
{
    public sealed class StageHud : MonoBehaviour
    {
        private StageController stage;
        private GUIStyle titleStyle;
        private GUIStyle centerStyle;

        public void Initialize(StageController controller)
        {
            stage = controller;
        }

        private void OnGUI()
        {
            if (stage == null) return;
            EnsureStyles();

            const float width = 420f;
            float left = (Screen.width - width) * 0.5f;
            GUI.Box(new Rect(left, 12f, width, 86f), GUIContent.none);
            GUI.Label(new Rect(left, 18f, width, 24f),
                $"STAGE {stage.StageNumber}    CAPACITY {stage.Capacity}", titleStyle);
            GUI.Label(new Rect(left, 43f, width, 22f),
                $"TIME {Mathf.CeilToInt(stage.RemainingTime):00}    C {stage.CurrentCredits:N0} / {stage.Quota:N0}", centerStyle);

            Rect bar = new Rect(left + 24f, 70f, width - 48f, 14f);
            GUI.Box(bar, GUIContent.none);
            Color previous = GUI.color;
            GUI.color = stage.CurrentCredits >= stage.Quota ? new Color(0.35f, 1f, 0.55f) : Color.cyan;
            GUI.Box(new Rect(bar.x + 2f, bar.y + 2f, (bar.width - 4f) * stage.QuotaProgress, bar.height - 4f), GUIContent.none);
            GUI.color = previous;

            if (stage.State == StageState.Playing) return;
            string heading = stage.State == StageState.Success ? "STAGE CLEAR" : "RUN FAILED";
            string detail = stage.State == StageState.Success
                ? $"Quota reached with {stage.CurrentCredits:N0} C"
                : $"Needed {Mathf.Max(0, stage.Quota - stage.CurrentCredits):N0} more C";
            float panelWidth = 440f;
            float panelHeight = 150f;
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
