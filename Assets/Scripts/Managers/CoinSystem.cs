using UnityEngine;
using System;
using ArrowBlast.Interfaces;

namespace ArrowBlast.Managers
{
    /// <summary>
    /// Manages the player's coin balance with persistence
    /// </summary>
    public class CoinSystem : MonoBehaviour, ICoinService
    {
        private const string COIN_KEY = "PlayerCoins";
        private int currentBalance;

        public event Action<int> OnBalanceChanged;

        private void Awake()
        {
            LoadBalance();
        }

        /// <summary>
        /// Get the current coin balance
        /// </summary>
        public int GetBalance()
        {
            return currentBalance;
        }

        /// <summary>
        /// Add coins to the player's balance
        /// </summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            currentBalance += amount;
            SaveBalance();
            OnBalanceChanged?.Invoke(currentBalance);
            Debug.Log($"[CoinSystem] Added {amount} coins. New balance: {currentBalance}");
        }

        /// <summary>
        /// Try to spend coins. Returns true if successful, false if insufficient balance
        /// </summary>
        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return false;
            if (currentBalance < amount)
            {
                Debug.LogWarning($"[CoinSystem] Insufficient coins. Need {amount}, have {currentBalance}");
                return false;
            }

            currentBalance -= amount;
            SaveBalance();
            OnBalanceChanged?.Invoke(currentBalance);
            Debug.Log($"[CoinSystem] Spent {amount} coins. New balance: {currentBalance}");
            return true;
        }

        private void LoadBalance()
        {
            currentBalance = PlayerPrefs.GetInt(COIN_KEY, 0);
            Debug.Log($"[CoinSystem] Loaded balance: {currentBalance}");
        }

        private void SaveBalance()
        {
            PlayerPrefs.SetInt(COIN_KEY, currentBalance);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Reset coins to zero (for testing/debugging)
        /// </summary>
        public void ResetCoins()
        {
            currentBalance = 0;
            SaveBalance();
            OnBalanceChanged?.Invoke(currentBalance);
        }
    }
}
