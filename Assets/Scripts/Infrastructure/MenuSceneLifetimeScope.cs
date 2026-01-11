using VContainer;
using VContainer.Unity;
using ArrowBlast.Interfaces;
using ArrowBlast.UI;
using UnityEngine;

namespace ArrowBlast.Infrastructure
{
    /// <summary>
    /// Menu scene lifetime scope - created/destroyed with menu scene
    /// Registers menu-specific UI controllers
    /// </summary>
    public class MenuSceneLifetimeScope : LifetimeScope
    {
        [Header("Scene References")]
        [SerializeField] private MainMenu mainMenu;
        [SerializeField] private ArrowBlast.Managers.ShopUI shopUI;

        protected override void Configure(IContainerBuilder builder)
        {
            // Main Menu Controller
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

            // Shop UI Controller
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
        }
    }
}
