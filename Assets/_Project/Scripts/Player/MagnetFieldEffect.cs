using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ScrapRush.Player
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class MagnetFieldEffect : MonoBehaviour
    {
        private const int Segments = 64;
        private LineRenderer line;
        private Material material;
        private Transform target;
        private Color color;
        private float duration;
        private float elapsed;

        public static MagnetFieldEffect Play(Transform target, float radius, float duration, float width,
            Color color, Scene scene)
        {
            var instance = new GameObject("Gravity Field");
            SceneManager.MoveGameObjectToScene(instance, scene);
            var effect = instance.AddComponent<MagnetFieldEffect>();
            effect.Initialize(target, radius, duration, width, color);
            return effect;
        }

        public void Stop() => Destroy(gameObject);

        private void Initialize(Transform followTarget, float radius, float lifetime, float width, Color tint)
        {
            target = followTarget;
            duration = lifetime;
            color = tint;
            line = GetComponent<LineRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            material = new Material(shader) { name = "Gravity Field (Runtime)" };
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Segments;
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = 1;
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }
            transform.position = target.position;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = target.position;
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - elapsed / duration);
            Color faded = new Color(color.r, color.g, color.b, color.a * alpha);
            line.startColor = line.endColor = faded;
            if (elapsed >= duration) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
