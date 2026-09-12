using UnityEngine;

namespace CityPuzzle.Services
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public AudioSource musicSource;
        public AudioSource sfxSource;

        const string MutedKey = "cp_muted";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ApplyMuted(PlayerPrefs.GetInt(MutedKey, 0) == 1);
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource == null || clip == null) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public bool ToggleMuted()
        {
            bool muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            muted = !muted;
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMuted(muted);
            return muted;
        }

        void ApplyMuted(bool muted)
        {
            AudioListener.volume = muted ? 0f : 1f;
        }
    }
}
