using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowBlast.Data
{
    [Serializable]
    [CreateAssetMenu(fileName = "NewLevel", menuName = "Arrow Blast/Level Data")]
    public class LevelData : ScriptableObject
    {
        public enum Difficulty { Easy, Medium, Hard }

        public Difficulty difficulty = Difficulty.Easy;
        public string levelName;
        public int width = 6;       // Wall width
        public int height = 8;      // Wall height
        public int gridRows = 8;    // Arrow grid rows
        public int gridCols = 6;    // Arrow grid cols

        public List<BlockData> blocks = new List<BlockData>();
        public List<ArrowData> arrows = new List<ArrowData>();
        public List<LockData> locks = new List<LockData>();
    }

    [Serializable]
    public class BlockData
    {
        public int colorIndex;
        public int secondaryColorIndex;
        public bool isTwoColor;
        public int gridX;
        public int gridY;
        public int lockId = -1; // -1 means no key, >= 0 means it's a KeyBlock
    }

    [Serializable]
    public class ArrowData
    {
        public int colorIndex;
        public int direction;   // 0=Up, 1=Right, 2=Down, 3=Left
        public int length;      // Straight length (Legacy)
        public int gridX;       // Head position X (Legacy)
        public int gridY;       // Head position Y (Legacy)
        public List<Vector2Int> segments = new List<Vector2Int>(); // Ordered list of cells [0] is head
    }


    [Serializable]
    public class LockData
    {
        public int gridX;       // Position in wall grid (bottom)
        public int gridY;
        public int sizeX;       // Width of lock (how many columns it spans)
        public int sizeY;       // Height of lock (how many rows it spans)
        public int lockId;      // ID to match with corresponding key
    }
}
