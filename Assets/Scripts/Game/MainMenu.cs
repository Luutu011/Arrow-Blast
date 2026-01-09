using UnityEngine;
using UnityEngine.UI;
using ArrowBlast.Managers;
using System.Collections.Generic;
using DG.Tweening;
using System.Collections;

namespace ArrowBlast.UI
{
    public class MainMenu : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject levelPanel; // Home
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Navigation Buttons")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeSettingsButton;

        [Header("Difficulty Backgrounds")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite easyBackgroundSprite;
        [SerializeField] private Sprite mediumBackgroundSprite;
        [SerializeField] private Sprite hardBackgroundSprite;

        [Header("Level Grid")]
        [SerializeField] private RectTransform levelGrid;
        [SerializeField] private Button levelButtonPrefab;
        [SerializeField] private Sprite lockedLevelSprite;
        [SerializeField] private Sprite easyLevelSprite;
        [SerializeField] private Sprite mediumLevelSprite;
        [SerializeField] private Sprite hardLevelSprite;

        [Header("Road Layout")]
        [SerializeField] private float verticalSpacing = 280f;
        [SerializeField] private float horizontalAmplitude = 160f;
        [SerializeField] private float curveFrequency = 1.0f;
        [SerializeField] private float yOffset = -550f;
        [SerializeField] private int RoadDotCount = 10;

        private LevelManager levelManager;
        private GameManager gameManager;

        private void Start()
        {
            levelManager = FindObjectOfType<LevelManager>();
            gameManager = FindObjectOfType<GameManager>();

            // Assign Navigation Listeners
            if (homeButton) homeButton.onClick.AddListener(ShowLevelPanel);
            if (shopButton) shopButton.onClick.AddListener(ShowShopPanel);
            if (settingsButton) settingsButton.onClick.AddListener(ShowSettingsPanel);
            if (closeSettingsButton) closeSettingsButton.onClick.AddListener(HideSettingsPanel);

            ShowLevelPanel();
        }

        public void ShowLevelPanel()
        {
            levelPanel.SetActive(true);
            if (shopPanel) shopPanel.SetActive(false);

            UpdateNavigationVisuals(true);
            UpdateBackgroundForCurrentLevel();
            PopulateLevelGrid();
        }

        public void ShowShopPanel()
        {
            levelPanel.SetActive(false);
            if (shopPanel) shopPanel.SetActive(true);

            UpdateNavigationVisuals(false);
        }

        public void ShowSettingsPanel()
        {
            if (settingsPanel) settingsPanel.SetActive(true);
        }

        public void HideSettingsPanel()
        {
            if (settingsPanel) settingsPanel.SetActive(false);
        }

        private void UpdateNavigationVisuals(bool isAtHome)
        {
            if (homeButton) homeButton.interactable = !isAtHome;
            if (shopButton) shopButton.interactable = isAtHome;
        }

        private void UpdateBackgroundForCurrentLevel()
        {
            if (backgroundImage == null || levelManager == null) return;

            var currentLevel = levelManager.GetCurrentLevel();
            if (currentLevel == null) return;

            switch (currentLevel.difficulty)
            {
                case ArrowBlast.Data.LevelData.Difficulty.Easy:
                    if (easyBackgroundSprite != null)
                        backgroundImage.sprite = easyBackgroundSprite;
                    break;
                case ArrowBlast.Data.LevelData.Difficulty.Medium:
                    if (mediumBackgroundSprite != null)
                        backgroundImage.sprite = mediumBackgroundSprite;
                    break;
                case ArrowBlast.Data.LevelData.Difficulty.Hard:
                    if (hardBackgroundSprite != null)
                        backgroundImage.sprite = hardBackgroundSprite;
                    break;
            }
        }

        private void PopulateLevelGrid()
        {
            // Clear existing
            foreach (Transform child in levelGrid)
            {
                Destroy(child.gameObject);
            }

            if (levelManager == null) return;

            var glg = levelGrid.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (glg) glg.enabled = false;

            int currentLevel = levelManager.CurrentLevelIndex;
            int totalLevels = levelManager.GetLevelCount();
            int highestUnlocked = levelManager.GetHighestUnlockedLevel();

            Vector2 lastPos = Vector2.zero;
            bool firstPosSet = false;

            for (int i = 0; i < 5; i++)
            {
                int levelIdx = currentLevel + i;
                if (levelIdx >= totalLevels) break;

                Button btn = Instantiate(levelButtonPrefab, levelGrid);
                btn.gameObject.SetActive(true);
                btn.transform.localScale = Vector3.zero;

                RectTransform rt = btn.GetComponent<RectTransform>();
                float yPos = i * verticalSpacing + yOffset;
                float xPos = Mathf.Sin(i * curveFrequency) * horizontalAmplitude;
                Vector2 currentPos = new Vector2(xPos, yPos);
                rt.anchoredPosition = currentPos;

                btn.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = (levelIdx + 1).ToString();

                // Check if level is unlocked
                bool isUnlocked = levelManager.IsLevelUnlocked(levelIdx);
                var img = btn.GetComponent<UnityEngine.UI.Image>();

                if (!isUnlocked)
                {
                    // Locked level - use locked sprite if available
                    if (lockedLevelSprite != null)
                    {
                        img.sprite = lockedLevelSprite;
                    }
                    img.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    btn.interactable = false;
                }
                else
                {
                    // Set sprite based on difficulty
                    var levelData = levelManager.GetLevel(levelIdx);
                    if (levelData != null)
                    {
                        img.color = Color.white; // Reset color to show sprite clearly
                        switch (levelData.difficulty)
                        {
                            case ArrowBlast.Data.LevelData.Difficulty.Easy:
                                if (easyLevelSprite != null) img.sprite = easyLevelSprite;
                                break;
                            case ArrowBlast.Data.LevelData.Difficulty.Medium:
                                if (mediumLevelSprite != null) img.sprite = mediumLevelSprite;
                                break;
                            case ArrowBlast.Data.LevelData.Difficulty.Hard:
                                if (hardLevelSprite != null) img.sprite = hardLevelSprite;
                                break;
                        }

                        // Add a border or highlight if it's the current level
                        if (i == 0)
                        {
                            // Optional: brightening it or add effect?
                        }
                    }
                }

                int index = levelIdx;
                if (isUnlocked)
                {
                    btn.onClick.AddListener(() => OnLevelSelected(index));
                }

                if (firstPosSet)
                {
                    CreateRoadDots(lastPos, currentPos, i);
                }

                lastPos = currentPos;
                firstPosSet = true;

                rt.DOScale(Vector3.one, 0.5f)
                  .SetDelay(i * 0.2f)
                  .SetEase(Ease.OutBack);
            }
        }

        private void CreateRoadDots(Vector2 start, Vector2 end, int levelOrder)
        {
            int dotCount = RoadDotCount;
            float margin = 70f;

            Vector2 dir = (end - start).normalized;
            float dist = Vector2.Distance(start, end);

            Vector2 actualStart = start + dir * margin;
            Vector2 actualEnd = end - dir * margin;

            for (int j = 1; j <= dotCount; j++)
            {
                float t = (float)j / (dotCount + 1);
                Vector2 dotPos = Vector2.Lerp(actualStart, actualEnd, t);

                Button dotBtn = Instantiate(levelButtonPrefab, levelGrid);
                dotBtn.gameObject.name = "RoadDot";
                dotBtn.interactable = false;

                var tmp = dotBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp) Destroy(tmp.gameObject);

                RectTransform rt = dotBtn.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(20, 20);
                rt.anchoredPosition = dotPos;
                rt.localScale = Vector3.zero;

                var img = dotBtn.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(1f, 1f, 1f, 0.6f);

                rt.DOScale(Vector3.one, 0.3f)
                  .SetDelay((levelOrder - 1) * 0.2f + (j * 0.04f))
                  .SetEase(Ease.OutBack);
            }
        }

        private void OnLevelSelected(int index)
        {
            if (levelManager != null && gameManager != null)
            {
                levelManager.SetLevelIndex(index);
                gameManager.RestartLevel();
                levelPanel.SetActive(false);
                if (shopPanel) shopPanel.SetActive(false);
                gameObject.SetActive(false);
            }
        }

        public void ExitGame()
        {
            Debug.Log("Exiting Game...");
            Application.Quit();
        }
    }
}
