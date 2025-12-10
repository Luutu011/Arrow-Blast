using System.Collections.Generic;
using UnityEngine;
using ArrowBlast.Core;
using ArrowBlast.Data;

namespace ArrowBlast.Managers
{
    public class LevelGenerator : MonoBehaviour
    {
        public LevelData GenerateRandomLevel(int difficulty, int wallW, int defaultWallH, int gridRows, int gridCols)
        {
            LevelData data = new LevelData();
            data.width = wallW;
            data.height = defaultWallH; // Initial height, will grow
            data.gridRows = gridRows;
            data.gridCols = gridCols;
            data.levelName = "Random_" + Random.Range(0, 10000);

            // Simulation state
            ArrowData[,] tempGrid = new ArrowData[gridCols, gridRows];
            List<ArrowData> placedArrows = new List<ArrowData>(); // Ordered A1, A2... (Pick order)
            
            // Limit iterations to prevent infinite loops
            int arrowsToPlace = (int)(gridRows * gridCols * 0.8f); // Fill 80%

            // Track column heights for the wall to stack efficiently
            int[] columnHeights = new int[wallW]; 

            for (int i = 0; i < arrowsToPlace; i++)
            {
                BlockColor color = (BlockColor)Random.Range(0, 6);
                int length = Random.Range(1, 5); // 1-4

                ArrowData newArrow = TryPlaceArrow(tempGrid, placedArrows, gridCols, gridRows, color, length);
                
                if (newArrow != null)
                {
                    // Success
                    // Add Blocks corresponding to Ammo count
                    int ammo = GetAmmoForLength(length);
                    
                    // Pick a "Cluster Center" for this batch of blocks to create a satisfying chunk
                    int clusterCenter = Random.Range(0, wallW);
                    // Determine a cluster width (e.g., 2 or 3 columns)
                    int clusterWidth = Random.Range(1, 4); 

                    for(int k=0; k<ammo; k++)
                    {
                        AddMatchingBlockStacked(data, columnHeights, color, clusterCenter, clusterWidth);
                    }
                    
                    // Add to grid
                    tempGrid[newArrow.gridX, newArrow.gridY] = newArrow;
                    placedArrows.Add(newArrow);
                    data.arrows.Add(newArrow);
                }
                else
                {
                    break;
                }
            }

            // Update final height
            int maxHeight = 0;
            foreach(int h in columnHeights) if(h > maxHeight) maxHeight = h;
            if(maxHeight > data.height) data.height = maxHeight;

            return data;
        }

        private int GetAmmoForLength(int length)
        {
            switch(length)
            {
                case 1: return 10;
                case 2: return 20;
                case 3: return 30;
                case 4: return 40;
                default: return 10;
            }
        }

        private void AddMatchingBlockStacked(LevelData data, int[] colHeights, BlockColor color, int clusterCenter, int clusterWidth)
        {
            // Find column within cluster range with lowest height
            // Cluster range: [clusterCenter - spread, clusterCenter + spread]
            // We want to fill the columns in this range roughly evenly
            
            int bestCol = -1;
            int minH = int.MaxValue;

            // Define start/end columns for the cluster, clamping to wall width
            // We alternate left/right from center to fill width
            // Actually simple iter is fine
            int startCol = Mathf.Max(0, clusterCenter - clusterWidth/2);
            int endCol = Mathf.Min(data.width - 1, clusterCenter + clusterWidth/2);

            // Iterate only within cluster range
            for(int i = startCol; i <= endCol; i++)
            {
                if(colHeights[i] < minH)
                {
                    minH = colHeights[i];
                    bestCol = i;
                }
            }
            
            // Fallback: If for some reason range is invalid (shouldn't be), pick random
            if (bestCol == -1) bestCol = Random.Range(0, data.width);

            // Add block
            BlockData bd = new BlockData();
            bd.gridX = bestCol;
            bd.gridY = minH; // Current top of this column
            bd.colorIndex = (int)color;
            data.blocks.Add(bd);

            // Increment height
            colHeights[bestCol]++;
        }

