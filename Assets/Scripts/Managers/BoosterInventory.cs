using UnityEngine;
using System;

namespace ArrowBlast.Managers
{
    public enum BoosterType
    {
        InstantExit,
        ExtraSlot
    }

    /// <summary>
    /// Manages the player's booster inventory with persistence
    /// </summary>
    public class BoosterInventory : MonoBehaviour
    {
        public static BoosterInventory Instance { get; private set; }

        private const string INSTANT_EXIT_KEY = "Booster_InstantExit";
        private const string EXTRA_SLOT_KEY = "Booster_ExtraSlot";

        private int instantExitCount;
        private int extraSlotCount;

        public event Action<BoosterType, int> OnInventoryChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            LoadInventory();
        }

        /// <summary>
        /// Get the amount of a specific booster type
        /// </summary>
        public int GetAmount(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.InstantExit:
                    return instantExitCount;
                case BoosterType.ExtraSlot:
                    return extraSlotCount;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Add a booster to the inventory
        /// </summary>
        public void AddBooster(BoosterType type, int amount = 1)
        {
            if (amount <= 0) return;

            switch (type)
            {
                case BoosterType.InstantExit:
                    instantExitCount += amount;
                    SaveBooster(INSTANT_EXIT_KEY, instantExitCount);
                    OnInventoryChanged?.Invoke(type, instantExitCount);
                    break;
                case BoosterType.ExtraSlot:
                    extraSlotCount += amount;
                    SaveBooster(EXTRA_SLOT_KEY, extraSlotCount);
                    OnInventoryChanged?.Invoke(type, extraSlotCount);
                    break;
            }

            Debug.Log($"[BoosterInventory] Added {amount} {type}. New count: {GetAmount(type)}");
        }

        /// <summary>
        /// Try to use a booster. Returns true if successful, false if none available
        /// </summary>
        public bool UseBooster(BoosterType type)
        {
            int currentAmount = GetAmount(type);
            if (currentAmount <= 0)
            {
                Debug.LogWarning($"[BoosterInventory] No {type} available");
                return false;
            }

            switch (type)
            {
                case BoosterType.InstantExit:
                    instantExitCount--;
                    SaveBooster(INSTANT_EXIT_KEY, instantExitCount);
                    OnInventoryChanged?.Invoke(type, instantExitCount);
                    break;
                case BoosterType.ExtraSlot:
                    extraSlotCount--;
                    SaveBooster(EXTRA_SLOT_KEY, extraSlotCount);
                    OnInventoryChanged?.Invoke(type, extraSlotCount);
                    break;
            }

            Debug.Log($"[BoosterInventory] Used {type}. Remaining: {GetAmount(type)}");
            return true;
        }

        private void LoadInventory()
        {
            instantExitCount = PlayerPrefs.GetInt(INSTANT_EXIT_KEY, 0);
            extraSlotCount = PlayerPrefs.GetInt(EXTRA_SLOT_KEY, 0);
            Debug.Log($"[BoosterInventory] Loaded - InstantExit: {instantExitCount}, ExtraSlot: {extraSlotCount}");
        }

        private void SaveBooster(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Reset all boosters to zero (for testing/debugging)
        /// </summary>
        public void ResetInventory()
        {
            instantExitCount = 0;
            extraSlotCount = 0;
            SaveBooster(INSTANT_EXIT_KEY, 0);
            SaveBooster(EXTRA_SLOT_KEY, 0);
            OnInventoryChanged?.Invoke(BoosterType.InstantExit, 0);
            OnInventoryChanged?.Invoke(BoosterType.ExtraSlot, 0);
        }
    }
}
