using System.IO;
using UnityEngine;
using ArrowBlast.Data;

namespace ArrowBlast.Managers
{
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "Levels");

        public static void SaveLevel(LevelData level, string levelName)
        {
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }

            string json = JsonUtility.ToJson(level, true);
            string filePath = Path.Combine(SavePath, $"{levelName}.json");
            File.WriteAllText(filePath, json);
            
            Debug.Log($"Level saved to: {filePath}");
        }

        public static LevelData LoadLevel(string levelName)
        {
            string filePath = Path.Combine(SavePath, $"{levelName}.json");
            
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                LevelData level = JsonUtility.FromJson<LevelData>(json);
                Debug.Log($"Level loaded from: {filePath}");
                return level;
            }
            else
            {
                Debug.LogWarning($"Level file not found: {filePath}");
                return null;
            }
        }

        public static bool LevelExists(string levelName)
        {
            string filePath = Path.Combine(SavePath, $"{levelName}.json");
            return File.Exists(filePath);
        }
    }
}
