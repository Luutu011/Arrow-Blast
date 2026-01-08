using UnityEngine;
using System.Collections.Generic;
using ArrowBlast.Data;

namespace ArrowBlast.Managers
{
    public class LevelManager : MonoBehaviour
    {
        [Header("Level Configuration")]
        [SerializeField] private List<LevelData> levels = new List<LevelData>();

        [Header("Runtime State")]
        [SerializeField] private int currentLevelIndex = 0;
        public int CurrentLevelIndex => currentLevelIndex;
        [SerializeField] private bool loopLevels = true;

        private const string HIGHEST_UNLOCKED_KEY = "HighestUnlockedLevel";
        private int highestUnlockedLevel = 0;

        private void Awake()
        {
            LoadProgress();
        }

        /// <summary>
        /// Check if a level is unlocked for play
        /// </summary>
        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex <= highestUnlockedLevel;
        }

        /// <summary>
        /// Get the highest unlocked level index
        /// </summary>
        public int GetHighestUnlockedLevel()
        {
            return highestUnlockedLevel;
        }

        /// <summary>
        /// Unlock the next level (called on level completion)
        /// </summary>
        public void UnlockNextLevel()
        {
            if (levels == null || levels.Count == 0) return;

            // Unlock the next level if it exists
            int nextLevel = currentLevelIndex + 1;
            if (nextLevel < levels.Count && nextLevel > highestUnlockedLevel)
            {
                highestUnlockedLevel = nextLevel;
                SaveProgress();
                Debug.Log($"[LevelManager] Unlocked level {nextLevel + 1}");
            }
        }

        private void LoadProgress()
        {
            highestUnlockedLevel = PlayerPrefs.GetInt(HIGHEST_UNLOCKED_KEY, 0);
            Debug.Log($"[LevelManager] Highest unlocked level: {highestUnlockedLevel}");
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt(HIGHEST_UNLOCKED_KEY, highestUnlockedLevel);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Returns the current level data.
        /// </summary>
        public LevelData GetCurrentLevel()
        {
            if (levels == null || levels.Count == 0)
            {
                Debug.LogWarning("[LEVEL MANAGER] No levels assigned!");
                return null;
            }

            // Ensure index is valid
            if (currentLevelIndex < 0 || currentLevelIndex >= levels.Count)
            {
                currentLevelIndex = 0;
            }

            return levels[currentLevelIndex];
        }

        /// <summary>
        /// Advances to the next level index.
        /// Returns true if a next level exists (or looped), false if finished.
        /// </summary>
        public bool AdvanceLevel()
        {
            if (levels.Count == 0) return false;

            currentLevelIndex++;

            if (currentLevelIndex >= levels.Count)
            {
                if (loopLevels)
                {
                    currentLevelIndex = 0;
                    return true;
                }
                else
                {
                    currentLevelIndex = levels.Count - 1; // Stay at last level
                    return false; // No more new levels
                }
            }
            return true;
        }

        public void ResetProgress()
        {
            currentLevelIndex = 0;
        }

        public int GetLevelCount()
        {
            return levels != null ? levels.Count : 0;
        }

        public void SetLevelIndex(int index)
        {
            if (index >= 0 && index < levels.Count)
            {
                currentLevelIndex = index;
            }
        }
    }
}
