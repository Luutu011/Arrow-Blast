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
        protected override void Configure(IContainerBuilder builder)
        {
            // Settings Service (no dependencies)
            builder.Register<ISettingsService>(resolver =>
            {
                var settingsManager = FindObjectOfType<SettingsManager>();
                if (settingsManager == null)
                {
                    var go = new UnityEngine.GameObject("SettingsManager");
                    settingsManager = go.AddComponent<SettingsManager>();
                }
                return settingsManager;
            }, Lifetime.Singleton);

            // Audio Service (depends on Settings)
            builder.Register<IAudioService>(resolver =>
            {
                var audioManager = FindObjectOfType<AudioManager>();
                if (audioManager == null)
                {
                    var go = new UnityEngine.GameObject("AudioManager");
                    audioManager = go.AddComponent<AudioManager>();
                }
                // Inject settings via method
                audioManager.Initialize(resolver.Resolve<ISettingsService>());
                return audioManager;
            }, Lifetime.Singleton);

            // Coin Service (no dependencies)
            builder.Register<ICoinService>(resolver =>
            {
                var coinSystem = FindObjectOfType<CoinSystem>();
                if (coinSystem == null)
                {
                    var go = new UnityEngine.GameObject("CoinSystem");
                    coinSystem = go.AddComponent<CoinSystem>();
                }
                return coinSystem;
            }, Lifetime.Singleton);

            // Booster Inventory (no dependencies)
            builder.Register<IBoosterInventory>(resolver =>
            {
                var boosterInventory = FindObjectOfType<BoosterInventory>();
                if (boosterInventory == null)
                {
                    var go = new UnityEngine.GameObject("BoosterInventory");
                    boosterInventory = go.AddComponent<BoosterInventory>();
                }
                return boosterInventory;
            }, Lifetime.Singleton);

            // Level Progress Service (no dependencies)
            builder.Register<ILevelProgressService>(resolver =>
            {
                var levelManager = FindObjectOfType<LevelManager>();
                if (levelManager == null)
                {
                    var go = new UnityEngine.GameObject("LevelManager");
                    levelManager = go.AddComponent<LevelManager>();
                }
                return levelManager;
            }, Lifetime.Singleton);

            // Shop Service (depends on Coins and Boosters)
            builder.Register<IShopService>(resolver =>
            {
                var shopManager = FindObjectOfType<ShopManager>();
                if (shopManager == null)
                {
                    var go = new UnityEngine.GameObject("ShopManager");
                    shopManager = go.AddComponent<ShopManager>();
                }
                shopManager.Initialize(
                    resolver.Resolve<ICoinService>(),
                    resolver.Resolve<IBoosterInventory>()
                );
                return shopManager;
            }, Lifetime.Singleton);

            // Tutorial Service (depends on Boosters)
            builder.Register<ITutorialService>(resolver =>
            {
                var tutorialManager = FindObjectOfType<TutorialManager>();
                if (tutorialManager == null)
                {
                    var go = new UnityEngine.GameObject("TutorialManager");
                    tutorialManager = go.AddComponent<TutorialManager>();
                }
                tutorialManager.Initialize(resolver.Resolve<IBoosterInventory>());
                return tutorialManager;
            }, Lifetime.Singleton);

            // Ads Service (no dependencies - but may have internal Unity Services dependencies)
            builder.Register<IAdsService>(resolver =>
            {
                var adsManager = FindObjectOfType<AdsManager>();
                if (adsManager != null)
                {
                    adsManager.Initialize(resolver.Resolve<ILivesService>());
                    return adsManager;
                }
                return null; // Ads are optional
            }, Lifetime.Singleton);

            // Lives Service (no dependencies)
            builder.Register<ILivesService>(resolver =>
            {
                var livesManager = FindObjectOfType<LivesManager>();
                if (livesManager == null)
                {
                    var go = new UnityEngine.GameObject("LivesManager");
                    livesManager = go.AddComponent<LivesManager>();
                }
                return livesManager;
            }, Lifetime.Singleton);

            // IAP Service (depends on Coins and Ads)
            builder.Register<IIAPService>(resolver =>
            {
                var iapManager = FindObjectOfType<IAPManager>();
                if (iapManager == null)
                {
                    var go = new UnityEngine.GameObject("IAPManager");
                    iapManager = go.AddComponent<IAPManager>();
                }
                iapManager.Initialize(
                    resolver.Resolve<ICoinService>(),
                    resolver.Resolve<IAdsService>()
                );
                return iapManager;
            }, Lifetime.Singleton);
        }
    }
}
