using UnityEngine;
using Solo.MOST_IN_ONE;

namespace ArrowBlast.Managers
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        private const string SFX_KEY = "Settings_Sfx";
        private const string MUSIC_KEY = "Settings_Music";
        private const string HAPTIC_KEY = "Settings_Haptic";

        public bool SfxEnabled { get; private set; }
        public bool MusicEnabled { get; private set; }
        public bool HapticEnabled { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Application.targetFrameRate = 30;
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadSettings()
        {
            SfxEnabled = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
            MusicEnabled = PlayerPrefs.GetInt(MUSIC_KEY, 1) == 1;
            HapticEnabled = PlayerPrefs.GetInt(HAPTIC_KEY, 1) == 1;
            MOST_HapticFeedback.HapticsEnabled = HapticEnabled;
        }

        public void SetSfx(bool enabled)
        {
            AudioManager.Instance.TriggerHaptic(MOST_HapticFeedback.HapticTypes.SoftImpact);
            SfxEnabled = enabled;
            PlayerPrefs.SetInt(SFX_KEY, enabled ? 1 : 0);
            PlayerPrefs.Save();
            AudioManager.Instance.UpdateSettings();
        }

        public void SetMusic(bool enabled)
        {
            AudioManager.Instance.TriggerHaptic(MOST_HapticFeedback.HapticTypes.SoftImpact);
            MusicEnabled = enabled;
            PlayerPrefs.SetInt(MUSIC_KEY, enabled ? 1 : 0);
            PlayerPrefs.Save();
            AudioManager.Instance.UpdateSettings();
        }

        public void SetHaptic(bool enabled)
        {
            AudioManager.Instance.TriggerHaptic(MOST_HapticFeedback.HapticTypes.SoftImpact);
            HapticEnabled = enabled;
            PlayerPrefs.SetInt(HAPTIC_KEY, enabled ? 1 : 0);
            PlayerPrefs.Save();
            MOST_HapticFeedback.HapticsEnabled = enabled;
        }
    }
}
