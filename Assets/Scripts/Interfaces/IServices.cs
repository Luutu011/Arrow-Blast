using System;
using ArrowBlast.Data;
using ArrowBlast.Managers;

namespace ArrowBlast.Interfaces
{
    /// <summary>
    /// Audio service for playing music, sound effects, and haptic feedback
    /// </summary>
    public interface IAudioService
    {
        void PlayMusic(string name);
        void PlaySfx(string name);
        void TriggerHaptic();
        void UpdateSettings();
    }

    /// <summary>
    /// Settings service for managing user preferences
    /// </summary>
    public interface ISettingsService
    {
        bool SfxEnabled { get; }
        bool MusicEnabled { get; }
        bool HapticEnabled { get; }
        void SetSfx(bool enabled);
        void SetMusic(bool enabled);
        void SetHaptic(bool enabled);
    }

    /// <summary>
    /// Coin economy service
    /// </summary>
    public interface ICoinService
    {
        int GetBalance();
        void AddCoins(int amount);
        bool SpendCoins(int amount);
        event Action<int> OnBalanceChanged;
    }

    /// <summary>
    /// Booster inventory management
    /// </summary>
    public interface IBoosterInventory
    {
        int GetAmount(BoosterType type);
        void AddBooster(BoosterType type, int amount = 1);
        bool UseBooster(BoosterType type);
        event Action<BoosterType, int> OnInventoryChanged;
    }

    /// <summary>
    /// Level progression and unlock management
    /// </summary>
    public interface ILevelProgressService
    {
        int CurrentLevelIndex { get; }
        bool IsLevelUnlocked(int levelIndex);
        int GetHighestUnlockedLevel();
        void UnlockNextLevel();
        LevelData GetCurrentLevel();
        LevelData GetLevel(int index);
        int GetLevelCount();
        void SetLevelIndex(int index);
        bool AdvanceLevel();
    }

    /// <summary>
    /// Tutorial and onboarding service
    /// </summary>
    public interface ITutorialService
    {
        void CheckTutorials(int currentLevelIndex);
        void HideTutorial();
    }

    /// <summary>
    /// Advertisement service
    /// </summary>
    public interface IAdsService
    {
        void ShowRewardedAd(Action onSuccess, Action onFailed = null);
        void ShowInterstitialAd(Action onClosed = null);
        bool IsRewardedAdReady();
        void DisableAdsPermanently();
    }

    /// <summary>
    /// In-App Purchase service
    /// </summary>
    public interface IIAPService
    {
        void BuyRemoveAds();
        void BuyCoins100();
        void BuyCoins500();
    }

    /// <summary>
    /// Shop and purchase service
    /// </summary>
    public interface IShopService
    {
        int GetBoosterCost();
        bool PurchaseBooster(BoosterType type);
        bool CanAfford();
    }

    /// <summary>
    /// Core game manager interface for UI controllers
    /// </summary>
    public interface IGameManager
    {
        void ToggleInstantExitBooster();
        void UseExtraSlotBooster();
        void RestartWithExtraSlot();
        void RestartLevel();
        void ReturnToLevelSelect();
    }
    /// <summary>
    /// Lives and hearts system service
    /// </summary>
    public interface ILivesService
    {
        int GetCurrentHearts();
        int GetMaxHearts();
        int GetSecondsUntilNextHeart();
        bool CanStartLevel();
        void DeductHeart();
        void AddHeart(int count);
    }
}
