using UnityEngine;
using System.IO;
using ArrowBlast.Data;

namespace ArrowBlast.Managers
{
    public static class SaveSystem
    {
        public static void SaveLevel(LevelData data, string fileName)
        {
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
            File.WriteAllText(path, json);
            Debug.Log("Saved level to " + path);
        }

        public static LevelData LoadLevel(string fileName)
        {
            // First check Resources (for Story mode logic usually)
            // Or persistentDataPath
            
            string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<LevelData>(json);
            }
            
            // As fallback check TextAsset in Resources if you implement that system
            // TextAsset ta = Resources.Load<TextAsset>("Levels/" + fileName);
            // if(ta) return JsonUtility.FromJson<LevelData>(ta.text);

            Debug.LogError("Level file not found: " + fileName);
            return null;
        }
    }
}
