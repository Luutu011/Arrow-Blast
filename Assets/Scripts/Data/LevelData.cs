using System;
using System.Collections.Generic;

namespace ArrowBlast.Data
{
    [Serializable]
    public class LevelData
    {
        public string levelName;
        public int width;       // Wall width
        public int height;      // Wall height
        public int gridRows;    // Arrow grid rows
        public int gridCols;    // Arrow grid cols
        
        public List<BlockData> blocks = new List<BlockData>();
        public List<ArrowData> arrows = new List<ArrowData>();
    }

    [Serializable]
    public class BlockData
    {
        public int colorIndex;
        public int gridX;
        public int gridY;
    }

    [Serializable]
    public class ArrowData
    {
        public int colorIndex;
        public int direction;   // 0=Up, 1=Right, 2=Down, 3=Left
        public int length;      // 1-4
        public int gridX;       // Head position
        public int gridY;
    }
}
