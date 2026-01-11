using UnityEngine;
using System;
using Unity.Services.LevelPlay;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    // TODO: REPLACE WITH YOUR APP KEY
    [Tooltip("Put your IronSource App Key here")]
    [SerializeField] private string appKey = "YOUR_APP_KEY_HERE";

    // TODO: REPLACE WITH YOUR INTERSTITIAL AD UNIT ID
    [Tooltip("Put your Interstitial Ad Unit ID here")]
    [SerializeField] private string interstitialAdUnitId = "YOUR_INTERSTITIAL_AD_UNIT_ID";

    // TODO: REPLACE WITH YOUR REWARDED AD UNIT ID
    [Tooltip("Put your Rewarded Ad Unit ID here")]
    [SerializeField] private string rewardedAdUnitId = "YOUR_REWARDED_AD_UNIT_ID";

    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedAd;

    // Interstitial frequency control
    private const string LEVELS_SINCE_AD_KEY = "LevelsSinceLastAd";
    private int levelsSinceLastAd;

    private const string NO_ADS_KEY = "NoAdsPurchased";
    private bool noAdsPurchased = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            noAdsPurchased = PlayerPrefs.GetInt(NO_ADS_KEY, 0) == 1;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (noAdsPurchased)
        {
            Debug.Log("Ads are disabled for this session.");
            return;
        }

        // Create a persistent black background for the banner
        CreateBannerBackground();

        LevelPlay.Init(appKey, "UserId");

        // Register for Initialization events
        LevelPlay.OnInitSuccess += OnSdkInitSuccess;
        LevelPlay.OnInitFailed += OnSdkInitFailed;

        // Load interstitial frequency
        levelsSinceLastAd = PlayerPrefs.GetInt(LEVELS_SINCE_AD_KEY, 0);
    }

    public void DisableAdsPermanently()
    {
        noAdsPurchased = true;
        PlayerPrefs.SetInt(NO_ADS_KEY, 1);
        PlayerPrefs.Save();

        HideBannerAd();
        Debug.Log("Ads have been permanently disabled.");
    }

    public bool AreAdsDisabled() => noAdsPurchased;

    private void CreateBannerBackground()
    {
        // 1. Create a new GameObject for the Canvas
        GameObject backgroundObj = new GameObject("BannerBackgroundCanvas");
        backgroundObj.transform.SetParent(this.transform); // Make it child of AdsManager so it persists

        // 2. Add Canvas component
        Canvas canvas = backgroundObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; // Ensure it's on top of other Unity UI, but Banner Ad will be on top of this

        // 3. Add CanvasScaler (optional, but good practice)
        UnityEngine.UI.CanvasScaler scaler = backgroundObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        // 4. Create the Image (The Black Bar)
        GameObject panelObj = new GameObject("BlackBar");
        panelObj.transform.SetParent(backgroundObj.transform);

        UnityEngine.UI.Image image = panelObj.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.white;

        // 5. Set RectTransform to cover bottom 10%
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0, 0); // Bottom Left
        rect.anchorMax = new Vector2(1, 0.1f); // Top Right (at 10% height)
        rect.pivot = new Vector2(0.5f, 0); // Pivot at bottom center
        rect.offsetMin = Vector2.zero; // Zero out offsets
        rect.offsetMax = Vector2.zero;
    }

    // TODO: REPLACE WITH YOUR BANNER AD UNIT ID
    [Tooltip("Put your Banner Ad Unit ID here")]
    [SerializeField] private string bannerAdUnitId = "YOUR_BANNER_AD_UNIT_ID";

    private LevelPlayBannerAd bannerAd;

    private void OnSdkInitSuccess(LevelPlayConfiguration configuration)
    {
        Debug.Log("LevelPlay SDK Initialized Successfully");

        // Create and Load Interstitial Ad after initialization
        CreateInterstitialAd();
        LoadInterstitialAd();

        // Create and Load Banner Ad
        CreateBannerAd();
        LoadBannerAd();

        // Create and Load Rewarded Ad
        CreateRewardedAd();
        LoadRewardedAd();
    }

    private void OnSdkInitFailed(LevelPlayInitError error)
    {
        Debug.LogError("LevelPlay SDK Initialization Failed: " + error);
    }

    // --- Banner Ad Logic ---

    private void CreateBannerAd()
    {
        // Create Banner Ad object with customized parameters using Config.
        var configBuilder = new LevelPlayBannerAd.Config.Builder();
        configBuilder.SetSize(LevelPlayAdSize.BANNER); // Using Standard Banner size
        configBuilder.SetPosition(LevelPlayBannerPosition.BottomCenter);
        configBuilder.SetDisplayOnLoad(true);
        configBuilder.SetRespectSafeArea(false); // We handle safe area with our own padding/background
        configBuilder.SetPlacementName("BannerPlacement");
        // configBuilder.SetBidFloor(1.0); // Optional: Minimum bid price
        var bannerConfig = configBuilder.Build();

        // Create Banner Ad Object
        bannerAd = new LevelPlayBannerAd(bannerAdUnitId, bannerConfig);

        // Register for Banner Ad events
        bannerAd.OnAdLoaded += OnBannerLoaded;
        bannerAd.OnAdLoadFailed += OnBannerLoadFailed;
        bannerAd.OnAdDisplayed += OnBannerDisplayed;
        bannerAd.OnAdDisplayFailed += OnBannerDisplayFailed;
        bannerAd.OnAdClicked += OnBannerClicked;
        bannerAd.OnAdCollapsed += OnBannerCollapsed;
        bannerAd.OnAdExpanded += OnBannerExpanded;
        bannerAd.OnAdLeftApplication += OnBannerLeftApplication;
    }

    public void LoadBannerAd()
    {
        if (bannerAd != null)
        {
            bannerAd.LoadAd();
        }
    }

    public void HideBannerAd()
    {
        if (bannerAd != null)
        {
            bannerAd.HideAd();
        }
    }

    public void ShowBannerAd()
    {
        if (bannerAd != null)
        {
            bannerAd.ShowAd();
        }
    }

    // --- Banner Ad Events ---

    private void OnBannerLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Ad Loaded: " + adInfo);
        ShowBannerAd(); // Show immediately when loaded
    }

    private void OnBannerLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError("Banner Ad Load Failed: " + error);
    }

    private void OnBannerDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Ad Displayed: " + adInfo);
    }

    private void OnBannerDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError("Banner Ad Display Failed: " + error);
    }

    private void OnBannerClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Ad Clicked: " + adInfo);
    }

    private void OnBannerCollapsed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Ad Collapsed: " + adInfo);
    }

    private void OnBannerExpanded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Ad Expanded: " + adInfo);
    }

    private void OnBannerLeftApplication(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Ad Left Application: " + adInfo);
    }

    // --- Interstitial Ad Logic ---

    private void CreateInterstitialAd()
    {
        // Create the Interstitial Ad Object
        interstitialAd = new LevelPlayInterstitialAd(interstitialAdUnitId);

        // Register for Interstitial Ad events
        interstitialAd.OnAdLoaded += OnInterstitialLoaded;
        interstitialAd.OnAdLoadFailed += OnInterstitialLoadFailed;
        interstitialAd.OnAdDisplayed += OnInterstitialDisplayed;
        interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;
        interstitialAd.OnAdClosed += OnInterstitialClosed;
        interstitialAd.OnAdClicked += OnInterstitialClicked;
        interstitialAd.OnAdInfoChanged += OnInterstitialInfoChanged;
    }

    public void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.LoadAd();
        }
    }

    private Action onAdClosedCallback;

    public void ShowInterstitialAd(Action onClosed = null)
    {
        onAdClosedCallback = onClosed;

        // Increment counter on every attempt
        levelsSinceLastAd++;
        PlayerPrefs.SetInt(LEVELS_SINCE_AD_KEY, levelsSinceLastAd);
        PlayerPrefs.Save();

        // Check if enough levels have passed (first 3 are free)
        if (levelsSinceLastAd < 3)
        {
            Debug.Log($"Ad-free attempt {levelsSinceLastAd}/3");

            // FIX: If the ad isn't ready (e.g. failed at start), try loading it NOW 
            // instead of waiting for level 3 to notice.
            if (interstitialAd != null && !interstitialAd.IsAdReady())
            {
                LoadInterstitialAd();
            }

            onAdClosedCallback?.Invoke();
            onAdClosedCallback = null;
            return;
        }

        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("Interstitial Ad is not ready yet.");
            // If ad is not ready, execute the callback immediately so the game flow continues
            onAdClosedCallback?.Invoke();
            onAdClosedCallback = null;

            // Optional: Try to load again if not ready
            LoadInterstitialAd();
        }
    }

    // --- Interstitial Ad Events ---

    private void OnInterstitialLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad Loaded: " + adInfo);
    }

    private void OnInterstitialLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError("Interstitial Ad Load Failed: " + error);
    }

    private void OnInterstitialDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad Displayed: " + adInfo);
    }

    private void OnInterstitialDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError("Interstitial Ad Display Failed: " + error);
        // If display fails, ensure we continue the flow
        onAdClosedCallback?.Invoke();
        onAdClosedCallback = null;

        // Optional: Load a new ad
        LoadInterstitialAd();
    }

    private void OnInterstitialClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad Closed: " + adInfo);

        // Reset counter
        levelsSinceLastAd = 0;
        PlayerPrefs.SetInt(LEVELS_SINCE_AD_KEY, 0);
        PlayerPrefs.Save();

        // Execute the callback
        onAdClosedCallback?.Invoke();
        onAdClosedCallback = null;

        // Load the next ad
        LoadInterstitialAd();
    }

    private void OnInterstitialClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad Clicked: " + adInfo);
    }

    private void OnInterstitialInfoChanged(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial Ad Info Changed: " + adInfo);
    }

    // --- Rewarded Ad Logic ---

    private void CreateRewardedAd()
    {
        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);

        rewardedAd.OnAdLoaded += OnRewardedLoaded;
        rewardedAd.OnAdLoadFailed += OnRewardedLoadFailed;
        rewardedAd.OnAdDisplayed += OnRewardedDisplayed;
        rewardedAd.OnAdDisplayFailed += OnRewardedDisplayFailed;
        rewardedAd.OnAdClosed += OnRewardedClosed;
        rewardedAd.OnAdClicked += OnRewardedClicked;
        rewardedAd.OnAdRewarded += OnRewardedRewarded;
    }

    public void LoadRewardedAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.LoadAd();
        }
    }

    private Action onRewardedCallback;

    public void ShowRewardedAd(Action onRewarded = null)
    {
        onRewardedCallback = onRewarded;

        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            rewardedAd.ShowAd();
        }
        else
        {
            Debug.Log("Rewarded Ad is not ready yet.");
            onRewardedCallback = null;
            LoadRewardedAd();
        }
    }

    // --- Rewarded Ad Events ---

    private void OnRewardedLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad Loaded: " + adInfo);
    }

    private void OnRewardedLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError("Rewarded Ad Load Failed: " + error);
    }

    private void OnRewardedDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad Displayed: " + adInfo);
    }

    private void OnRewardedDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError("Rewarded Ad Display Failed: " + error);
        onRewardedCallback = null;
        LoadRewardedAd();
    }

    private void OnRewardedClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad Closed: " + adInfo);
        LoadRewardedAd();
    }

    private void OnRewardedClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad Clicked: " + adInfo);
    }

    private void OnRewardedRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"Rewarded Ad Rewarded: {adInfo}");

        // Give heart to player
        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.AddHeart(1);
        }

        onRewardedCallback?.Invoke();
        onRewardedCallback = null;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to avoid memory leaks
        LevelPlay.OnInitSuccess -= OnSdkInitSuccess;
        LevelPlay.OnInitFailed -= OnSdkInitFailed;

        if (interstitialAd != null)
        {
            interstitialAd.OnAdLoaded -= OnInterstitialLoaded;
            interstitialAd.OnAdLoadFailed -= OnInterstitialLoadFailed;
            interstitialAd.OnAdDisplayed -= OnInterstitialDisplayed;
            interstitialAd.OnAdDisplayFailed -= OnInterstitialDisplayFailed;
            interstitialAd.OnAdClosed -= OnInterstitialClosed;
            interstitialAd.OnAdClicked -= OnInterstitialClicked;
            interstitialAd.OnAdInfoChanged -= OnInterstitialInfoChanged;

            interstitialAd.DestroyAd();
        }

        if (bannerAd != null)
        {
            bannerAd.OnAdLoaded -= OnBannerLoaded;
            bannerAd.OnAdLoadFailed -= OnBannerLoadFailed;
            bannerAd.OnAdDisplayed -= OnBannerDisplayed;
            bannerAd.OnAdDisplayFailed -= OnBannerDisplayFailed;
            bannerAd.OnAdClicked -= OnBannerClicked;
            bannerAd.OnAdCollapsed -= OnBannerCollapsed;
            bannerAd.OnAdExpanded -= OnBannerExpanded;
            bannerAd.OnAdLeftApplication -= OnBannerLeftApplication;

            bannerAd.DestroyAd();
        }

        if (rewardedAd != null)
        {
            rewardedAd.OnAdLoaded -= OnRewardedLoaded;
            rewardedAd.OnAdLoadFailed -= OnRewardedLoadFailed;
            rewardedAd.OnAdDisplayed -= OnRewardedDisplayed;
            rewardedAd.OnAdDisplayFailed -= OnRewardedDisplayFailed;
            rewardedAd.OnAdClosed -= OnRewardedClosed;
            rewardedAd.OnAdClicked -= OnRewardedClicked;
            rewardedAd.OnAdRewarded -= OnRewardedRewarded;

            rewardedAd.DestroyAd();
        }
    }
}