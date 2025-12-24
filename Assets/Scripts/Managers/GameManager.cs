using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ArrowBlast.Core;
using ArrowBlast.Data;
using ArrowBlast.Game;

namespace ArrowBlast.Managers
{
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
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private Material projectileMaterial;
        [SerializeField] private float projectileSpeed = 15f;

        // Runtime Data
        private Block[,] wallGrid;
        private Arrow[,] arrowGrid;
        private List<Slot> slots = new List<Slot>();
        private int wallWidth, wallHeight;
        private int arrowRows, arrowCols;

        private float shootTimer;
        private bool isGameOver;

        public void RestartLevel()
        {
            LoadCurrentLevel();
        }

        private void Start()
        {
            InitializeSlots();
            if (levelManager == null) levelManager = FindObjectOfType<LevelManager>();
            // if (levelManager != null) LoadCurrentLevel(); // Removed: Load only from UI
        }

        private void LoadCurrentLevel()
        {
            if (levelManager == null) return;
            LevelData data = levelManager.GetCurrentLevel();
            if (data != null)
            {
                isGameOver = false;
                BuildLevel(data);
            }
        }

        private void LoadNextLevel()
        {
            foreach (var s in slots) s.ClearSlot();
            if (levelManager.AdvanceLevel()) LoadCurrentLevel();
            else LoadCurrentLevel(); // Loop
        }

        private void InitializeSlots()
        {
            slots.Clear();
            foreach (Transform child in slotsContainer) Destroy(child.gameObject);
            for (int i = 0; i < 5; i++)
            {
                Slot s = Instantiate(slotPrefab, slotsContainer);
                s.transform.localPosition = new Vector3((i - 2) * 1.2f, 0, 0);
                s.Initialize();
                slots.Add(s);
            }
        }

        private void BuildLevel(LevelData data)
        {
            foreach (Transform t in wallContainer) Destroy(t.gameObject);
            foreach (Transform t in arrowContainer) Destroy(t.gameObject);

            AutoScaler scaler = GetComponent<AutoScaler>();
            if (scaler != null) scaler.UpdateSettings(data.width, data.height, data.gridCols, data.gridRows, cellSize);

            wallWidth = data.width;
            wallHeight = data.height;
            wallGrid = new Block[wallWidth, wallHeight];

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

            foreach (var ad in data.arrows)
            {
                Arrow a = Instantiate(arrowPrefab, arrowContainer);
                a.Init((BlockColor)ad.colorIndex, (Direction)ad.direction, ad.length, ad.gridX, ad.gridY, ad.segments, cellSize);
                a.transform.localPosition = GetArrowWorldPosition(ad.gridX, ad.gridY);
                var occupied = a.GetOccupiedCells();
                foreach (var c in occupied)
                    if (IsValidCell(c.x, c.y)) arrowGrid[c.x, c.y] = a;
            }
        }

        private bool IsValidCell(int x, int y) => x >= 0 && x < arrowCols && y >= 0 && y < arrowRows;

        private Vector3 GetWallWorldPosition(int gridX, int gridY)
        {
            float offsetX = -(wallWidth / 2f) * cellSize + (cellSize / 2f);
            float offsetY = gridY * cellSize + (cellSize / 2f);
            return new Vector3(gridX * cellSize + offsetX, offsetY, 0);
        }

        private Vector3 GetArrowWorldPosition(int gridX, int gridY)
        {
            float offsetX = -(arrowCols / 2f) * cellSize + (cellSize / 2f);
            float offsetY = -(arrowRows / 2f) * cellSize + (cellSize / 2f);
            return new Vector3(gridX * cellSize + offsetX, gridY * cellSize + offsetY, 0);
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
            bool inputDetected = false;
            Vector2 inputPosition = Vector2.zero;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                inputDetected = true;
                inputPosition = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                inputDetected = true;
                inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (inputDetected)
            {
                Ray ray = Camera.main.ScreenPointToRay(inputPosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    Arrow arrow = hit.collider.GetComponent<Arrow>();
                    if (arrow != null) TryCollectArrow(arrow);
                }
            }
        }

