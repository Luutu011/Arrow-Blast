using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ArrowBlast.Core;
using ArrowBlast.Data;
using ArrowBlast.Game;

namespace ArrowBlast.Managers
{
    /// <summary>
    /// Main game manager for Arrow Blast - Portrait Mobile Layout - FULL 3D
    /// Layout (Top to Bottom): Wall (y=6) -> Shooter Slots (y=0) -> Arrow Grid (y=-6)
    /// All occupied slots shoot simultaneously (no "active shooter")
    /// Slots don't shift when empty
    /// Uses 3D physics and 3D objects with 2D-style animations
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private LevelGenerator levelGenerator;
        [SerializeField] private Transform wallContainer;
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private Transform arrowContainer;

        [Header("Prefabs")]
        [SerializeField] private Block blockPrefab;
        [SerializeField] private Arrow arrowPrefab;
        [SerializeField] private Slot slotPrefab;

        [Header("Settings")]
        [SerializeField] private float cellSize = 0.8f;
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
        private int debugFrameCount = 0;

        private void Start()
        {
            Debug.Log("[GAMEMANAGER] Start() called");

            InitializeSlots();

            Debug.Log($"[GAMEMANAGER] Slots initialized: {slots.Count} slots created");

            if (isStoryMode)
            {
                LoadStoryLevel(storyLevelName);
            }
            else
            {
                CreateRandomLevel();
            }

            Debug.Log("[GAMEMANAGER] Level loaded successfully");
            Debug.Log($"[INPUT SYSTEM] Mouse.current is: {(Mouse.current != null ? "AVAILABLE" : "NULL - INPUT SYSTEM NOT CONFIGURED!")}");
        }

        // Debug display to show input status on screen
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;

            GUI.Label(new Rect(10, 10, 500, 30), $"GameManager Running: YES", style);
            GUI.Label(new Rect(10, 40, 500, 30), $"Update() Frames: {debugFrameCount}", style);
            GUI.Label(new Rect(10, 70, 500, 30), $"Mouse.current: {(Mouse.current != null ? "YES" : "NO")}", style);
            GUI.Label(new Rect(10, 100, 500, 30), $"Arrows in scene: {FindObjectsOfType<Arrow>().Length}", style);
            GUI.Label(new Rect(10, 130, 500, 30), $"Click anywhere to test", style);

            if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                GUI.Label(new Rect(10, 160, 500, 30), $"Mouse: {mousePos}", style);

                bool isPressed = Mouse.current.leftButton.isPressed;
                bool wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;

                GUI.Label(new Rect(10, 190, 500, 30), $"Button Held: {isPressed}", style);
                GUI.Label(new Rect(10, 220, 600, 30), $"wasPressedThisFrame: {wasPressedThisFrame}", style);

