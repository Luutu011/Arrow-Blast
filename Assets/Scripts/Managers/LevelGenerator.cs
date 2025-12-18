using System.Collections.Generic;
using UnityEngine;
using ArrowBlast.Core;
using ArrowBlast.Data;

namespace ArrowBlast.Managers
{
    public class LevelGenerator : MonoBehaviour
    {
        [SerializeField] private int seed = 12345;

        public LevelData GenerateRandomLevel(int difficulty, int wallWidth, int wallHeight, int arrowRows, int arrowCols)
        {
            Random.InitState(seed + difficulty);
            
            LevelData level = new LevelData
            {
                levelName = $"Random_Level_{difficulty}",
                width = wallWidth,
                height = wallHeight,
                gridRows = arrowRows,
                gridCols = arrowCols
            };

            // Generate wall blocks (portrait - narrower)
            int blockCount = Mathf.Min(wallWidth * wallHeight / 2, 20 + difficulty * 5);
            HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();

            for (int i = 0; i < blockCount; i++)
            {
                int x = Random.Range(0, wallWidth);
                int y = Random.Range(0, wallHeight);
                Vector2Int pos = new Vector2Int(x, y);

                if (!usedPositions.Contains(pos))
                {
                    usedPositions.Add(pos);
                    level.blocks.Add(new BlockData
                    {
                        colorIndex = Random.Range(0, 6),
                        gridX = x,
                        gridY = y
                    });
                }
            }

            // Generate arrows
            int arrowCount = Mathf.Min(arrowRows * arrowCols / 3, 15 + difficulty * 3);
            bool[,] arrowGrid = new bool[arrowCols, arrowRows];

            for (int i = 0; i < arrowCount; i++)
            {
                int attempts = 0;
                while (attempts < 50)
                {
                    int x = Random.Range(0, arrowCols);
                    int y = Random.Range(0, arrowRows);
                    Direction dir = (Direction)Random.Range(0, 4);
                    int length = Random.Range(1, 5);

                    if (CanPlaceArrow(x, y, dir, length, arrowCols, arrowRows, arrowGrid))
                    {
                        PlaceArrow(x, y, dir, length, arrowGrid);
                        level.arrows.Add(new ArrowData
                        {
                            colorIndex = Random.Range(0, 6),
                            direction = (int)dir,
                            length = length,
                            gridX = x,
                            gridY = y
                        });
                        break;
                    }
                    attempts++;
                }
            }

            return level;
        }

        private bool CanPlaceArrow(int x, int y, Direction dir, int length, int cols, int rows, bool[,] grid)
        {
            Vector2Int head = new Vector2Int(x, y);
            if (head.x < 0 || head.x >= cols || head.y < 0 || head.y >= rows) return false;
            if (grid[head.x, head.y]) return false;

            Vector2Int back = Vector2Int.zero;
            switch(dir)
            {
                case Direction.Up: back = new Vector2Int(0, 1); break;
                case Direction.Down: back = new Vector2Int(0, -1); break;
                case Direction.Left: back = new Vector2Int(1, 0); break;
                case Direction.Right: back = new Vector2Int(-1, 0); break;
            }

            for (int i = 1; i < length; i++)
            {
                Vector2Int cell = head + back * i;
                if (cell.x < 0 || cell.x >= cols || cell.y < 0 || cell.y >= rows) return false;
                if (grid[cell.x, cell.y]) return false;
            }

            return true;
        }

        private void PlaceArrow(int x, int y, Direction dir, int length, bool[,] grid)
        {
            Vector2Int head = new Vector2Int(x, y);
            grid[head.x, head.y] = true;

            Vector2Int back = Vector2Int.zero;
            switch(dir)
            {
                case Direction.Up: back = new Vector2Int(0, 1); break;
                case Direction.Down: back = new Vector2Int(0, -1); break;
                case Direction.Left: back = new Vector2Int(1, 0); break;
                case Direction.Right: back = new Vector2Int(-1, 0); break;
            }

            for (int i = 1; i < length; i++)
            {
                Vector2Int cell = head + back * i;
                grid[cell.x, cell.y] = true;
            }
        }
    }
}
