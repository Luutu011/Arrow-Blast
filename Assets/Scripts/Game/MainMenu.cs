using UnityEngine;
using UnityEngine.UI;
using ArrowBlast.Managers;
using System.Collections.Generic;

namespace ArrowBlast.UI
{
    public class MainMenu : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject levelPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Main Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Back Buttons")]
        [SerializeField] private Button levelBackButton;
        [SerializeField] private Button settingsBackButton;

        [Header("Level Grid")]
        [SerializeField] private RectTransform levelGrid;
        [SerializeField] private Button levelButtonPrefab;

        private LevelManager levelManager;
        private GameManager gameManager;

        private void Start()
        {
            levelManager = FindObjectOfType<LevelManager>();
            gameManager = FindObjectOfType<GameManager>();

            // Assign Listeners
            if (playButton) playButton.onClick.AddListener(ShowLevelPanel);
            if (settingsButton) settingsButton.onClick.AddListener(ShowSettingsPanel);
            if (exitButton) exitButton.onClick.AddListener(ExitGame);
            if (levelBackButton) levelBackButton.onClick.AddListener(ShowMainPanel);
            if (settingsBackButton) settingsBackButton.onClick.AddListener(ShowMainPanel);

            ShowMainPanel();
        }

        public void ShowMainPanel()
        {
            mainPanel.SetActive(true);
            levelPanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        public void ShowLevelPanel()
        {
            mainPanel.SetActive(false);
            levelPanel.SetActive(true);
            settingsPanel.SetActive(false);

            PopulateLevelGrid();
        }

        public void ShowSettingsPanel()
        {
            mainPanel.SetActive(false);
            levelPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        private void PopulateLevelGrid()
        {
            // Clear existing buttons
            foreach (Transform child in levelGrid)
            {
                Destroy(child.gameObject);
            }

            if (levelManager == null) return;

            // Get levels from level manager (need to make levels public or add a count)
            int levelCount = levelManager.GetLevelCount();
            for (int i = 0; i < levelCount; i++)
            {
                int index = i;
                Button btn = Instantiate(levelButtonPrefab, levelGrid);
                btn.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = (i + 1).ToString();
                btn.onClick.AddListener(() => OnLevelSelected(index));
            }
        }

        private void OnLevelSelected(int index)
        {
            if (levelManager != null && gameManager != null)
            {
                levelManager.SetLevelIndex(index);
                gameManager.RestartLevel(); // Need to implement/make public
                levelPanel.SetActive(false); // Hide menu to show game
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
