using UnityEngine;

namespace ArrowBlast.Managers
{
    /// <summary>
    /// Handles booster purchases from the shop
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        private const int BOOSTER_COST = 50;

        [Header("Dependencies")]
        [SerializeField] private CoinSystem coinSystem;
        [SerializeField] private BoosterInventory boosterInventory;

        private void Awake()
        {
            // Auto-find if not assigned
            if (coinSystem == null) coinSystem = FindObjectOfType<CoinSystem>();
            if (boosterInventory == null) boosterInventory = FindObjectOfType<BoosterInventory>();
        }

        /// <summary>
        /// Attempt to purchase a booster with coins
        /// </summary>
        public bool PurchaseBooster(BoosterType type)
        {
            if (coinSystem == null || boosterInventory == null)
            {
                Debug.LogError("[ShopManager] Missing dependencies!");
                return false;
            }

            // Check if player has enough coins
            if (coinSystem.GetBalance() < BOOSTER_COST)
            {
                Debug.LogWarning($"[ShopManager] Cannot purchase {type}. Need {BOOSTER_COST} coins, have {coinSystem.GetBalance()}");
                return false;
            }

            // Attempt to spend coins
            if (coinSystem.SpendCoins(BOOSTER_COST))
            {
                boosterInventory.AddBooster(type);
                Debug.Log($"[ShopManager] Purchased {type} for {BOOSTER_COST} coins");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the cost of a booster
        /// </summary>
        public int GetBoosterCost()
        {
            return BOOSTER_COST;
        }

        /// <summary>
        /// Check if player can afford a booster
        /// </summary>
        public bool CanAfford()
        {
            if (coinSystem == null) return false;
            return coinSystem.GetBalance() >= BOOSTER_COST;
        }
    }
}
