using UnityEngine;
using System;
using ArrowBlast.Interfaces;

public class LivesManager : MonoBehaviour, ILivesService
{
    private const int MAX_HEARTS = 5;
    private const int REGEN_SECONDS = 600; // 10 minutes
    private const string HEARTS_KEY = "PlayerHearts";
    private const string LAST_REGEN_KEY = "LastRegenTime";

    private int currentHearts;
    private DateTime lastRegenTime;
    private float regenTimer;

    void Awake()
    {
        LoadHearts();
    }

    void Update()
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

        string lastRegenStr = PlayerPrefs.GetString(LAST_REGEN_KEY, "");
        if (string.IsNullOrEmpty(lastRegenStr))
        {
            lastRegenTime = DateTime.Now;
            regenTimer = 0f;
        }
        else
        {
            lastRegenTime = DateTime.Parse(lastRegenStr);

            // Calculate hearts regenerated while offline
            double elapsedSeconds = (DateTime.Now - lastRegenTime).TotalSeconds;
            int heartsToAdd = Mathf.FloorToInt((float)elapsedSeconds / REGEN_SECONDS);

            if (heartsToAdd > 0 && currentHearts < MAX_HEARTS)
            {
                AddHeart(heartsToAdd);
            }

            // Set timer to remainder
            regenTimer = (float)(elapsedSeconds % REGEN_SECONDS);
        }

        SaveHearts();
    }

    void SaveHearts()
    {
        PlayerPrefs.SetInt(HEARTS_KEY, currentHearts);
        PlayerPrefs.SetString(LAST_REGEN_KEY, lastRegenTime.ToString());
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

        // Reset timer when hearts are full
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