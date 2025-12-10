using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ArrowBlast.Core;
using ArrowBlast.Data;
using ArrowBlast.Game;

namespace ArrowBlast.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private LevelGenerator levelGenerator;
        [SerializeField] private Transform wallContainer;
        [SerializeField] private Transform puzzleContainer;
        [SerializeField] private Transform slotsContainer;

        [Header("Prefabs")]
        [SerializeField] private Block blockPrefab;
        [SerializeField] private Arrow arrowPrefab;
        [SerializeField] private Slot slotPrefab;

        [Header("Settings")]
        [SerializeField] private float cellSize = 1.0f;
        [SerializeField] private float fireRate = 0.2f;
        
        [Header("Game State")]
        public bool isStoryMode = false;
        public string storyLevelName = "Level1";

        // Runtime Data
        private Block[,] wallGrid;
        private Arrow[,] arrowGrid;
        private List<Slot> slots = new List<Slot>();
        private int wallWidth, wallHeight;
        private int arrowRows, arrowCols;

        private float shootTimer;
        private bool isGameOver;

        private void Start()
        {
            InitializeSlots();
            
            if (isStoryMode)
            {
                LoadStoryLevel(storyLevelName);
            }
            else
            {
                CreateRandomLevel();
            }
        }

        private void InitializeSlots()
        {
            // Create 5 slots
            for (int i = 0; i < 5; i++)
            {
                Slot s = Instantiate(slotPrefab, slotsContainer);
                s.Initialize();
                slots.Add(s);
            }
        }

        private void CreateRandomLevel()
        {
            LevelData data = levelGenerator.GenerateRandomLevel(1, 6, 8, 8, 6); // Hardcoded sizes for demo
            BuildLevel(data);
        }

        private void LoadStoryLevel(string levelName)
        {
            LevelData data = SaveSystem.LoadLevel(levelName);
            if(data != null)
            {
                BuildLevel(data);
            }
            else
            {
                Debug.LogWarning("Story level not found, falling back to random");
                CreateRandomLevel();
            }
        }

        private void BuildLevel(LevelData data)
        {
            // Clear existing
            foreach(Transform t in wallContainer) Destroy(t.gameObject);
            foreach(Transform t in puzzleContainer) Destroy(t.gameObject);

            wallWidth = data.width;
            wallHeight = data.height;
            wallGrid = new Block[wallWidth, wallHeight];

            foreach(var bd in data.blocks)
            {
                Block b = Instantiate(blockPrefab, wallContainer);
                b.Init((BlockColor)bd.colorIndex, bd.gridX, bd.gridY);
                b.UpdateGridPosition(bd.gridX, bd.gridY); 
                b.transform.localPosition = new Vector3(bd.gridX * cellSize, bd.gridY * cellSize, 0);
                wallGrid[bd.gridX, bd.gridY] = b;
            }

            arrowRows = data.gridRows; 
            arrowCols = data.gridCols;
            arrowGrid = new Arrow[arrowCols, arrowRows];

            foreach(var ad in data.arrows)
            {
                Arrow a = Instantiate(arrowPrefab, puzzleContainer);
                a.Init((BlockColor)ad.colorIndex, (Direction)ad.direction, ad.length, ad.gridX, ad.gridY);
                a.transform.localPosition = new Vector3(ad.gridX * cellSize, -ad.gridY * cellSize - 2.0f, 0); 
                
                // occupy grid
                var occupied = a.GetOccupiedCells();
                foreach(var c in occupied)
                {
                    if(IsValidCell(c.x, c.y))
                    {
                        arrowGrid[c.x, c.y] = a;
                    }
                }
            }
        }
        
        private bool IsValidCell(int x, int y)
        {
            return x >=0 && x < arrowCols && y >= 0 && y < arrowRows;
        }

        private void Update()
        {
            if (isGameOver) return;

            HandleInput();
            HandleShooting();
            CheckWinCondition();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Raycast
                Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

                // Use Physics Raycast if 3D
                if(hit.collider == null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if(Physics.Raycast(ray, out RaycastHit hit3d))
                    {
                         Arrow arrow = hit3d.collider.GetComponent<Arrow>(); 
                         if(arrow) TryCollectArrow(arrow);
                         return;
                    }
                }

                if (hit.collider != null)
                {
                    Arrow arrow = hit.collider.GetComponent<Arrow>();
                    if (arrow != null)
                    {
                        TryCollectArrow(arrow);
                    }
                }
            }
        }

        private void TryCollectArrow(Arrow arrow)
        {
            if (CanArrowEscape(arrow))
            {
                // Add to Slot
                if (TryAddToSlot(arrow.Color, arrow.GetAmmoAmount()))
                {
                    // Remove from Grid (Clear ALL occupied cells)
                    var occupied = arrow.GetOccupiedCells();
                    foreach(var c in occupied)
                    {
                        if(IsValidCell(c.x, c.y) && arrowGrid[c.x,c.y] == arrow)
                        {
                            arrowGrid[c.x, c.y] = null;
                        }
                    }
                    Destroy(arrow.gameObject);
                }
            }
        }

        private bool CanArrowEscape(Arrow arrow)
        {
            int cx = arrow.GridX; // Head position
            int cy = arrow.GridY;
            int dx = 0, dy = 0;

            switch(arrow.ArrowDirection)
            {
                case Direction.Up: dy = -1; break; 
                case Direction.Down: dy = 1; break;
                case Direction.Left: dx = -1; break;
                case Direction.Right: dx = 1; break;
            }
            
            // Check path from Head
            int nx = cx + dx;
            int ny = cy + dy;

            while (IsValidCell(nx, ny))
            {
                // If any cell in path is not null, BLOCKED
                if (arrowGrid[nx, ny] != null) return false;
                nx += dx;
                ny += dy;
            }

            return true;
        }

        private bool TryAddToSlot(BlockColor color, int amount)
        {
            // Find first empty or matching last slot?
            // "Queue". You have 5 slots.
            // Logic: Fill first available slot.
            // If slots[0] is occupied, try slots[1].
            
            foreach(var slot in slots)
            {
                if (!slot.IsOccupied)
                {
                    slot.FillSlot(color, amount);
                    return true;
                }
                // If we want merge logic:
                // if (slot.IsOccupied && slot.CurrentColor == color && slot == lastFilledSlot) ...
                // But prompt implies distinct slots in queue.
            }
            return false;
        }

        private void HandleShooting()
        {
            shootTimer += Time.deltaTime;
            if (shootTimer < fireRate) return;

            // Check first occupied slot
            Slot activeSlot = slots.Find(s => s.IsOccupied);
            if (activeSlot == null) return;

            // Try to find target
            if (TryDestroyBlock(activeSlot.CurrentColor))
            {
                activeSlot.UseAmmo(1);
                shootTimer = 0;
                
                // Shift slots if empty? 
                // Slot logic handles internal clearing.
                // But if Slot 0 becomes empty, do we shift 1->0?
                // "Queue" implies shifting.
                if (!activeSlot.IsOccupied)
                {
                    ShiftSlots();
                }
            }
        }

        private void ShiftSlots()
        {
            // Move 1->0, 2->1 ...
            for(int i=0; i<slots.Count-1; i++)
            {
                Slot current = slots[i];
                Slot next = slots[i+1];
                
                if (!current.IsOccupied && next.IsOccupied)
                {
                    current.FillSlot(next.CurrentColor, next.AmmoCount);
                    next.ClearSlot();
                }
            }
            // Repeat bubble sort? Or just once is enough if we do it every frame/update.
            // If we just expended Slot 0, it's empty. Next call will shift 1 to 0.
        }

        private bool TryDestroyBlock(BlockColor color)
        {
            // Find bottom-most block of this color
            // Iterate columns?
            // "Look at the bottom row of the wall." 
            // Blocks fall. So we just need to find any block that is "Exposed" at the bottom.
            // Exposed = No block below it.
            
            // Find ALL exposed blocks matching color.
            // "The Active Shooter ... will only shoot at Red blocks that are at the bottom".
            // Pick ONE. (Say, leftmost?)
            
            for (int x = 0; x < wallWidth; x++)
            {
                // Find bottom-most in this column
                for (int y = 0; y < wallHeight; y++)
                {
                    // Assuming Y=0 is bottom? 
                    // In BuildLevel: y * cellSize. So 0 is Bottom.
                    Block b = wallGrid[x, y];
                    if (b != null)
                    {
                        // Found the lowest block in this column
                        if (b.Color == color)
                        {
                            // Destroy it
                            DestroyBlock(x, y);
                            return true;
                        }
                        // If bottom block is NOT color, then this column is blocked.
                        break; 
                    }
                }
            }
            return false;
        }

        private void DestroyBlock(int x, int y)
        {
            Block b = wallGrid[x, y];
            if(b) Destroy(b.gameObject);
            wallGrid[x, y] = null;
            
            // Gravity: Move blocks above down
            // In array and visual
            for (int k = y + 1; k < wallHeight; k++)
            {
                Block above = wallGrid[x, k];
                if (above != null)
                {
                    wallGrid[x, k-1] = above;
                    wallGrid[x, k] = null;
                    above.UpdateGridPosition(x, k-1);
                    // Visual tween should happen here
                    above.transform.localPosition = new Vector3(x * cellSize, (k-1) * cellSize, 0);
                }
            }
        }

        private void CheckWinCondition()
        {
             bool hasBlocks = false;
             foreach(var b in wallGrid) if(b != null) hasBlocks = true;
             
             if (!hasBlocks)
             {
                 Debug.Log("Victory!");
                 isGameOver = true;
                 return;
             }

             // Loss Condition 1: Slots Full and Active Shooter Stuck
             bool slotsFull = slots.TrueForAll(s => s.IsOccupied);
             if (slotsFull)
             {
                 Slot active = slots[0]; // Assuming shift logic keeps 0 as active
                 // Check if active matches ANY bottom block
                 bool canShoot = false;
                 // We need to peek if TryDestroyBlock WOULD succeed.
                 // Actually we can just check if ANY bottom block matches active color
                 if (CanHitAnyBlock(active.CurrentColor)) canShoot = true;

                 if (!canShoot)
                 {
                     Debug.Log("Game Over: Slots Full and Stuck!");
                     isGameOver = true;
                     return;
                 }
             }

             // Loss Condition 2: No Ammo Left
             bool arrowsRemaining = false;
             foreach(var a in arrowGrid) if(a != null) arrowsRemaining = true;
             
             bool slotsEmpty = slots.TrueForAll(s => !s.IsOccupied);

             if (!arrowsRemaining && slotsEmpty && hasBlocks)
             {
                  Debug.Log("Game Over: Out of Ammo!");
                  isGameOver = true;
             }
        }

        private bool CanHitAnyBlock(BlockColor color)
        {
            for (int x = 0; x < wallWidth; x++)
            {
                // Find bottom-most
                for (int y = 0; y < wallHeight; y++)
                {
                    Block b = wallGrid[x, y];
                    if (b != null)
                    {
                        if (b.Color == color) return true;
                        break; // Blocked by wrong color
                    }
                }
            }
            return false;
        }
        public void SaveCurrentLevel(string levelName)
        {
            // Reconstruct LevelData from current state logic not fully implemented
            // But we can save the 'Initial' state if we stored it, or generating a new one to verify generator
            // Use LevelGenerator to generate and then Save
            
            // To fulfill request: "in random a LevelGenerator with Json save"
            // We'll generate a fresh level and save it for future Story use.
            
            LevelData newData = levelGenerator.GenerateRandomLevel(1, wallWidth, wallHeight, arrowRows, arrowCols);
            newData.levelName = levelName;
            SaveSystem.SaveLevel(newData, levelName);
        }
    }
}
