using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowBlast.Interfaces;

namespace ArrowBlast.Managers
{
    /// <summary>
    /// Handles all shop UI interactions and displays
    /// Follows Single Responsibility Principle - only manages shop UI
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [Header("Shop Buttons")]
        [SerializeField] private Button buyInstantExitButton;
        [SerializeField] private Button buyExtraSlotButton;

        [Header("IAP Buttons")]
        [SerializeField] private Button buyRemoveAdsButton;
        [SerializeField] private Button buy100CoinsButton;
        [SerializeField] private Button buy500CoinsButton;

        [Header("Display")]
        [SerializeField] private TextMeshProUGUI coinBalanceText;
        [SerializeField] private TextMeshProUGUI boosterCostText;

        [Header("Inventory Display")]
        [SerializeField] private TextMeshProUGUI instantExitInventoryText;
        [SerializeField] private TextMeshProUGUI extraSlotInventoryText;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private float feedbackDisplayDuration = 2f;

        private IShopService shopManager;
        private ICoinService coinSystem;
        private IBoosterInventory boosterInventory;
        private IIAPService iapService;

        public void Initialize(IShopService shop, ICoinService coins, IBoosterInventory inventory, IIAPService iap)
        {
            shopManager = shop;
            coinSystem = coins;
            boosterInventory = inventory;
            iapService = iap;
        }

        private void Awake()
        {
            // Initialization handled by VContainer
        }

        private void OnEnable()
        {
            SetupButtons();
            SubscribeToEvents();
            UpdateDisplay();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SetupButtons()
        {
            if (buyInstantExitButton != null)
            {
                buyInstantExitButton.onClick.RemoveAllListeners();
                buyInstantExitButton.onClick.AddListener(() => OnPurchaseBooster(BoosterType.InstantExit));
            }

            if (buyExtraSlotButton != null)
            {
                buyExtraSlotButton.onClick.RemoveAllListeners();
                buyExtraSlotButton.onClick.AddListener(() => OnPurchaseBooster(BoosterType.ExtraSlot));
            }

            // Setup IAP buttons
            if (buyRemoveAdsButton != null)
            {
                buyRemoveAdsButton.onClick.RemoveAllListeners();
                buyRemoveAdsButton.onClick.AddListener(() => { if (iapService != null) iapService.BuyRemoveAds(); });
            }

            if (buy100CoinsButton != null)
            {
                buy100CoinsButton.onClick.RemoveAllListeners();
                buy100CoinsButton.onClick.AddListener(() => { if (iapService != null) iapService.BuyCoins100(); });
            }

            if (buy500CoinsButton != null)
            {
                buy500CoinsButton.onClick.RemoveAllListeners();
                buy500CoinsButton.onClick.AddListener(() => { if (iapService != null) iapService.BuyCoins500(); });
            }

            // Display booster cost
            if (boosterCostText != null && shopManager != null)
            {
                boosterCostText.text = $"Cost: {shopManager.GetBoosterCost()} coins";
            }
        }

        private void SubscribeToEvents()
        {
            if (coinSystem != null)
            {
                coinSystem.OnBalanceChanged += OnCoinBalanceChanged;
            }

            if (boosterInventory != null)
            {
                boosterInventory.OnInventoryChanged += OnInventoryChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (coinSystem != null)
            {
                coinSystem.OnBalanceChanged -= OnCoinBalanceChanged;
            }

            if (boosterInventory != null)
            {
                boosterInventory.OnInventoryChanged -= OnInventoryChanged;
            }
        }

        private void OnCoinBalanceChanged(int newBalance)
        {
            UpdateCoinDisplay(newBalance);
            UpdateButtonStates();
        }

        private void OnInventoryChanged(BoosterType type, int newAmount)
        {
            UpdateInventoryDisplay(type, newAmount);
        }

        private void UpdateDisplay()
        {
            if (coinSystem != null)
            {
                UpdateCoinDisplay(coinSystem.GetBalance());
            }

            if (boosterInventory != null)
            {
                UpdateInventoryDisplay(BoosterType.InstantExit, boosterInventory.GetAmount(BoosterType.InstantExit));
                UpdateInventoryDisplay(BoosterType.ExtraSlot, boosterInventory.GetAmount(BoosterType.ExtraSlot));
            }

            UpdateButtonStates();
            HideFeedback();
        }

        private void UpdateCoinDisplay(int balance)
        {
            if (coinBalanceText != null)
            {
                coinBalanceText.text = $"Coins: {balance}";
            }
        }

        private void UpdateInventoryDisplay(BoosterType type, int amount)
        {
            switch (type)
            {
                case BoosterType.InstantExit:
                    if (instantExitInventoryText != null)
                    {
                        instantExitInventoryText.text = $"Owned: {amount}";
                    }
                    break;

                case BoosterType.ExtraSlot:
                    if (extraSlotInventoryText != null)
                    {
                        extraSlotInventoryText.text = $"Owned: {amount}";
                    }
                    break;
            }
        }

        private void UpdateButtonStates()
        {
            if (shopManager == null) return;

            bool canAfford = shopManager.CanAfford();

            if (buyInstantExitButton != null)
            {
                buyInstantExitButton.interactable = canAfford;
            }

            if (buyExtraSlotButton != null)
            {
                buyExtraSlotButton.interactable = canAfford;
            }
        }

        private void OnPurchaseBooster(BoosterType type)
        {
            if (shopManager == null)
            {
                ShowFeedback("Shop unavailable!", false);
                return;
            }

            if (shopManager.PurchaseBooster(type))
            {
                ShowFeedback($"Purchased {type}!", true);
            }
            else
            {
                ShowFeedback("Not enough coins!", false);
            }
        }

        private void ShowFeedback(string message, bool isSuccess)
        {
            if (feedbackText == null) return;

            feedbackText.text = message;
            feedbackText.color = isSuccess ? Color.green : Color.red;
            feedbackText.gameObject.SetActive(true);

            CancelInvoke(nameof(HideFeedback));
            Invoke(nameof(HideFeedback), feedbackDisplayDuration);
        }

        private void HideFeedback()
        {
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
        }
    }
}
