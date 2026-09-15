using UnityEngine;

namespace ScrapRush.Core
{
    // A scene-local 2D source keeps one-shots alive after their originating object is destroyed.
    public sealed class SfxPlayer : MonoBehaviour
    {
        private static SfxPlayer instance;
        private AudioSource source;

        public static void Play(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            if (instance == null)
            {
                instance = FindFirstObjectByType<SfxPlayer>();
                if (instance == null)
                    instance = new GameObject("SFX Player").AddComponent<SfxPlayer>();
            }

            instance.Source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        private AudioSource Source
        {
            get
            {
                if (source != null) return source;
                source = GetComponent<AudioSource>();
                if (source == null) source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                return source;
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
