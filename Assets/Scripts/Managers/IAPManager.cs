using UnityEngine;
using UnityEngine.Purchasing;
using System;

namespace ArrowBlast.Managers
{
    /// <summary>
    /// Handles In-App Purchases using Unity IAP.
    /// Supports Consumables (Coins) and Non-Consumables (Remove Ads).
    /// </summary>
    public class IAPManager : MonoBehaviour, IStoreListener
    {
        public static IAPManager Instance { get; private set; }

        private static IStoreController m_StoreController;
        private static IExtensionProvider m_StoreExtensionProvider;

        [Header("Product IDs")]
        public string productRemoveAds = "com.arrowblast.removeads";
        public string productAddCoins100 = "com.arrowblast.coins100";
        public string productAddCoins500 = "com.arrowblast.coins500";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePurchasing();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializePurchasing()
        {
            if (IsInitialized()) return;

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Add products to the builder
            builder.AddProduct(productRemoveAds, ProductType.NonConsumable);
            builder.AddProduct(productAddCoins100, ProductType.Consumable);
            builder.AddProduct(productAddCoins500, ProductType.Consumable);

            UnityPurchasing.Initialize(this, builder);
        }

        public bool IsInitialized()
        {
            return m_StoreController != null && m_StoreExtensionProvider != null;
        }

        public void BuyRemoveAds()
        {
            BuyProductID(productRemoveAds);
        }

        public void BuyCoins100()
        {
            BuyProductID(productAddCoins100);
        }

        public void BuyCoins500()
        {
            BuyProductID(productAddCoins500);
        }

        private void BuyProductID(string productId)
        {
            if (IsInitialized())
            {
                Product product = m_StoreController.products.WithID(productId);

                if (product != null && product.availableToPurchase)
                {
                    Debug.Log(string.Format("Purchasing product asynchroniously: '{0}'", product.definition.id));
                    m_StoreController.InitiatePurchase(product);
                }
                else
                {
                    Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
                }
            }
            else
            {
                Debug.Log("BuyProductID FAIL. Not initialized.");
            }
        }

        // --- IStoreListener Callbacks ---

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("OnInitialized: PASS");
            m_StoreController = controller;
            m_StoreExtensionProvider = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.Log("OnInitializeFailed InitializationFailureReason:" + error + ". Message: " + message);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            if (String.Equals(args.purchasedProduct.definition.id, productRemoveAds, StringComparison.Ordinal))
            {
                Debug.Log("ProcessPurchase: PASS. Product: " + productRemoveAds);
                OnPurchaseRemoveAds();
            }
            else if (String.Equals(args.purchasedProduct.definition.id, productAddCoins100, StringComparison.Ordinal))
            {
                Debug.Log("ProcessPurchase: PASS. Product: " + productAddCoins100);
                OnPurchaseCoins(100);
            }
            else if (String.Equals(args.purchasedProduct.definition.id, productAddCoins500, StringComparison.Ordinal))
            {
                Debug.Log("ProcessPurchase: PASS. Product: " + productAddCoins500);
                OnPurchaseCoins(500);
            }
            else
            {
                Debug.Log(string.Format("ProcessPurchase: TYPE NOT RECOGNIZED. Product: '{0}'", args.purchasedProduct.definition.id));
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        }

        // --- Purchase Handlers ---

        private void OnPurchaseRemoveAds()
        {
            if (AdsManager.Instance != null)
            {
                AdsManager.Instance.DisableAdsPermanently();
            }
        }

        private void OnPurchaseCoins(int amount)
        {
            CoinSystem coinSystem = FindObjectOfType<CoinSystem>();
            if (coinSystem != null)
            {
                coinSystem.AddCoins(amount);
            }
        }
    }
}