        private ArrowData TryPlaceArrow(ArrowData[,] grid, List<ArrowData> existingArrows, int cols, int rows, BlockColor color, int length)
        {
            // Canvas candidates
            List<Vector2Int> candidates = new List<Vector2Int>();
            for(int x=0; x<cols; x++)
                for(int y=0; y<rows; y++)
                    if(grid[x,y] == null) candidates.Add(new Vector2Int(x, y));

            // Shuffle
            for(int k=0; k<candidates.Count; k++) {
                Vector2Int temp = candidates[k];
                int r = Random.Range(k, candidates.Count);
                candidates[k] = candidates[r];
                candidates[r] = temp;
            }

            foreach(var pos in candidates)
            {
                List<Direction> dirs = new List<Direction>{ Direction.Up, Direction.Down, Direction.Left, Direction.Right };
                // Shuffle dirs
                 for(int k=0; k<dirs.Count; k++) {
                    Direction t = dirs[k];
                    int r = Random.Range(k, dirs.Count);
                    dirs[k] = dirs[r];
                    dirs[r] = t;
                }

                foreach(var dir in dirs)
                {
                    // Candidate Arrow checking Multi-tile
                    ArrowData candidate = new ArrowData { gridX = pos.x, gridY = pos.y, direction = (int)dir, length = length, colorIndex = (int)color };
                    
                    // 1. Check if Body fits in Grid and is Empty
                    List<Vector2Int> body = GetOccupiedCellsSimple(candidate);
                    bool bodyFits = true;
                    foreach(var cell in body)
                    {
                        if(cell.x < 0 || cell.x >= cols || cell.y < 0 || cell.y >= rows) { bodyFits = false; break; }
                        if(grid[cell.x, cell.y] != null) { bodyFits = false; break; }
                    }
                    if(!bodyFits) continue;

                    // 2. Check if this Body blocks any Existing Arrow's Path
                    if (IsPlacementValid(existingArrows, body))
                    {
                        return candidate; // Return candidate with valid length/dir
                    }
                }
            }

            return null;
        }

        private List<Vector2Int> GetOccupiedCellsSimple(ArrowData a)
        {
             List<Vector2Int> list = new List<Vector2Int>();
             Vector2Int head = new Vector2Int(a.gridX, a.gridY);
             list.Add(head);
             
             Vector2Int back = Vector2Int.zero;
             switch((Direction)a.direction)
             {
                 case Direction.Up: back = new Vector2Int(0, 1); break;
                 case Direction.Down: back = new Vector2Int(0, -1); break;
                 case Direction.Left: back = new Vector2Int(1, 0); break;
                 case Direction.Right: back = new Vector2Int(-1, 0); break;
             }
             for(int i=1; i<a.length; i++) list.Add(head + back * i);
             return list;
        }

        private bool IsPlacementValid(List<ArrowData> existingArrows, List<Vector2Int> newObstacles)
        {
            // Existing Arrow A_i must NOT hit New Arrow's Body
            foreach(var arrow in existingArrows)
            {
                if (DoesArrowHitObstacles(arrow, newObstacles))
                {
                    return false; // Blocked
                }
            }
            return true;
        }

        private bool DoesArrowHitObstacles(ArrowData shooter, List<Vector2Int> obstacles)
        {
            int cx = shooter.gridX;
            int cy = shooter.gridY;
            int dx = 0; int dy = 0;
            
            // Check direction convention
            // Up = Head Moves Up = (0, -1) in Grid
            switch(shooter.direction)
            {
                case 0: dy = -1; break; // Up
                case 1: dx = 1; break; // Right
                case 2: dy = 1; break;// Down
                case 3: dx = -1; break;// Left
            }
            
            foreach(var obs in obstacles)
            {
                int tx = obs.x;
                int ty = obs.y;
                
                if (dx != 0)
                {
                    if (ty != cy) continue; 
                    if (dx > 0 && tx > cx) return true; // Right
                    if (dx < 0 && tx < cx) return true; // Left
                }
                else
                {
                    if (tx != cx) continue; 
                    if (dy > 0 && ty > cy) return true; // Down
                    if (dy < 0 && ty < cy) return true; // Up (dy=-1, ty<cy)
                }
            }

            return false;
        }
    }
}
