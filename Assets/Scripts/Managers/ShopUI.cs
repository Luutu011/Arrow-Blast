using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        [Header("Display")]
        [SerializeField] private TextMeshProUGUI coinBalanceText;
        [SerializeField] private TextMeshProUGUI boosterCostText;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private float feedbackDisplayDuration = 2f;

        private ShopManager shopManager;
        private CoinSystem coinSystem;
        private BoosterInventory boosterInventory;

        private void Awake()
        {
            // Auto-find dependencies
            shopManager = FindObjectOfType<ShopManager>();
            coinSystem = FindObjectOfType<CoinSystem>();
            boosterInventory = FindObjectOfType<BoosterInventory>();
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
            // Could add visual feedback here if needed
        }

        private void UpdateDisplay()
        {
            if (coinSystem != null)
            {
                UpdateCoinDisplay(coinSystem.GetBalance());
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
