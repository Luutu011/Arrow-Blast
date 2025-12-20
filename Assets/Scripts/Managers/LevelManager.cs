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
        [SerializeField] private bool loopLevels = true;

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
    }
}
