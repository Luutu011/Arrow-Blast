using System;
using UnityEngine;
using ArrowBlast.Interfaces;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
}

namespace ArrowBlast.Managers
{
    public class AudioManager : MonoBehaviour, IAudioService
    {
        public Sound[] musicSounds, sfxSounds;
        public AudioSource musicSource, sfxSource;

        [Header("SFX Settings")]
        [SerializeField] private float sfxCooldown = 0.05f;
        private System.Collections.Generic.Dictionary<string, float> lastPlayTime = new System.Collections.Generic.Dictionary<string, float>();

        private ISettingsService _settings;

        // Called by VContainer after instantiation
        public void Initialize(ISettingsService settings)
        {
            _settings = settings;
            UpdateSettings();
            PlayMusic("Backgound");
        }

        public void UpdateSettings()
        {
            if (_settings == null) return;

            if (musicSource != null)
                musicSource.mute = !_settings.MusicEnabled;

            if (sfxSource != null)
                sfxSource.mute = !_settings.SfxEnabled;
        }

        public void PlayMusic(string name)
        {
            if (_settings != null && !_settings.MusicEnabled) return;

            Sound s = Array.Find(musicSounds, x => x.name == name);
            if (s == null)
            {
                Debug.Log("Sound Not Found: " + name);
            }
            else
            {
                musicSource.clip = s.clip;
                musicSource.Play();
            }
        }

        public void PlaySfx(string name)
        {
            if (_settings != null && !_settings.SfxEnabled) return;

            if (lastPlayTime.TryGetValue(name, out float lastTime))
            {
                if (Time.time - lastTime < sfxCooldown) return;
            }

            Sound s = Array.Find(sfxSounds, x => x.name == name);
            if (s == null)
            {
                Debug.Log("Sound Not Found: " + name);
            }
            else
            {
                sfxSource.PlayOneShot(s.clip);
                lastPlayTime[name] = Time.time;
            }
        }

        public void TriggerHaptic()
        {
            if (_settings != null && _settings.HapticEnabled)
            {
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            }
        }
    }
}