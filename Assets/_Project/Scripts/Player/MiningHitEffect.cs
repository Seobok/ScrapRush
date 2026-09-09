using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScrapRush.Player
{
    // A world-space one-shot, independent of the ore's lifetime and the player's movement.
    public sealed class MiningHitEffect : MonoBehaviour
    {
        private SpriteRenderer visual;
        private Sprite[] frames;
        private float framesPerSecond;
        private float elapsed;

        public static void Play(MiningSettings settings, Vector3 position, float oreSize, Scene scene)
        {
            if (settings.hitFrames == null || settings.hitFrames.Length == 0 || settings.hitFrames[0] == null) return;
            var instance = new GameObject("MiningHit");
            SceneManager.MoveGameObjectToScene(instance, scene);
            instance.transform.position = position;
            var effect = instance.AddComponent<MiningHitEffect>();
            effect.frames = settings.hitFrames;
            effect.framesPerSecond = Mathf.Max(1f, settings.hitFramesPerSecond);
            effect.visual = instance.AddComponent<SpriteRenderer>();
            effect.visual.sortingOrder = 20;
            effect.visual.sprite = effect.frames[0];
            float canvasSize = Mathf.Max(effect.frames[0].rect.width, effect.frames[0].rect.height) / effect.frames[0].pixelsPerUnit;
            instance.transform.localScale = Vector3.one * (oreSize * settings.hitSizeMultiplier / canvasSize);
        }

        private void Update() => Tick(Time.deltaTime);

        internal void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            int frame = Mathf.FloorToInt(elapsed * framesPerSecond);
            if (frame >= frames.Length)
            {
                visual.enabled = false;
                Destroy(gameObject);
                return;
            }
            visual.sprite = frames[frame];
            // Fade the remaining glow during the final two frames.
            float alpha = Mathf.Clamp01((frames.Length - elapsed * framesPerSecond) / 2f);
            visual.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
