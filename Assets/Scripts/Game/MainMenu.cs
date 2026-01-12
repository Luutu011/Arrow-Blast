using UnityEngine;
using UnityEngine.UI;
using ArrowBlast.Managers;
using ArrowBlast.Interfaces;
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

        [Header("Settings Toggles")]
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle hapticToggle;

        [Header("Difficulty Backgrounds")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite easyBackgroundSprite;
        [SerializeField] private Sprite mediumBackgroundSprite;
        [SerializeField] private Sprite hardBackgroundSprite;

        [Header("Level Grid")]
        [SerializeField] private RectTransform levelGrid;
        [SerializeField] private Button levelButtonPrefab;
        [SerializeField] private Sprite easyLevelSprite;
        [SerializeField] private Sprite mediumLevelSprite;
        [SerializeField] private Sprite hardLevelSprite;

        [Header("Road Layout")]
        [SerializeField] private float verticalSpacing = 280f;
        [SerializeField] private float horizontalAmplitude = 160f;
        [SerializeField] private float curveFrequency = 1.0f;
        [SerializeField] private float yOffset = -550f;
        [SerializeField] private int RoadDotCount = 10;

        private ILevelProgressService levelManager;
        private IGameManager gameManager;
        private ISettingsService settingsManager;
        private IAudioService audioManager;
        private IBoosterInventory boosterInventory;
        private PanelSelection currentSelection;

        // Called by VContainer
        public void Initialize(
            ILevelProgressService levelProgress,
            IGameManager game,
            ISettingsService settings,
            IAudioService audio,
            IBoosterInventory inventory)
        {
            levelManager = levelProgress;
            gameManager = game;
            settingsManager = settings;
            audioManager = audio;
            boosterInventory = inventory;
        }

        private void Start()
        {
            // Removed FindObjectOfType calls as dependencies are injected

            // Assign Navigation Listeners
            if (homeButton) homeButton.onClick.AddListener(ShowLevelPanel);
            if (shopButton) shopButton.onClick.AddListener(ShowShopPanel);
            if (settingsButton) settingsButton.onClick.AddListener(ShowSettingsPanel);
            if (closeSettingsButton) closeSettingsButton.onClick.AddListener(HideSettingsPanel);

            InitializeSettingsToggles();

            ShowLevelPanel();
        }

        private void OnEnable()
        {
            InitializeSettingsToggles();
        }

        private void InitializeSettingsToggles()
        {
            if (settingsManager == null) return;

            if (sfxToggle)
            {
                sfxToggle.onValueChanged.RemoveAllListeners();
                sfxToggle.SetIsOnWithoutNotify(settingsManager.SfxEnabled);
                sfxToggle.onValueChanged.AddListener(settingsManager.SetSfx);
            }

            if (musicToggle)
            {
                musicToggle.onValueChanged.RemoveAllListeners();
                musicToggle.SetIsOnWithoutNotify(settingsManager.MusicEnabled);
                musicToggle.onValueChanged.AddListener(settingsManager.SetMusic);
            }

            if (hapticToggle)
            {
                hapticToggle.onValueChanged.RemoveAllListeners();
                hapticToggle.SetIsOnWithoutNotify(settingsManager.HapticEnabled);
                hapticToggle.onValueChanged.AddListener(settingsManager.SetHaptic);
            }
        }

        public void ShowLevelPanel()
        {
            // Only return if we are already in the Home state AND the grid is actually populated
            if (currentSelection == PanelSelection.Home && levelPanel != null && levelPanel.activeSelf && levelGrid.childCount > 0) return;

            if (levelPanel) levelPanel.SetActive(true);
            if (shopPanel) shopPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);

            UpdateNavigationVisuals(PanelSelection.Home);
            UpdateBackgroundForCurrentLevel();
            PopulateLevelGrid();
        }

        public void ShowShopPanel()
        {
            if (currentSelection == PanelSelection.Shop && shopPanel != null && shopPanel.activeSelf) return;

            if (levelPanel) levelPanel.SetActive(false);
            if (shopPanel) shopPanel.SetActive(true);
            if (settingsPanel) settingsPanel.SetActive(false);

            UpdateNavigationVisuals(PanelSelection.Shop);
        }

        public void ShowSettingsPanel()
        {
            if (settingsPanel) settingsPanel.SetActive(true);
        }

        public void HideSettingsPanel()
        {
            if (settingsPanel) settingsPanel.SetActive(false);
        }

        private enum PanelSelection { Home, Shop, Settings }

        private void UpdateNavigationVisuals(PanelSelection selected)
        {
            currentSelection = selected;
            SetButtonState(homeButton, selected == PanelSelection.Home);
            SetButtonState(shopButton, selected == PanelSelection.Shop);
            SetButtonState(settingsButton, selected == PanelSelection.Settings);
        }

        private void SetButtonState(Button btn, bool isActive)
        {
            if (btn == null) return;

            // Use Image color alpha to highlight active state without disabling the button
            // Disabling the button (interactable=false) often triggers a "faded" disabled color in Unity
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                Color c = Color.white; // Keep at full white brightness
                c.a = isActive ? 1.0f : 0.6f; // Use transparency for inactive buttons
                img.color = c;
            }

            // Note: We keep interactable = true so it doesn't look gray/faded, 
            // but we check the current state in ShowPanel methods to avoid redundant clicks.
        }

        private void UpdateBackgroundForCurrentLevel()
        {
            if (backgroundImage == null || levelManager == null) return;

            // Use the highest unlocked level to determine the menu background
            int progressionIndex = levelManager.GetHighestUnlockedLevel();
            var currentLevel = levelManager.GetLevel(progressionIndex);

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

            int currentLevel = levelManager.GetHighestUnlockedLevel();
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

                var textComp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComp != null) textComp.text = (levelIdx + 1).ToString();

                var img = btn.GetComponent<UnityEngine.UI.Image>();
                bool isUnlocked = levelManager.IsLevelUnlocked(levelIdx);

                // Set sprite based on difficulty regardless of unlock status
                var levelData = levelManager.GetLevel(levelIdx);
                if (levelData != null && img != null)
                {
                    // Locked icons shouldn't be TOO grey, just a bit desaturated/transparent
                    img.color = isUnlocked ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.85f);
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
                }

                if (isUnlocked)
                {
                    int index = levelIdx;
                    btn.onClick.AddListener(() => OnLevelSelected(index));
                }
                else
                {
                    btn.interactable = false;
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
