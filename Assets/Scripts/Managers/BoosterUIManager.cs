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
                instantExitButton.onClick.AddListener(() => gameManager.ToggleInstantExitBooster());
                UpdateInstantExitVisual(false); // Reset to normal
            }

            if (extraSlotButton != null)
            {
                extraSlotButton.onClick.RemoveAllListeners();
                extraSlotButton.onClick.AddListener(() => gameManager.UseExtraSlotBooster());
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
                    if (instantExitButton != null)
                    {
                        instantExitButton.interactable = amount > 0;
                    }
                    break;

                case BoosterType.ExtraSlot:
                    if (extraSlotAmountText != null)
                    {
                        extraSlotAmountText.text = amount.ToString();
                    }
                    if (extraSlotButton != null)
                    {
                        extraSlotButton.interactable = amount > 0;
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
                UpdateAllDisplays();
            }
        }
    }
}
