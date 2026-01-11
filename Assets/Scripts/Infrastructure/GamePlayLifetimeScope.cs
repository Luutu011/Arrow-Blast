using VContainer;
using VContainer.Unity;
using ArrowBlast.Interfaces;
using ArrowBlast.Managers;
using ArrowBlast.UI;
using UnityEngine;

namespace ArrowBlast.Infrastructure
{
    /// <summary>
    /// Lifetime scope for the main GamePlayScene - handles both menu and game UI controllers.
    /// In Unity, make sure to drag your UI objects into the corresponding slots in the Inspector.
    /// </summary>
    public class GamePlayLifetimeScope : LifetimeScope
    {
        [Header("Menu References")]
        [SerializeField] private MainMenu mainMenu;
        [SerializeField] private ShopUI shopUI;

        [Header("Game References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BoosterUIManager boosterUIManager;
        [SerializeField] private GameEndUIManager gameEndUIManager;

        protected override void Configure(IContainerBuilder builder)
        {
            // --- Menu Controllers ---
            // These handle the Main Menu and Shop functionality
            if (mainMenu != null)
            {
                builder.RegisterInstance(mainMenu);
                builder.RegisterBuildCallback(container =>
                {
                    mainMenu.Initialize(
                        container.Resolve<ILevelProgressService>(),
                        container.Resolve<IGameManager>(),
                        container.Resolve<ISettingsService>(),
                        container.Resolve<IAudioService>(),
                        container.Resolve<IBoosterInventory>()
                    );
                });
            }

            if (shopUI != null)
            {
                builder.RegisterInstance(shopUI);
                builder.RegisterBuildCallback(container =>
                {
                    shopUI.Initialize(
                        container.Resolve<IShopService>(),
                        container.Resolve<ICoinService>(),
                        container.Resolve<IBoosterInventory>(),
                        container.Resolve<IIAPService>()
                    );
                });
            }

            // --- Game Controllers ---
            // These handle gameplay, booster buttons in-game, and win/loss screens
            if (gameManager != null)
            {
                builder.RegisterInstance<IGameManager>(gameManager);
                builder.RegisterBuildCallback(container =>
                {
                    gameManager.Initialize(
                        container.Resolve<IAudioService>(),
                        container.Resolve<ILevelProgressService>(),
                        container.Resolve<ICoinService>(),
                        container.Resolve<IBoosterInventory>(),
                        container.Resolve<ITutorialService>()
                    );
                });
            }

            if (boosterUIManager != null)
            {
                builder.RegisterInstance(boosterUIManager);
                builder.RegisterBuildCallback(container =>
                {
                    boosterUIManager.Initialize(
                        container.Resolve<IGameManager>(),
                        container.Resolve<ICoinService>(),
                        container.Resolve<IBoosterInventory>(),
                        container.Resolve<IShopService>(),
                        container.Resolve<ILevelProgressService>()
                    );
                });
            }

            if (gameEndUIManager != null)
            {
                builder.RegisterInstance(gameEndUIManager);
                builder.RegisterBuildCallback(container =>
                {
                    gameEndUIManager.Initialize(
                        container.Resolve<IGameManager>(),
                        container.Resolve<ICoinService>(),
                        container.Resolve<IAdsService>()
                    );
                });
            }
        }
    }
}