                if (wasPressedThisFrame)
                {
                    style.normal.textColor = Color.red;
                    GUI.Label(new Rect(10, 250, 800, 40), $">>> CLICK DETECTED <<<", style);
                }
            }
        }

        private void InitializeSlots()
        {
            // Create 5 slots
            for (int i = 0; i < 5; i++)
            {
                Slot s = Instantiate(slotPrefab, slotsContainer);
                s.transform.localPosition = new Vector3((i - 2) * 1.2f, 0, 0); // Spread horizontally
                s.Initialize();
                slots.Add(s);
            }
        }

        private void CreateRandomLevel()
        {
            LevelData data = levelGenerator.GenerateRandomLevel(1, 6, 8, 8, 6);
            BuildLevel(data);
        }

        private void LoadStoryLevel(string levelName)
        {
            LevelData data = SaveSystem.LoadLevel(levelName);
            if (data != null)
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
            foreach (Transform t in wallContainer) Destroy(t.gameObject);
            foreach (Transform t in arrowContainer) Destroy(t.gameObject);

            wallWidth = data.width;
            wallHeight = data.height;
            wallGrid = new Block[wallWidth, wallHeight];

            // Build wall blocks
            foreach (var bd in data.blocks)
            {
                Block b = Instantiate(blockPrefab, wallContainer);
                b.Init((BlockColor)bd.colorIndex, bd.gridX, bd.gridY);
                b.transform.localPosition = GetWallWorldPosition(bd.gridX, bd.gridY);
                wallGrid[bd.gridX, bd.gridY] = b;
            }

            arrowRows = data.gridRows;
            arrowCols = data.gridCols;
            arrowGrid = new Arrow[arrowCols, arrowRows];

            // Build arrow grid
            foreach (var ad in data.arrows)
            {
                Arrow a = Instantiate(arrowPrefab, arrowContainer);
                a.Init((BlockColor)ad.colorIndex, (Direction)ad.direction, ad.length, ad.gridX, ad.gridY);
                a.transform.localPosition = GetArrowWorldPosition(ad.gridX, ad.gridY);

                // Occupy grid cells
                var occupied = a.GetOccupiedCells();
                foreach (var c in occupied)
                {
                    if (IsValidCell(c.x, c.y))
                    {
                        arrowGrid[c.x, c.y] = a;
                    }
                }
            }
        }

        private bool IsValidCell(int x, int y)
        {
            return x >= 0 && x < arrowCols && y >= 0 && y < arrowRows;
        }

        // Helper: Convert wall grid coordinates to centered world position
        private Vector3 GetWallWorldPosition(int gridX, int gridY)
        {
            float offsetX = -(wallWidth / 2f) * cellSize + (cellSize / 2f);
            float offsetY = -(wallHeight / 2f) * cellSize + (cellSize / 2f);
            return new Vector3(gridX * cellSize + offsetX, gridY * cellSize + offsetY, 0);
        }

        // Helper: Convert arrow grid coordinates to centered world position
        private Vector3 GetArrowWorldPosition(int gridX, int gridY)
        {
            float offsetX = -(arrowCols / 2f) * cellSize + (cellSize / 2f);
            float offsetY = -(arrowRows / 2f) * cellSize + (cellSize / 2f);
            return new Vector3(gridX * cellSize + offsetX, gridY * cellSize + offsetY, 0);
        }

        private void Update()
        {
            debugFrameCount++;

            if (isGameOver) return;

            HandleInput();
            HandleShooting();
            CheckWinCondition();
        }

        private void HandleInput()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Debug.Log($"[INPUT] Mouse clicked at screen position: {mousePosition}");

                // 3D Physics Raycast (no 2D physics)
                Ray ray = Camera.main.ScreenPointToRay(mousePosition);
                Debug.Log($"[RAYCAST] Ray origin: {ray.origin}, direction: {ray.direction}");
                Debug.Log($"[CAMERA] Camera position: {Camera.main.transform.position}, ortho size: {Camera.main.orthographicSize}");

                // Increase raycast distance significantly for orthographic camera
                float raycastDistance = 1000f;

                // Try raycast with no layer mask
                if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
                {
                    Debug.Log($"[HIT] Raycast hit: {hit.collider.gameObject.name} at position {hit.point}, distance: {hit.distance}");
                    Debug.Log($"[HIT] Hit object layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

                    Arrow arrow = hit.collider.GetComponent<Arrow>();
                    if (arrow != null)
                    {
                        Debug.Log($"[ARROW] Found arrow at grid ({arrow.GridX}, {arrow.GridY}), Direction: {arrow.ArrowDirection}, Color: {arrow.Color}");
                        TryCollectArrow(arrow);
                    }
                    else
                    {
                        Debug.LogWarning($"[NO ARROW] Hit object '{hit.collider.gameObject.name}' has no Arrow component.");
                        Component[] components = hit.collider.GetComponents<Component>();
                        Debug.Log($"[COMPONENTS] {string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name))}");
                    }
                }
                else
                {
                    Debug.LogWarning("[NO HIT] Raycast didn't hit anything!");

                    // Debug: Show all arrows in scene
                    Arrow[] allArrows = FindObjectsOfType<Arrow>();
                    Debug.Log($"[DEBUG] Total arrows in scene: {allArrows.Length}");

                    if (allArrows.Length > 0)
                    {
                        foreach (var a in allArrows)
                        {
                            Collider col = a.GetComponent<Collider>();
                            Debug.Log($"  - Arrow '{a.gameObject.name}' at world pos {a.transform.position}, " +
                                     $"has collider: {col != null}, " +
                                     $"enabled: {(col != null ? col.enabled.ToString() : "N/A")}, " +
                                     $"layer: {LayerMask.LayerToName(a.gameObject.layer)}");

                            if (col != null)
                            {
                                BoxCollider box = col as BoxCollider;
                                if (box != null)
                                {
                                    Debug.Log($"    BoxCollider size: {box.size}, center: {box.center}, bounds: {box.bounds}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("[ERROR] No arrows found in scene!");
                    }
                }
            }
        }

        private void TryCollectArrow(Arrow arrow)
        {
            if (CanArrowEscape(arrow))
            {
                if (TryAddToSlot(arrow.Color, arrow.GetAmmoAmount()))
                {
                    // Remove from grid
                    var occupied = arrow.GetOccupiedCells();
                    foreach (var c in occupied)
                    {
                        if (IsValidCell(c.x, c.y) && arrowGrid[c.x, c.y] == arrow)
                        {
                            arrowGrid[c.x, c.y] = null;
                        }
                    }

                    // Optional: Animate arrow moving to slot before destroying
                    Destroy(arrow.gameObject);
                }
            }
        }

        private bool CanArrowEscape(Arrow arrow)
        {
            int cx = arrow.GridX;
            int cy = arrow.GridY;
            int dx = 0, dy = 0;

            switch (arrow.ArrowDirection)
            {
                case Direction.Up: dy = -1; break;
                case Direction.Down: dy = 1; break;
                case Direction.Left: dx = -1; break;
                case Direction.Right: dx = 1; break;
            }

            int nx = cx + dx;
            int ny = cy + dy;

            while (IsValidCell(nx, ny))
            {
                if (arrowGrid[nx, ny] != null) return false;
                nx += dx;
                ny += dy;
            }

            return true;
        }

        private bool TryAddToSlot(BlockColor color, int amount)
        {
            // Find first empty slot
            foreach (var slot in slots)
            {
                if (!slot.IsOccupied)
                {
                    slot.FillSlot(color, amount);
                    return true;
                }
            }
            return false;
        }

        private void HandleShooting()
        {
            shootTimer += Time.deltaTime;
            if (shootTimer < fireRate) return;

            // ALL OCCUPIED SLOTS SHOOT SIMULTANEOUSLY
            bool anySlotShot = false;
            foreach (var slot in slots)
            {
                if (slot.IsOccupied)
                {
                    if (TryDestroyBlock(slot.CurrentColor))
                    {
                        slot.UseAmmo(1);
                        anySlotShot = true;
                    }
                }
            }

            if (anySlotShot)
            {
                shootTimer = 0;
            }
        }

        private bool TryDestroyBlock(BlockColor color)
        {
            // Find bottom-most exposed block of matching color
            for (int x = 0; x < wallWidth; x++)
            {
                for (int y = 0; y < wallHeight; y++)
                {
                    Block b = wallGrid[x, y];
                    if (b != null)
                    {
                        if (b.Color == color)
                        {
                            DestroyBlock(x, y);
                            return true;
                        }
                        break; // Column blocked
                    }
                }
            }
            return false;
        }

        private void DestroyBlock(int x, int y)
        {
            Block b = wallGrid[x, y];
            if (b) Destroy(b.gameObject);
            wallGrid[x, y] = null;

            // Gravity: Move blocks above down (2D-style animation)
            for (int k = y + 1; k < wallHeight; k++)
            {
                Block above = wallGrid[x, k];
                if (above != null)
                {
                    wallGrid[x, k - 1] = above;
                    wallGrid[x, k] = null;
                    above.UpdateGridPosition(x, k - 1, GetWallWorldPosition(x, k - 1)); // Pass centered world position
                }
            }
        }

        private void CheckWinCondition()
        {
            bool hasBlocks = false;
            foreach (var b in wallGrid) if (b != null) hasBlocks = true;

            if (!hasBlocks)
            {
                Debug.Log("Victory!");
                isGameOver = true;
                return;
            }

            // Loss: Slots full and no matching blocks
            bool slotsFull = true;
            foreach (var s in slots) if (!s.IsOccupied) slotsFull = false;

            if (slotsFull)
            {
                bool canAnySlotShoot = false;
                foreach (var slot in slots)
                {
                    if (CanHitAnyBlock(slot.CurrentColor))
                    {
                        canAnySlotShoot = true;
                        break;
                    }
                }

                if (!canAnySlotShoot)
                {
                    Debug.Log("Game Over: All Slots Full and Stuck!");
                    isGameOver = true;
                    return;
                }
            }

            // Loss: Out of ammo
            bool arrowsRemaining = false;
            foreach (var a in arrowGrid) if (a != null) arrowsRemaining = true;

            bool slotsEmpty = true;
            foreach (var s in slots) if (s.IsOccupied) slotsEmpty = false;

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
                for (int y = 0; y < wallHeight; y++)
                {
                    Block b = wallGrid[x, y];
                    if (b != null)
                    {
                        if (b.Color == color) return true;
                        break;
                    }
                }
            }
            return false;
        }

        public void SaveCurrentLevel(string levelName)
        {
            LevelData newData = levelGenerator.GenerateRandomLevel(1, wallWidth, wallHeight, arrowRows, arrowCols);
            newData.levelName = levelName;
            SaveSystem.SaveLevel(newData, levelName);
        }
    }
}
