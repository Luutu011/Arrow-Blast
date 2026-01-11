using UnityEngine;
using ArrowBlast.Interfaces;

namespace ArrowBlast.Managers
{
    public class SettingsManager : MonoBehaviour, ISettingsService
    {
        private const string SFX_KEY = "Settings_Sfx";
        private const string MUSIC_KEY = "Settings_Music";
        private const string HAPTIC_KEY = "Settings_Haptic";

        public bool SfxEnabled { get; private set; }
        public bool MusicEnabled { get; private set; }
        public bool HapticEnabled { get; private set; }

        private void Awake()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            SfxEnabled = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
            MusicEnabled = PlayerPrefs.GetInt(MUSIC_KEY, 1) == 1;
            HapticEnabled = PlayerPrefs.GetInt(HAPTIC_KEY, 1) == 1;
        }

        public void SetSfx(bool enabled)
        {
            SfxEnabled = enabled;
            PlayerPrefs.SetInt(SFX_KEY, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMusic(bool enabled)
        {
            MusicEnabled = enabled;
            PlayerPrefs.SetInt(MUSIC_KEY, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetHaptic(bool enabled)
        {
            HapticEnabled = enabled;
            PlayerPrefs.SetInt(HAPTIC_KEY, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
