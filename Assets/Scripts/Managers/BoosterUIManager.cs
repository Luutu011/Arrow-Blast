using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArrowBlast.Managers
{
    public class BoosterUIManager : MonoBehaviour
    {
        [Header("Booster Buttons")]
        [SerializeField] private Button instantExitButton;
        [SerializeField] private Button extraSlotButton;

        [Header("Inventory Display")]
        [SerializeField] private TextMeshProUGUI instantExitAmountText;
        [SerializeField] private TextMeshProUGUI extraSlotAmountText;

        [Header("Coin Display")]
        [SerializeField] private TextMeshProUGUI coinBalanceText;

        [Header("Purchase Prompt")]
        [SerializeField] private GameObject purchasePromptPanel;
        [SerializeField] private TextMeshProUGUI promptMessageText;
        [SerializeField] private Button confirmPurchaseButton;
        [SerializeField] private Button cancelPurchaseButton;

        private BoosterType pendingPurchaseType;

        private GameManager gameManager;
        private CoinSystem coinSystem;
        private BoosterInventory boosterInventory;

        public void Initialize(GameManager manager, CoinSystem coins, BoosterInventory inventory)
        {
            gameManager = manager;
            coinSystem = coins;
            boosterInventory = inventory;

            if (instantExitButton != null)
            {
                instantExitButton.onClick.RemoveAllListeners();
                instantExitButton.onClick.AddListener(() => OnBoosterButtonClicked(BoosterType.InstantExit));
                UpdateInstantExitVisual(false); // Reset to normal
            }

            if (extraSlotButton != null)
            {
                extraSlotButton.onClick.RemoveAllListeners();
                extraSlotButton.onClick.AddListener(() => OnBoosterButtonClicked(BoosterType.ExtraSlot));
            }

            // Setup purchase prompt buttons
            if (confirmPurchaseButton != null)
            {
                confirmPurchaseButton.onClick.RemoveAllListeners();
                confirmPurchaseButton.onClick.AddListener(OnConfirmPurchase);
            }

            if (cancelPurchaseButton != null)
            {
                cancelPurchaseButton.onClick.RemoveAllListeners();
                cancelPurchaseButton.onClick.AddListener(OnCancelPurchase);
            }

            // Hide prompt on start
            if (purchasePromptPanel != null)
            {
                purchasePromptPanel.SetActive(false);
            }

            // Subscribe to inventory changes
            if (boosterInventory != null)
            {
                boosterInventory.OnInventoryChanged += OnInventoryChanged;
            }

            // Subscribe to coin changes
            if (coinSystem != null)
            {
                coinSystem.OnBalanceChanged += OnCoinBalanceChanged;
            }

            // Initial update
            UpdateBoosterUnlockStatus();
            UpdateAllDisplays();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (boosterInventory != null)
            {
                boosterInventory.OnInventoryChanged -= OnInventoryChanged;
            }

            if (coinSystem != null)
            {
                coinSystem.OnBalanceChanged -= OnCoinBalanceChanged;
            }
        }

        private void OnInventoryChanged(BoosterType type, int newAmount)
        {
            UpdateBoosterDisplay(type, newAmount);
        }

        private void OnCoinBalanceChanged(int newBalance)
        {
            UpdateCoinDisplay(newBalance);
        }

        private void UpdateAllDisplays()
        {
            if (boosterInventory != null)
            {
                UpdateBoosterDisplay(BoosterType.InstantExit, boosterInventory.GetAmount(BoosterType.InstantExit));
                UpdateBoosterDisplay(BoosterType.ExtraSlot, boosterInventory.GetAmount(BoosterType.ExtraSlot));
            }

            if (coinSystem != null)
            {
                UpdateCoinDisplay(coinSystem.GetBalance());
            }
        }

        private void UpdateBoosterDisplay(BoosterType type, int amount)
        {
            switch (type)
            {
                case BoosterType.InstantExit:
                    if (instantExitAmountText != null)
                    {
                        instantExitAmountText.text = amount.ToString();
                    }
                    break;

                case BoosterType.ExtraSlot:
                    if (extraSlotAmountText != null)
                    {
                        extraSlotAmountText.text = amount.ToString();
                    }
                    break;
            }
        }

        private void UpdateCoinDisplay(int balance)
        {
            if (coinBalanceText != null)
            {
                coinBalanceText.text = balance.ToString();
            }
        }

        public void UpdateInstantExitVisual(bool isActive)
        {
            if (instantExitButton == null) return;

            ColorBlock cb = instantExitButton.colors;
            Color targetColor = isActive ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;

            cb.normalColor = targetColor;
            cb.selectedColor = targetColor;
            instantExitButton.colors = cb;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (visible)
            {
                UpdateBoosterUnlockStatus();
                UpdateAllDisplays();
            }
        }

        private void UpdateBoosterUnlockStatus()
        {
            LevelManager levelManager = FindAnyObjectByType<LevelManager>();
            if (levelManager == null) return;

            int currentLevel = levelManager.CurrentLevelIndex; // 0-based

            // InstantExit unlock after level 5 (Level 6+)
            bool instantExitUnlocked = currentLevel >= 5;
            UpdateBoosterButtonState(instantExitButton, instantExitUnlocked);

            // ExtraSlot unlock after level 10 (Level 11+)
            bool extraSlotUnlocked = currentLevel >= 10;
            UpdateBoosterButtonState(extraSlotButton, extraSlotUnlocked);
        }

        private void UpdateBoosterButtonState(Button button, bool unlocked)
        {
            if (button == null) return;

            button.interactable = unlocked;

            // Grey out effect: Adjust alpha or color of the button image/icon
            Image img = button.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = unlocked ? 1.0f : 0.4f; // Semi-transparent when locked
                img.color = c;
            }

            // Also grey out the icon if it's a child
            foreach (Image childImg in button.GetComponentsInChildren<Image>())
            {
                if (childImg == img) continue;
                Color c = childImg.color;
                c.a = unlocked ? 1.0f : 0.4f;
                childImg.color = c;
            }
        }

        private void OnBoosterButtonClicked(BoosterType type)
        {
            AudioManager.Instance.TriggerHaptic(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.SoftImpact);
            if (boosterInventory == null) return;

            // Check if player has the booster
            if (boosterInventory.GetAmount(type) > 0)
            {
                // Use the booster normally
                if (type == BoosterType.InstantExit)
                {
                    gameManager.ToggleInstantExitBooster();
                }
                else if (type == BoosterType.ExtraSlot)
                {
                    gameManager.UseExtraSlotBooster();
                }
            }
            else
            {
                // Show purchase prompt
                ShowPurchasePrompt(type);
            }
        }

        private void ShowPurchasePrompt(BoosterType type)
        {
            if (purchasePromptPanel == null) return;

            pendingPurchaseType = type;

            ShopManager shopManager = FindAnyObjectByType<ShopManager>();
            int cost = shopManager != null ? shopManager.GetBoosterCost() : 50;
            int currentCoins = coinSystem != null ? coinSystem.GetBalance() : 0;
            bool canAfford = currentCoins >= cost;

            if (promptMessageText != null)
            {
                promptMessageText.text = $"You don't have {type}!\nBuy for {cost} coins?";
            }

            if (confirmPurchaseButton != null)
            {
                confirmPurchaseButton.interactable = canAfford;
            }

            purchasePromptPanel.SetActive(true);
        }

        private void OnConfirmPurchase()
        {
            AudioManager.Instance.TriggerHaptic(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.SoftImpact);
            ShopManager shopManager = FindAnyObjectByType<ShopManager>();
            if (shopManager != null)
            {
                if (shopManager.PurchaseBooster(pendingPurchaseType))
                {
                    // Successfully purchased, close prompt
                    if (purchasePromptPanel != null)
                    {
                        purchasePromptPanel.SetActive(false);
                    }
                }
                else
                {
                    // Not enough coins
                    if (promptMessageText != null)
                    {
                        promptMessageText.text = "Not enough coins!";
                    }
                }
            }
        }

        private void OnCancelPurchase()
        {
            AudioManager.Instance.TriggerHaptic(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.SoftImpact);
            if (purchasePromptPanel != null)
            {
                purchasePromptPanel.SetActive(false);
            }
        }
    }
}
