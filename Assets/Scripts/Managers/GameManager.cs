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
    /// Logic updated to use LevelManager for level progression.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private LevelManager levelManager;
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

            if (levelManager == null)
            {
                levelManager = FindObjectOfType<LevelManager>();
            }

            if (levelManager != null)
            {
                LoadCurrentLevel();
            }
            else
            {
                Debug.LogError("[GAMEMANAGER] No LevelManager assigned or found in scene!");
            }

            Debug.Log($"[INPUT SYSTEM] Mouse.current is: {(Mouse.current != null ? "AVAILABLE" : "NULL - INPUT SYSTEM NOT CONFIGURED!")}");
        }

        private void LoadCurrentLevel()
        {
            if (levelManager == null) return;

            LevelData data = levelManager.GetCurrentLevel();
            if (data != null)
            {
                Debug.Log($"[GAMEMANAGER] Loading level: {data.name}");
                isGameOver = false;
                BuildLevel(data);
            }
            else
            {
                Debug.LogError("[GAMEMANAGER] LevelManager returned no level data!");
            }
        }

        private void LoadNextLevel()
        {
            Debug.Log("[GAMEMANAGER] Loading Next Level...");
            // Clear slot visuals but keep them tracking occupancy if needed? 
            // Better to reset slots completely for clean slate.
            foreach (var s in slots) s.ClearSlot();

            if (levelManager.AdvanceLevel())
            {
                LoadCurrentLevel();
            }
            else
            {
                Debug.Log("All Levels Completed!");
                // Optionally show end game screen
                LoadCurrentLevel(); // Replay last level or loop
            }
        }

        private void InitializeSlots()
        {
            slots.Clear();
            // Clear existing children in slotsContainer in case of restart
            foreach (Transform child in slotsContainer) Destroy(child.gameObject);

            // Create 5 slots
            for (int i = 0; i < 5; i++)
            {
                Slot s = Instantiate(slotPrefab, slotsContainer);
                s.transform.localPosition = new Vector3((i - 2) * 1.2f, 0, 0); // Spread horizontally
                s.Initialize();
                slots.Add(s);
            }
        }

        private void BuildLevel(LevelData data)
        {
            // Clear existing
            foreach (Transform t in wallContainer) Destroy(t.gameObject);
            foreach (Transform t in arrowContainer) Destroy(t.gameObject);

            // Update AutoScaler with new level dimensions
            AutoScaler scaler = GetComponent<AutoScaler>();
            if (scaler != null)
            {
                scaler.UpdateSettings(data.width, data.height, data.gridCols, data.gridRows, cellSize);
            }

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
                a.Init((BlockColor)ad.colorIndex, (Direction)ad.direction, ad.length, ad.gridX, ad.gridY, ad.segments, cellSize);
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

        // Helper: Convert wall grid coordinates to centered world position (Horizontal only)
        // Vertical is anchored to bottom (y=0 in local space is bottom row)
        private Vector3 GetWallWorldPosition(int gridX, int gridY)
        {
            float offsetX = -(wallWidth / 2f) * cellSize + (cellSize / 2f);
            float offsetY = gridY * cellSize + (cellSize / 2f); // Bottom-anchored
            return new Vector3(gridX * cellSize + offsetX, offsetY, 0);
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
            bool inputDetected = false;
            Vector2 inputPosition = Vector2.zero;

            // Check for Mouse input (Game view, desktop)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                inputDetected = true;
                inputPosition = Mouse.current.position.ReadValue();
                Debug.Log($"[INPUT] MOUSE clicked at: {inputPosition}");
            }
            // Check for Touch input (Simulator view, mobile)
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                inputDetected = true;
                inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                Debug.Log($"[INPUT] TOUCH detected at: {inputPosition}");
            }

            if (inputDetected)
            {
                // 3D Physics Raycast (no 2D physics)
                Ray ray = Camera.main.ScreenPointToRay(inputPosition);
                // Increase raycast distance significantly for orthographic camera
                float raycastDistance = 1000f;

                // Try raycast with no layer mask
                if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
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
            if (!CanArrowEscape(arrow))
            {
                Debug.Log($"[BLOCKED] Arrow is blocked and cannot escape!");
                return;
            }

            // Find empty slot
            Slot targetSlot = null;
            foreach (var slot in slots)
            {
                if (!slot.IsOccupied)
                {
                    targetSlot = slot;
                    break;
                }
            }

            if (targetSlot == null)
            {
                Debug.Log($"[FAILED] No empty slots!");
                return;
            }

            // Remove from grid immediately (visual stays for animation)
            var occupied = arrow.GetOccupiedCells();
            foreach (var c in occupied)
            {
                if (IsValidCell(c.x, c.y) && arrowGrid[c.x, c.y] == arrow)
                {
                    arrowGrid[c.x, c.y] = null;
                }
            }

            // Calculate exit direction
            Vector3 exitDirection = Vector3.zero;
            switch (arrow.ArrowDirection)
            {
                case Direction.Up: exitDirection = Vector3.up; break;
                case Direction.Down: exitDirection = Vector3.down; break;
                case Direction.Left: exitDirection = Vector3.left; break;
                case Direction.Right: exitDirection = Vector3.right; break;
            }

            // Calculate arrow grid bounds in world space
            float gridCenterY = arrowContainer.position.y;
            float gridHalfHeight = (arrowRows * cellSize) / 2f;
            float gridBottom = gridCenterY - gridHalfHeight;
            float gridTop = gridCenterY + gridHalfHeight;

            float gridCenterX = arrowContainer.position.x;
            float gridHalfWidth = (arrowCols * cellSize) / 2f;
            float gridLeft = gridCenterX - gridHalfWidth;
            float gridRight = gridCenterX + gridHalfWidth;

            // Calculate exit position based on arrow direction
            Vector3 arrowWorldPos = arrow.transform.position;
            Vector3 exitTargetPos = arrowWorldPos;

            switch (arrow.ArrowDirection)
            {
                case Direction.Up:
                    exitTargetPos.y = gridTop + 2f; // Up maps to Top
                    break;
                case Direction.Down:
                    exitTargetPos.y = gridBottom - 2f; // Down maps to Bottom
                    break;
                case Direction.Left:
                    exitTargetPos.x = gridLeft - 2f; // Exit left
                    break;
                case Direction.Right:
                    exitTargetPos.x = gridRight + 2f; // Exit right
                    break;
            }

            Vector3 slotWorldPos = targetSlot.transform.position;
            BlockColor arrowColor = arrow.Color;

            // Note: We don't increment currentAmmo here immediately, we wait for callbacks
            // But we need to track it locally for this collection sequence? 
            // In the previous version, currentAmmo was local to the callback. 
            // Yes, GameManager tracks total slots, but the individual collection tracks its count for filling.
            // Wait, previous code:
            // int currentAmmo = 0; -> inside TryCollectArrow
            // then modified by callbacks.

            // Re-implementing correctly:
            int currentAnimationAmmo = 0; // Local counter for this arrow's collection

            arrow.AnimateCollection(
                exitDirection,
                exitTargetPos,
                slotWorldPos,
                // Head Arrival callback
                () =>
                {
                    targetSlot.FillSlot(arrowColor, 0);
                    currentAnimationAmmo = 0;
                },
                // Ammo increment callback
                (int ammoToAdd) =>
                {
                    currentAnimationAmmo += ammoToAdd;
                    targetSlot.FillSlot(arrowColor, currentAnimationAmmo);
                },
                // Complete callback
                () =>
                {
                    targetSlot.FillSlot(arrowColor, currentAnimationAmmo);
                    Destroy(arrow.gameObject);
                    CheckWinCondition();
                });
        }

        private bool CanArrowEscape(Arrow arrow)
        {
            var segments = arrow.GetOccupiedCells();
            int dx = 0, dy = 0;

            switch (arrow.ArrowDirection)
            {
                case Direction.Up: dy = 1; break;
                case Direction.Down: dy = -1; break;
                case Direction.Left: dx = -1; break;
                case Direction.Right: dx = 1; break;
            }

            // A rigid arrow can escape if ALL segments have a clear path to the edge
            // along the movement direction.
            foreach (var seg in segments)
            {
                int nx = seg.x + dx;
                int ny = seg.y + dy;

                while (IsValidCell(nx, ny))
                {
                    // If cell is occupied, check if it's by the SAME arrow
                    if (arrowGrid[nx, ny] != null && arrowGrid[nx, ny] != arrow)
                    {
                        Debug.Log($"[ESCAPE BLOCKED] Arrow at ({arrow.GridX}, {arrow.GridY}) blocked at ({nx}, {ny}) by {arrowGrid[nx, ny].name} at ({arrowGrid[nx, ny].GridX}, {arrowGrid[nx, ny].GridY})");
                        return false;
                    }
                    nx += dx;
                    ny += dy;
                }
            }

            return true;
        }

        private bool TryAddToSlot(BlockColor color, int amount)
        {
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
                CheckWinCondition();
            }
        }

        private bool TryDestroyBlock(BlockColor color)
        {
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

            for (int k = y + 1; k < wallHeight; k++)
            {
                Block above = wallGrid[x, k];
                if (above != null)
                {
                    wallGrid[x, k - 1] = above;
                    wallGrid[x, k] = null;
                    above.UpdateGridPosition(x, k - 1, GetWallWorldPosition(x, k - 1));
                }
            }
        }

        private void CheckWinCondition()
        {
            if (isGameOver) return;

            bool hasBlocks = false;
            foreach (var b in wallGrid) if (b != null) hasBlocks = true;

            if (!hasBlocks)
            {
                Debug.Log("🎉 VICTORY! All blocks destroyed!");
                isGameOver = true;
                StartCoroutine(VictoryRoutine());
                return;
            }

            bool slotsFull = true;
            foreach (var s in slots) if (!s.IsOccupied) slotsFull = false;

            if (slotsFull)
            {
                bool canAnySlotShoot = false;
                foreach (var slot in slots)
                {
                    if (slot.IsOccupied && CanHitAnyBlock(slot.CurrentColor))
                    {
                        canAnySlotShoot = true;
                        break;
                    }
                }

                if (!canAnySlotShoot)
                {
                    Debug.Log("💀 GAME OVER: All Slots Full, No Matching Blocks!");
                    isGameOver = true;
                    return;
                }
            }

            bool arrowsRemaining = false;
            foreach (var a in arrowGrid) if (a != null) arrowsRemaining = true;

            bool slotsEmpty = true;
            foreach (var s in slots) if (s.IsOccupied) slotsEmpty = false;

            if (!arrowsRemaining && slotsEmpty && hasBlocks)
            {
                Debug.Log("💀 GAME OVER: Out of Ammo!");
                isGameOver = true;
            }
        }

        private IEnumerator VictoryRoutine()
        {
            yield return new WaitForSeconds(2.0f);
            LoadNextLevel();
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

        // Debug display to show input status on screen
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;

            GUI.Label(new Rect(10, 10, 500, 30), $"GameManager Running: YES", style);
            if (levelManager != null && levelManager.GetCurrentLevel() != null)
            {
                GUI.Label(new Rect(10, 40, 500, 30), $"Level: {levelManager.GetCurrentLevel().name}", style);
            }

            GUI.Label(new Rect(10, 70, 700, 30), $"Mouse: {(Mouse.current != null ? "YES" : "NO")} | Touch: {(Touchscreen.current != null ? "YES" : "NO")}", style);
            GUI.Label(new Rect(10, 100, 500, 30), $"Arrows in scene: {FindObjectsOfType<Arrow>().Length}", style);
            GUI.Label(new Rect(10, 130, 500, 30), $"Click anywhere to test", style);

            if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                GUI.Label(new Rect(10, 160, 500, 30), $"Mouse: {mousePos}", style);
            }
        }
    }
}
