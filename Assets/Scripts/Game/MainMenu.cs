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

        [Header("Level Grid")]
        [SerializeField] private RectTransform levelGrid;
        [SerializeField] private Button levelButtonPrefab;

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

            float verticalSpacing = 180f;
            float horizontalAmplitude = 80f;

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
                float yPos = i * verticalSpacing - 350f;
                float xPos = Mathf.Sin(i * 1.5f) * horizontalAmplitude;
                Vector2 currentPos = new Vector2(xPos, yPos);
                rt.anchoredPosition = currentPos;

                btn.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = (levelIdx + 1).ToString();

                var img = btn.GetComponent<UnityEngine.UI.Image>();
                img.color = (i == 0) ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.3f, 0.3f, 0.3f);

                int index = levelIdx;
                btn.onClick.AddListener(() => OnLevelSelected(index));

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
            int dotCount = 5;
            float margin = 60f;

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
                rt.sizeDelta = new Vector2(15, 15);
                rt.anchoredPosition = dotPos;
                rt.localScale = Vector3.zero;

                var img = dotBtn.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(1f, 1f, 1f, 0.5f);

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
