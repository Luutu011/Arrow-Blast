using UnityEngine;
using System;
using ArrowBlast.Managers;

public class LivesManager : MonoBehaviour, IUpdateable
{
    public static LivesManager Instance;

    private const int MAX_HEARTS = 5;
    private const int REGEN_SECONDS = 600; // 10 minutes
    private const string HEARTS_KEY = "PlayerHearts";
    private const string LAST_REGEN_TIME_KEY = "LastRegenTime";

    private int currentHearts;
    private DateTime lastRegenTime;
    private float regenTimer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadHearts();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.RegisterUpdateable(this);
    }

    private void OnDisable()
    {
        if (UpdateManager.Instance != null)
            UpdateManager.Instance.UnregisterUpdateable(this);
    }

    public void ManagedUpdate()
    {
        if (currentHearts < MAX_HEARTS)
        {
            regenTimer += Time.deltaTime;

            if (regenTimer >= REGEN_SECONDS)
            {
                AddHeart(1);
                regenTimer = 0f;
                lastRegenTime = DateTime.Now;
            }
        }
    }

    void LoadHearts()
    {
        currentHearts = PlayerPrefs.GetInt(HEARTS_KEY, MAX_HEARTS);
        
        string lastRegenStr = PlayerPrefs.GetString(LAST_REGEN_TIME_KEY, "");
        if (string.IsNullOrEmpty(lastRegenStr))
        {
            lastRegenTime = DateTime.Now;
            regenTimer = 0f;
        }
        else
        {
            if (DateTime.TryParse(lastRegenStr, out lastRegenTime))
            {
                double elapsedSeconds = (DateTime.Now - lastRegenTime).TotalSeconds;
                int heartsToAdd = Mathf.FloorToInt((float)elapsedSeconds / REGEN_SECONDS);
                
                if (heartsToAdd > 0 && currentHearts < MAX_HEARTS)
                {
                    AddHeart(heartsToAdd);
                }
                
                regenTimer = (float)(elapsedSeconds % REGEN_SECONDS);
            }
            else
            {
                lastRegenTime = DateTime.Now;
                regenTimer = 0f;
            }
        }
        
        SaveHearts();
    }

    void SaveHearts()
    {
        PlayerPrefs.SetInt(HEARTS_KEY, currentHearts);
        PlayerPrefs.SetString(LAST_REGEN_TIME_KEY, lastRegenTime.ToString());
        PlayerPrefs.Save();
    }

    public bool CanStartLevel()
    {
        return currentHearts > 0;
    }

    public void DeductHeart()
    {
        if (currentHearts > 0)
        {
            currentHearts--;
            SaveHearts();
        }
    }

    public void AddHeart(int count)
    {
        currentHearts = Mathf.Min(currentHearts + count, MAX_HEARTS);
        
        if (currentHearts >= MAX_HEARTS)
        {
            regenTimer = 0f;
            lastRegenTime = DateTime.Now;
        }
        
        SaveHearts();
    }

    public int GetCurrentHearts()
    {
        return currentHearts;
    }

    public int GetSecondsUntilNextHeart()
    {
        if (currentHearts >= MAX_HEARTS) return 0;
        return Mathf.CeilToInt(REGEN_SECONDS - regenTimer);
    }

    public int GetMaxHearts()
    {
        return MAX_HEARTS;
    }
}