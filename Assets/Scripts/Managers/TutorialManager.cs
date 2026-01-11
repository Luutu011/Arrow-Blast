using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ArrowBlast.Managers
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TutorialManager>();
                }
                return _instance;
            }
        }
        private static TutorialManager _instance;

        [Header("Tutorial UI")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private Image tutorialImage;
        [SerializeField] private Button closeButton;

        [Header("Tutorial Resources")]
        [SerializeField] private Sprite welcomeTutorialSprite;
        [SerializeField] private Sprite instantExitTutorialSprite;
        [SerializeField] private Sprite extraSlotTutorialSprite;

        private const string TUTORIAL_PREFIX = "TutorialSeen_";

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
        }

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(HideTutorial);
            }
        }

        public void CheckTutorials(int currentLevelIndex)
        {
            Debug.Log($"[TutorialManager] Checking tutorials for level index: {currentLevelIndex}");

            // Welcome Tutorial (Level 1)
            if (currentLevelIndex == 0)
            {
                ShowTutorial("Welcome", welcomeTutorialSprite);
            }
            // Instant Exit Tutorial (Unlocked at Level 6, index 5)
            else if (currentLevelIndex == 5)
            {
                ShowTutorial("InstantExit", instantExitTutorialSprite);
            }
            // Extra Slot Tutorial (Unlocked at Level 11, index 10)
            else if (currentLevelIndex == 10)
            {
                ShowTutorial("ExtraSlot", extraSlotTutorialSprite);
            }
        }

        private void ShowTutorial(string key, Sprite sprite)
        {
            if (PlayerPrefs.GetInt(TUTORIAL_PREFIX + key, 0) == 1) return;

            Debug.Log($"[TutorialManager] Attempting to show tutorial: {key}");

            if (sprite == null)
            {
                Debug.LogWarning($"[TutorialManager] Sprite for {key} is missing!");
                return;
            }

            if (tutorialPanel == null)
            {
                Debug.LogWarning("[TutorialManager] tutorialPanel is not assigned!");
                return;
            }

            // Gift boosters on first unlock
            if (BoosterInventory.Instance != null)
            {
                if (key == "InstantExit") BoosterInventory.Instance.AddBooster(BoosterType.InstantExit, 2);
                if (key == "ExtraSlot") BoosterInventory.Instance.AddBooster(BoosterType.ExtraSlot, 2);
            }

            if (tutorialImage != null)
                tutorialImage.sprite = sprite;

            tutorialPanel.SetActive(true);

            PlayerPrefs.SetInt(TUTORIAL_PREFIX + key, 1);
            PlayerPrefs.Save();
        }

        public void HideTutorial()
        {
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
        }
    }
}
