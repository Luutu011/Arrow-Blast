using System;
using UnityEngine;
using Solo.MOST_IN_ONE;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
}
namespace ArrowBlast.Managers
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public Sound[] musicSounds, sfxSounds;
        public AudioSource musicSource, sfxSource;

        private void Start()
        {
            UpdateSettings();
            PlayMusic("Backgound");
        }

        public void UpdateSettings()
        {
            if (SettingsManager.Instance == null) return;

            if (musicSource != null)
                musicSource.mute = !SettingsManager.Instance.MusicEnabled;

            if (sfxSource != null)
                sfxSource.mute = !SettingsManager.Instance.SfxEnabled;
        }

        public void PlayMusic(string name)
        {
            if (SettingsManager.Instance != null && !SettingsManager.Instance.MusicEnabled) return;

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

        [Header("SFX Settings")]
        [SerializeField] private float sfxCooldown = 0.05f;
        private System.Collections.Generic.Dictionary<string, float> lastPlayTime = new System.Collections.Generic.Dictionary<string, float>();
        private System.Collections.Generic.Dictionary<string, AudioClip> sfxCache;

        public void PlaySfx(string name)
        {
            if (SettingsManager.Instance != null && !SettingsManager.Instance.SfxEnabled) return;

            if (sfxCache == null)
            {
                sfxCache = new System.Collections.Generic.Dictionary<string, AudioClip>();
                foreach (var s in sfxSounds) if (s != null) sfxCache[s.name] = s.clip;
            }

            if (lastPlayTime.TryGetValue(name, out float lastTime))
            {
                if (Time.time - lastTime < sfxCooldown) return;
            }

            if (sfxCache.TryGetValue(name, out AudioClip clip))
            {
                sfxSource.PlayOneShot(clip);
                lastPlayTime[name] = Time.time;
            }
            else
            {
                Debug.LogWarning("Sound Not Found: " + name);
            }
        }

        private const float HAPTIC_COOLDOWN = 0.05f;

        public void TriggerHaptic(MOST_HapticFeedback.HapticTypes type = MOST_HapticFeedback.HapticTypes.LightImpact)
        {
            if (SettingsManager.Instance != null && SettingsManager.Instance.HapticEnabled)
            {
                MOST_HapticFeedback.GenerateWithCooldown(type, HAPTIC_COOLDOWN);
            }
        }
    }
}