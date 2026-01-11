using UnityEngine;
using UnityEngine.UI;
using ArrowBlast.Managers;
using ArrowBlast.Interfaces;
using DG.Tweening;
using System;

namespace ArrowBlast.UI
{
    public class GameEndUIManager : MonoBehaviour
    {
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

        private IGameManager gameManager;
        private ICoinService coinSystem;
        private IAdsService adsManager;
        private int lastEarnedCoins;

        // Called by VContainer
        public void Initialize(IGameManager manager, ICoinService coins, IAdsService ads)
        {
            gameManager = manager;
            coinSystem = coins;
            adsManager = ads;

            if (winDoubleCoinsButton) 
            {
                winDoubleCoinsButton.onClick.RemoveAllListeners();
                winDoubleCoinsButton.onClick.AddListener(OnWinDoubleCoinsClicked);
            }
            if (winHomeButton) 
            {
                winHomeButton.onClick.RemoveAllListeners();
                winHomeButton.onClick.AddListener(OnHomeClicked);
            }
            if (loseRestartWithExtraButton) 
            {
                loseRestartWithExtraButton.onClick.RemoveAllListeners();
                loseRestartWithExtraButton.onClick.AddListener(OnLoseRestartWithExtraClicked);
            }
            if (loseHomeButton) 
            {
                loseHomeButton.onClick.RemoveAllListeners();
                loseHomeButton.onClick.AddListener(OnHomeClicked);
            }

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
            if (adsManager == null) return;
            
            adsManager.ShowRewardedAd(() =>
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
            if (adsManager == null) return;

            adsManager.ShowRewardedAd(() =>
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
            winPanel.SetActive(false);
            losePanel.SetActive(false);
            if (gameManager != null)
            {
                gameManager.ReturnToLevelSelect();
            }
        }
    }
}
