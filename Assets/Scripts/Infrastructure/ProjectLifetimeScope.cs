using VContainer;
using VContainer.Unity;
using ArrowBlast.Interfaces;
using ArrowBlast.Managers;
using UnityEngine;

namespace ArrowBlast.Infrastructure
{
    /// <summary>
    /// Project-level lifetime scope - persists across all scenes
    /// Registers core services that should exist throughout the game lifetime
    /// </summary>
    public class ProjectLifetimeScope : LifetimeScope
    {
        [Header("Manager References")]
        [SerializeField] private SettingsManager settingsManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private CoinSystem coinSystem;
        [SerializeField] private BoosterInventory boosterInventory;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private ShopManager shopManager;
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private AdsManager adsManager;
        [SerializeField] private LivesManager livesManager;
        [SerializeField] private IAPManager iapManager;

        protected override void Configure(IContainerBuilder builder)
        {
            // Settings Service
            builder.Register<ISettingsService>(resolver =>
            {
                if (settingsManager == null) settingsManager = FindObjectOfType<SettingsManager>();
                return settingsManager;
            }, Lifetime.Singleton);

            // Audio Service
            builder.Register<IAudioService>(resolver =>
            {
                if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();
                if (audioManager != null) audioManager.Initialize(resolver.Resolve<ISettingsService>());
                return audioManager;
            }, Lifetime.Singleton);

            // Coin Service
            builder.Register<ICoinService>(resolver =>
            {
                if (coinSystem == null) coinSystem = FindObjectOfType<CoinSystem>();
                return coinSystem;
            }, Lifetime.Singleton);

            // Booster Inventory
            builder.Register<IBoosterInventory>(resolver =>
            {
                if (boosterInventory == null) boosterInventory = FindObjectOfType<BoosterInventory>();
                return boosterInventory;
            }, Lifetime.Singleton);

            // Level Progress Service
            builder.Register<ILevelProgressService>(resolver =>
            {
                if (levelManager == null) levelManager = FindObjectOfType<LevelManager>();
                return levelManager;
            }, Lifetime.Singleton);

            // Shop Service
            builder.Register<IShopService>(resolver =>
            {
                if (shopManager == null) shopManager = FindObjectOfType<ShopManager>();
                if (shopManager != null) shopManager.Initialize(
                    resolver.Resolve<ICoinService>(),
                    resolver.Resolve<IBoosterInventory>()
                );
                return shopManager;
            }, Lifetime.Singleton);

            // Tutorial Service
            builder.Register<ITutorialService>(resolver =>
            {
                if (tutorialManager == null) tutorialManager = FindObjectOfType<TutorialManager>();
                if (tutorialManager != null) tutorialManager.Initialize(resolver.Resolve<IBoosterInventory>());
                return tutorialManager;
            }, Lifetime.Singleton);

            // Ads Service
            builder.Register<IAdsService>(resolver =>
            {
                if (adsManager == null) adsManager = FindObjectOfType<AdsManager>();
                if (adsManager != null) adsManager.Initialize(resolver.Resolve<ILivesService>());
                return adsManager;
            }, Lifetime.Singleton);

            // Lives Service
            builder.Register<ILivesService>(resolver =>
            {
                if (livesManager == null) livesManager = FindObjectOfType<LivesManager>();
                return livesManager;
            }, Lifetime.Singleton);

            // IAP Service
            builder.Register<IIAPService>(resolver =>
            {
                if (iapManager == null) iapManager = FindObjectOfType<IAPManager>();
                if (iapManager != null) iapManager.Initialize(
                    resolver.Resolve<ICoinService>(),
                    resolver.Resolve<IAdsService>()
                );
                return iapManager;
            }, Lifetime.Singleton);
        }
    }
}
