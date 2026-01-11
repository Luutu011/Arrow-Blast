using VContainer;
using VContainer.Unity;
using ArrowBlast.Interfaces;
using ArrowBlast.Managers;
using UnityEngine;

namespace ArrowBlast.Infrastructure
{
    /// <summary>
    /// Game scene lifetime scope - created/destroyed with game scene
    /// Registers gameplay-specific components and UI controllers
    /// </summary>
    public class GameSceneLifetimeScope : LifetimeScope
    {
        [Header("Scene References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BoosterUIManager boosterUIManager;
        [SerializeField] private ArrowBlast.UI.GameEndUIManager gameEndUIManager;

        protected override void Configure(IContainerBuilder builder)
        {
            // Game Manager - inject all required services
            if (gameManager != null)
            {
                builder.RegisterInstance<IGameManager>(gameManager);

                // Initialize GameManager with injected dependencies
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

            // Booster UI Manager
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

            // Game End UI Manager
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
