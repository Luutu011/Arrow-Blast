using System.Collections.Generic;
using UnityEngine;

namespace ArrowBlast.Data
{
    [System.Serializable]
    public class LevelData
    {
        public string levelName;
        public int width;  // Wall width
        public int height; // Wall height
        public List<BlockData> blocks = new List<BlockData>();
        
        public int gridRows; // Arrow Grid Rows
        public int gridCols; // Arrow Grid Cols
        public List<ArrowData> arrows = new List<ArrowData>();
    }

    [System.Serializable]
    public class BlockData
    {
        public int gridX;
        public int gridY;
        public int colorIndex; // 0:Red, 1:Blue, 2:Green, 3:Yellow, 4:Purple, 5:Orange
    }

    [System.Serializable]
    public class ArrowData
    {
        public int gridX;
        public int gridY;
        public int direction; // 0:Up, 1:Right, 2:Down, 3:Left
        public int colorIndex;
        public int length; // 1 to 4
    }
}
