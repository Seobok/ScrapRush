using System;
using UnityEngine;

namespace ScrapRush.World
{
    // A pooled world-space one-shot played where a Scrap reaches the player.
    public sealed class ScrapAbsorbEffect : MonoBehaviour
    {
        private SpriteRenderer visual;
        private Sprite[] frames;
        private Action<ScrapAbsorbEffect> completed;
        private float framesPerSecond;
        private float elapsed;

        public bool IsPlaying { get; private set; }

        public void Play(Sprite[] animationFrames, float fps, float size, Action<ScrapAbsorbEffect> onCompleted)
        {
            frames = animationFrames;
            framesPerSecond = Mathf.Max(1f, fps);
            completed = onCompleted;
            elapsed = 0f;
            IsPlaying = true;

            if (visual == null)
            {
                visual = gameObject.AddComponent<SpriteRenderer>();
                visual.sortingOrder = 20;
            }

            visual.enabled = true;
            visual.color = Color.white;
            visual.sprite = frames[0];
            float canvasSize = Mathf.Max(frames[0].rect.width, frames[0].rect.height) / frames[0].pixelsPerUnit;
            transform.localScale = Vector3.one * (size / canvasSize);
        }

        private void Update() => Tick(Time.deltaTime);

        internal void Tick(float deltaTime)
        {
            if (!IsPlaying || deltaTime <= 0f) return;
            elapsed += deltaTime;
            int frame = Mathf.FloorToInt(elapsed * framesPerSecond);
            if (frame >= frames.Length)
            {
                IsPlaying = false;
                visual.enabled = false;
                var callback = completed;
                completed = null;
                callback?.Invoke(this);
                return;
            }

            visual.sprite = frames[frame];
        }

        public void Deactivate()
        {
            IsPlaying = false;
            completed = null;
            if (visual != null) visual.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
