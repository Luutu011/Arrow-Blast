using UnityEngine;
using ArrowBlast.Interfaces;

namespace ArrowBlast.Managers
{
    /// <summary>
    /// Handles booster purchases from the shop
    /// </summary>
    public class ShopManager : MonoBehaviour, IShopService
    {
        private const int BOOSTER_COST = 50;

        private ICoinService _coinService;
        private IBoosterInventory _boosterInventory;

        // Called by VContainer
        public void Initialize(ICoinService coinService, IBoosterInventory boosterInventory)
        {
            _coinService = coinService;
            _boosterInventory = boosterInventory;
        }

        /// <summary>
        /// Attempt to purchase a booster with coins
        /// </summary>
        public bool PurchaseBooster(BoosterType type)
        {
            if (_coinService == null || _boosterInventory == null)
            {
                Debug.LogError("[ShopManager] Missing dependencies!");
                return false;
            }

            // Check if player has enough coins
            if (_coinService.GetBalance() < BOOSTER_COST)
            {
                Debug.LogWarning($"[ShopManager] Cannot purchase {type}. Need {BOOSTER_COST} coins, have {_coinService.GetBalance()}");
                return false;
            }

            // Attempt to spend coins
            if (_coinService.SpendCoins(BOOSTER_COST))
            {
                _boosterInventory.AddBooster(type);
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
            if (_coinService == null) return false;
            return _coinService.GetBalance() >= BOOSTER_COST;
        }
    }
}