        private void TryCollectArrow(Arrow arrow)
        {
            if (!CanArrowEscape(arrow))
            {
                arrow.AnimateBlocked();
                return;
            }

            Slot targetSlot = null;
            foreach (var slot in slots)
                if (!slot.IsOccupied && !slot.IsReserved) { targetSlot = slot; break; }

            if (targetSlot == null) return;

            targetSlot.SetReserved(true);
            var occupied = arrow.GetOccupiedCells();
            foreach (var c in occupied)
                if (IsValidCell(c.x, c.y) && arrowGrid[c.x, c.y] == arrow) arrowGrid[c.x, c.y] = null;

            Vector3 exitDirection = Vector3.zero;
            switch (arrow.ArrowDirection)
            {
                case Direction.Up: exitDirection = Vector3.up; break;
                case Direction.Down: exitDirection = Vector3.down; break;
                case Direction.Left: exitDirection = Vector3.left; break;
                case Direction.Right: exitDirection = Vector3.right; break;
            }

            float gridCenterY = arrowContainer.position.y;
            float gridHalfHeight = (arrowRows * cellSize) / 2f;
            float gridBottom = gridCenterY - gridHalfHeight;
            float gridTop = gridCenterY + gridHalfHeight;
            float gridCenterX = arrowContainer.position.x;
            float gridHalfWidth = (arrowCols * cellSize) / 2f;
            float gridLeft = gridCenterX - gridHalfWidth;
            float gridRight = gridCenterX + gridHalfWidth;

            Vector3 arrowWorldPos = arrow.transform.position;
            Vector3 exitTargetPos = arrowWorldPos;
            switch (arrow.ArrowDirection)
            {
                case Direction.Up: exitTargetPos.y = gridTop + 2f; break;
                case Direction.Down: exitTargetPos.y = gridBottom - 2f; break;
                case Direction.Left: exitTargetPos.x = gridLeft - 2f; break;
                case Direction.Right: exitTargetPos.x = gridRight + 2f; break;
            }

            Vector3 slotWorldPos = targetSlot.transform.position;
            BlockColor arrowColor = arrow.Color;
            arrow.AnimateCollection(exitDirection, exitTargetPos, slotWorldPos,
                () => { targetSlot.InitializeCollection(arrowColor); },
                (int ammoToAdd) => { targetSlot.AddAmmo(ammoToAdd); },
                () => { targetSlot.FinalizeCollection(); Destroy(arrow.gameObject); CheckWinCondition(); });
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

            foreach (var seg in segments)
            {
                int nx = seg.x + dx;
                int ny = seg.y + dy;
                while (IsValidCell(nx, ny))
                {
                    if (arrowGrid[nx, ny] != null && arrowGrid[nx, ny] != arrow) return false;
                    nx += dx; ny += dy;
                }
            }
            return true;
        }

        private void HandleShooting()
        {
            shootTimer += Time.deltaTime;
            if (shootTimer < fireRate) return;

            bool anySlotShot = false;
            foreach (var slot in slots)
            {
                if (slot.IsOccupied && slot.AmmoCount > 0)
                {
                    if (TryShootProjectile(slot))
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

        private bool TryShootProjectile(Slot slot)
        {
            BlockColor color = slot.CurrentColor;
            for (int x = 0; x < wallWidth; x++)
            {
                for (int y = 0; y < wallHeight; y++)
                {
                    Block b = wallGrid[x, y];
                    if (b != null)
                    {
                        if (b.Color == color)
                        {
                            Vector3 targetPos = b.transform.position;
                            int targetX = x;
                            int targetY = y;
                            wallGrid[x, y] = null;

                            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            sphere.name = $"Projectile_{color}";
                            sphere.transform.position = slot.transform.position;
                            sphere.transform.localScale = Vector3.one * 0.4f;
                            Destroy(sphere.GetComponent<SphereCollider>());
                            Projectile proj = sphere.AddComponent<Projectile>();

                            Color vColor = GetBlockColorVisual(color);
                            proj.Initialize(color, projectileMaterial, vColor, () =>
                            {
                                if (b != null) FinalizeBlockDestruction(targetX, targetY, b);
                            });

                            float distance = Vector3.Distance(slot.transform.position, targetPos);
                            float duration = distance / projectileSpeed;
                            proj.Launch(targetPos, duration);
                            return true;
                        }
                        break;
                    }
                }
            }
            return false;
        }

        private void FinalizeBlockDestruction(int x, int y, Block targetBlock)
        {
            if (targetBlock != null) Destroy(targetBlock.gameObject);

            // Robust Column Collapse: Repack all stationary blocks to the bottom
            List<Block> columnBlocks = new List<Block>();
            for (int k = 0; k < wallHeight; k++)
            {
                if (wallGrid[x, k] != null)
                {
                    columnBlocks.Add(wallGrid[x, k]);
                    wallGrid[x, k] = null;
                }
            }

            for (int k = 0; k < columnBlocks.Count; k++)
            {
                wallGrid[x, k] = columnBlocks[k];
                wallGrid[x, k].UpdateGridPosition(x, k, GetWallWorldPosition(x, k));
            }

            CheckWinCondition();
        }

        private Color GetBlockColorVisual(BlockColor color)
        {
            switch (color)
            {
                case BlockColor.Red: return Color.red;
                case BlockColor.Blue: return Color.blue;
                case BlockColor.Green: return Color.green;
                case BlockColor.Yellow: return Color.yellow;
                case BlockColor.Purple: return new Color(0.5f, 0f, 0.5f);
                case BlockColor.Orange: return new Color(1f, 0.5f, 0f);
                default: return Color.white;
            }
        }

        private void CheckWinCondition()
        {
            if (isGameOver || wallGrid == null) return;

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
            foreach (var s in slots) if (!s.IsOccupied && !s.IsReserved) slotsFull = false;

            if (slotsFull)
            {
                bool anyReserved = false;
                foreach (var s in slots) if (s.IsReserved) anyReserved = true;

                if (!anyReserved)
                {
                    bool canAnySlotShoot = false;
                    foreach (var slot in slots)
                        if (slot.IsOccupied && CanHitAnyBlock(slot.CurrentColor)) { canAnySlotShoot = true; break; }

                    if (!canAnySlotShoot)
                    {
                        Debug.Log("💀 GAME OVER: All Slots Full, No Matching Blocks!");
                        isGameOver = true;
                        return;
                    }
                }
                else return;
            }

            bool arrowsRemaining = false;
            foreach (var a in arrowGrid) if (a != null) arrowsRemaining = true;
            bool slotsEmpty = true;
            foreach (var s in slots) if (s.IsOccupied || s.IsReserved) slotsEmpty = false;

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
    }
}
