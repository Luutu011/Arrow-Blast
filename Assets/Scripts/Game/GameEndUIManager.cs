using UnityEngine;
using UnityEngine.UI;
using ArrowBlast.Managers;
using DG.Tweening;
using System;

namespace ArrowBlast.UI
{
    public class GameEndUIManager : MonoBehaviour
    {
        public static GameEndUIManager Instance { get; private set; }

        [Header("Win Panel")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private Button winDoubleCoinsButton;
        [SerializeField] private Button winHomeButton;
        [SerializeField] private ParticleSystem winParticleEffect;
        [SerializeField] private TMPro.TextMeshProUGUI winCoinText;

        [Header("Lose Panel")]
        [SerializeField] private GameObject losePanel;
        [SerializeField] private Button loseRestartWithExtraButton;
        [SerializeField] private Button loseHomeButton;

        private GameManager gameManager;
        private CoinSystem coinSystem;
        private int lastEarnedCoins;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            gameManager = FindObjectOfType<GameManager>();
            coinSystem = FindObjectOfType<CoinSystem>();

            if (winDoubleCoinsButton) winDoubleCoinsButton.onClick.AddListener(OnWinDoubleCoinsClicked);
            if (winHomeButton) winHomeButton.onClick.AddListener(OnHomeClicked);
            if (loseRestartWithExtraButton) loseRestartWithExtraButton.onClick.AddListener(OnLoseRestartWithExtraClicked);
            if (loseHomeButton) loseHomeButton.onClick.AddListener(OnHomeClicked);

            winPanel.SetActive(false);
            losePanel.SetActive(false);
        }

        public void ShowWinPanel(int earnedCoins)
        {
            lastEarnedCoins = earnedCoins;
            if (winCoinText) winCoinText.text = "+" + earnedCoins;

            winPanel.SetActive(true);
            winPanel.transform.localScale = Vector3.zero;
            winPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

            if (winParticleEffect) winParticleEffect.Play();
        }

        public void ShowLosePanel()
        {
            losePanel.SetActive(true);
            losePanel.transform.localScale = Vector3.zero;
            losePanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }

        private void OnWinDoubleCoinsClicked()
        {
            AudioManager.Instance.TriggerHaptic(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.SoftImpact);
            AdsManager.Instance.ShowRewardedAd(() =>
            {
                if (coinSystem != null)
                {
                    coinSystem.AddCoins(lastEarnedCoins); // Add again to double
                    if (winCoinText) winCoinText.text = "+" + (lastEarnedCoins * 2);
                    winDoubleCoinsButton.interactable = false;
                }
            });
        }

        private void OnLoseRestartWithExtraClicked()
        {
            AudioManager.Instance.TriggerHaptic(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.SoftImpact);
            AdsManager.Instance.ShowRewardedAd(() =>
            {
                losePanel.SetActive(false);
                if (gameManager != null)
                {
                    gameManager.RestartWithExtraSlot();
                }
            });
        }

        private void OnHomeClicked()
        {
            AudioManager.Instance.TriggerHaptic(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.SoftImpact);
            winPanel.SetActive(false);
            losePanel.SetActive(false);
            if (gameManager != null)
            {
                gameManager.ReturnToLevelSelect();
            }
        }
    }
}
