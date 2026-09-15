using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ScrapRush.Player
{
    public sealed class ElectricArcEffect : MonoBehaviour
    {
        private LineRenderer line;
        private Material material;
        private Color color;
        private float duration;
        private float elapsed;

        public static void Play(IReadOnlyList<Vector3> points, ElectricSettings settings, Scene scene)
        {
            if (points == null || points.Count < 2 || settings.arcDuration <= 0 || settings.arcWidth <= 0) return;
            var instance = new GameObject("Electric Arc");
            SceneManager.MoveGameObjectToScene(instance, scene);
            var effect = instance.AddComponent<ElectricArcEffect>();
            effect.duration = settings.arcDuration;
            effect.color = settings.arcColor;
            effect.line = instance.AddComponent<LineRenderer>();
            effect.line.useWorldSpace = true;
            effect.line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) effect.line.SetPosition(i, points[i]);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Destroy(instance);
                return;
            }
            effect.material = new Material(shader) { name = "Electric Arc (Runtime)" };
            effect.line.sharedMaterial = effect.material;
            effect.line.startWidth = settings.arcWidth;
            effect.line.endWidth = settings.arcWidth * 0.45f;
            effect.line.startColor = effect.color;
            effect.line.endColor = effect.color;
            effect.line.numCornerVertices = 2;
            effect.line.numCapVertices = 2;
            effect.line.alignment = LineAlignment.View;
            effect.line.textureMode = LineTextureMode.Stretch;
            effect.line.generateLightingData = false;
            effect.line.shadowCastingMode = ShadowCastingMode.Off;
            effect.line.receiveShadows = false;
            effect.line.sortingOrder = 22;
        }

        private void Update() => Tick(Time.deltaTime);

        internal void Tick(float deltaTime)
        {
            elapsed += Mathf.Max(0f, deltaTime);
            if (line == null || elapsed >= duration)
            {
                if (line != null) line.enabled = false;
                Destroy(gameObject);
                return;
            }
            float alpha = 1f - elapsed / duration;
            Color faded = new Color(color.r, color.g, color.b, color.a * alpha);
            line.startColor = faded;
            line.endColor = faded;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
