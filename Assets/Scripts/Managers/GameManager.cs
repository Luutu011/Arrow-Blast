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
        [SerializeField] private Transform projectileContainer;
        [SerializeField] private BoosterUIManager boosterUIManager;
        [SerializeField] private ArrowBlast.UI.MainMenu mainMenu;

        [Header("Prefabs")]
        [SerializeField] private Block blockPrefab;
        [SerializeField] private Arrow arrowPrefab;
        [SerializeField] private Slot slotPrefab;
        [SerializeField] private KeyBlock keyBlockPrefab;
        [SerializeField] private LockObstacle lockObstaclePrefab;
        [SerializeField] private Projectile projectilePrefab;

        [Header("Settings")]
        [SerializeField] private float cellSize = 0.8f;
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private Material projectileMaterial;
        [SerializeField] private float projectileSpeed = 3f;
        [SerializeField] private int initialProjectilePoolSize = 20;

        // Runtime Data
        private Block[,] wallGrid;
        private Arrow[,] arrowGrid;
        private List<Slot> slots = new List<Slot>();
        private List<KeyBlock> activeKeys = new List<KeyBlock>();
        private List<LockObstacle> activeLocks = new List<LockObstacle>();
        private int wallWidth, wallHeight;
        private int arrowRows, arrowCols;

        private float shootTimer;
        private bool isGameOver;

        // Object Pooling
        private Queue<Block> blockPool = new Queue<Block>();
        private Queue<Projectile> projectilePool = new Queue<Projectile>();
        private List<object>[] columnData;
        private const int ACTIVE_COL_HEIGHT = 12;
        private const int VISIBLE_COL_HEIGHT = 8;

        // Booster State
        private bool isInstantExitActive;
        private bool extraSlotUsedThisLevel;

        public void RestartLevel()
        {
            LoadCurrentLevel();
        }

        private void Start()
        {
            InitializeSlots();
            if (slotsContainer != null) slotsContainer.gameObject.SetActive(false); // Hide on start
            if (boosterUIManager != null)
            {
                boosterUIManager.Initialize(this);
                boosterUIManager.SetVisible(false); // Hide boosters on menu
            }
            if (levelManager == null) levelManager = FindObjectOfType<LevelManager>();
            InitializeProjectilePool();
            // if (levelManager != null) LoadCurrentLevel(); // Removed: Load only from UI
        }

        private void LoadCurrentLevel()
        {
            if (levelManager == null) return;
            LevelData data = levelManager.GetCurrentLevel();
            if (data != null)
            {
                isGameOver = false;
                extraSlotUsedThisLevel = false;
                isInstantExitActive = false;
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
                s.Initialize();
                slots.Add(s);
            }
            UpdateSlotPositions();
        }

        private void UpdateSlotPositions()
        {
            float spacing = 1.2f;
            float startX = -((slots.Count - 1) * spacing) / 2f;
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].transform.localPosition = new Vector3(startX + i * spacing, 0, 0);
            }
        }

        public void ToggleInstantExitBooster()
        {
            if (isGameOver) return;
            isInstantExitActive = !isInstantExitActive;
            SetArrowsScared(isInstantExitActive);

            if (boosterUIManager != null)
                boosterUIManager.UpdateInstantExitVisual(isInstantExitActive);

            //Debug.Log($"Booster: Instant Exit {(isInstantExitActive ? "Active" : "Deactivated")}!");
        }

        private void SetArrowsScared(bool scared)
        {
            HashSet<Arrow> processed = new HashSet<Arrow>();
            foreach (var a in arrowGrid)
            {
                if (a != null && !processed.Contains(a))
                {
                    a.SetScared(scared);
                    processed.Add(a);
                }
            }
        }

        public void UseExtraSlotBooster()
        {
            if (isGameOver || extraSlotUsedThisLevel) return;
            extraSlotUsedThisLevel = true;

            Slot s = Instantiate(slotPrefab, slotsContainer);
            s.Initialize();
            slots.Add(s);
            UpdateSlotPositions();
            //Debug.Log("Booster: Extra Slot Added!");
        }

        private void BuildLevel(LevelData data)
        {
            if (slotsContainer != null) slotsContainer.gameObject.SetActive(true); // Show when level starts
            if (boosterUIManager != null) boosterUIManager.SetVisible(true); // Show boosters when level starts

            foreach (Transform t in wallContainer) Destroy(t.gameObject);
            foreach (Transform t in arrowContainer) Destroy(t.gameObject);
            activeKeys.Clear();
            activeLocks.Clear();

            AutoScaler scaler = GetComponent<AutoScaler>();
            if (scaler != null) scaler.UpdateSettings(data.width, VISIBLE_COL_HEIGHT, data.gridCols, data.gridRows, cellSize);

            wallWidth = data.width;
            wallHeight = ACTIVE_COL_HEIGHT;
            wallGrid = new Block[wallWidth, wallHeight];

            InitializeColumnData(data); // Pre-sort all blocks and keys

            for (int x = 0; x < wallWidth; x++)
            {
                RefillColumn(x); // Fills up to 12 rows (8 visible + 4 buffer)
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

            foreach (var ld in data.locks)
            {
                LockObstacle l = Instantiate(lockObstaclePrefab, arrowContainer);
                // Correct multi-cell centering
                Vector3 basePos = GetArrowWorldPosition(ld.gridX, ld.gridY);
                float offsetX = (ld.sizeX - 1) * 0.5f * cellSize;
                float offsetY = (ld.sizeY - 1) * 0.5f * cellSize;
                l.transform.localPosition = basePos + new Vector3(offsetX, offsetY, -0.49f);

                l.Init(ld.gridX, ld.gridY, ld.sizeX, ld.sizeY, ld.lockId, cellSize);
                activeLocks.Add(l);
            }
        }

        private void InitializeColumnData(LevelData data)
        {
            columnData = new List<object>[wallWidth];
            for (int i = 0; i < wallWidth; i++) columnData[i] = new List<object>();

            List<object> all = new List<object>();
            foreach (var b in data.blocks) all.Add(b);
            foreach (var k in data.keys) all.Add(k);

            // Sort by gridY so lowest fall in first
            all.Sort((a, b) =>
            {
                int ya = (a is BlockData ba) ? ba.gridY : ((KeyData)a).gridY;
                int yb = (b is BlockData bb) ? bb.gridY : ((KeyData)b).gridY;
                return ya.CompareTo(yb);
            });

            foreach (var item in all)
            {
                int x = (item is BlockData ba) ? ba.gridX : ((KeyData)item).gridX;
                if (x >= 0 && x < wallWidth) columnData[x].Add(item);
            }
        }

        private void RefillColumn(int x)
        {
            int currentItems = 0;
            for (int k = 0; k < ACTIVE_COL_HEIGHT; k++)
            {
                if (wallGrid[x, k] != null) currentItems++;
            }
            currentItems += activeKeys.FindAll(k => k.GridX == x).Count;

            while (currentItems < ACTIVE_COL_HEIGHT && columnData[x].Count > 0)
            {
                object itemData = columnData[x][0];
                columnData[x].RemoveAt(0);

                if (itemData is BlockData bd)
                {
                    Block b = GetBlockFromPool();
                    b.Init((BlockColor)bd.colorIndex, x, currentItems, bd.isTwoColor, (BlockColor)bd.secondaryColorIndex);
                    // Start from above the visible area
                    b.transform.localPosition = GetWallWorldPosition(x, ACTIVE_COL_HEIGHT);
                    wallGrid[x, currentItems] = b;
                    b.UpdateGridPosition(x, currentItems, GetWallWorldPosition(x, currentItems));
                }
                else if (itemData is KeyData kd)
                {
                    KeyBlock k = Instantiate(keyBlockPrefab, wallContainer);
                    k.Init(x, currentItems, kd.lockId);
                    k.transform.localPosition = GetWallWorldPosition(x, ACTIVE_COL_HEIGHT);
                    activeKeys.Add(k);
                    k.UpdateGridPosition(x, currentItems, GetWallWorldPosition(x, currentItems));
                    if (currentItems == 0) StartCoroutine(DelayedUnlock(k, x));
                }
                currentItems++;
            }
        }

        private Block GetBlockFromPool()
        {
            if (blockPool.Count > 0)
            {
                Block b = blockPool.Dequeue();
                b.gameObject.SetActive(true);
                return b;
            }
            return Instantiate(blockPrefab, wallContainer);
        }

        private void ReturnBlockToPool(Block b)
        {
            if (b == null) return;
            b.gameObject.SetActive(false);
            blockPool.Enqueue(b);
        }

        private void InitializeProjectilePool()
        {
            if (projectilePrefab == null) return;

            // Clear existing pool if any (unlikely in Start, but good for safety)
            while (projectilePool.Count > 0)
            {
                Projectile p = projectilePool.Dequeue();
                if (p != null) Destroy(p.gameObject);
            }

            for (int i = 0; i < initialProjectilePoolSize; i++)
            {
                Projectile p = CreateNewProjectile();
                p.gameObject.SetActive(false);
                projectilePool.Enqueue(p);
            }
        }

        private Projectile CreateNewProjectile()
        {
            Projectile p = Instantiate(projectilePrefab, projectileContainer);
            return p;
        }

        private Projectile GetProjectileFromPool()
        {
            if (projectilePool.Count > 0)
            {
                Projectile p = projectilePool.Dequeue();
                p.gameObject.SetActive(true);
                return p;
            }
            return CreateNewProjectile();
        }

        private void ReturnProjectileToPool(Projectile p)
        {
            if (p == null) return;
            p.gameObject.SetActive(false);
            projectilePool.Enqueue(p);
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
            if (!isInstantExitActive && !CanArrowEscape(arrow))
            {
                arrow.AnimateBlocked();
                return;
            }

            Slot targetSlot = null;
            foreach (var slot in slots)
                if (!slot.IsOccupied && !slot.IsReserved) { targetSlot = slot; break; }

            if (targetSlot == null) return;

            if (isInstantExitActive)
            {
                isInstantExitActive = false;
                SetArrowsScared(false);
                if (boosterUIManager != null)
                    boosterUIManager.UpdateInstantExitVisual(false);
            }

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

                    foreach (var l in activeLocks)
                    {
                        if (l.IsLocked && l.BlocksCell(nx, ny)) return false;
                    }

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
                    else
                    {
                        slot.ResetRotation(); // No matching blocks, return to idle
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
                    if (b != null && !b.IsTargeted) // Only target non-targeted blocks
                    {
                        if (b.Color == color)
                        {
                            Vector3 targetPos = b.transform.position;
                            int targetX = x;
                            int targetY = y;

                            slot.RotateToward(targetPos);

                            b.IsTargeted = true; // Reserve this layer
                            bool willBeDestroyed = !b.IsTwoColor;
                            if (willBeDestroyed) wallGrid[x, y] = null;

                            Projectile proj = GetProjectileFromPool();
                            proj.name = $"Projectile_{color}";
                            proj.transform.position = slot.transform.position;
                            proj.transform.localScale = Vector3.one * 0.4f;

                            proj.Initialize(color, projectileMaterial, () =>
                            {
                                if (b != null) StartCoroutine(HandleBlockHit(targetX, targetY, b, willBeDestroyed));
                            }, ReturnProjectileToPool);

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

        private IEnumerator HandleBlockHit(int x, int y, Block b, bool willBeDestroyed)
        {
            if (willBeDestroyed)
            {
                yield return b.AnimateDeath();
                ReturnBlockToPool(b);
                FinalizeBlockDestruction(x, y, null);
            }
            else
            {
                yield return b.AnimateTransition();
                b.IsTargeted = false; // Layer transition complete, can be targeted again
                CheckWinCondition();
            }
        }

        private void FinalizeBlockDestruction(int x, int y, Block targetBlock)
        {
            // Robust Column Collapse: Repack all stationary blocks AND keys to the bottom
            List<Component> columnItems = new List<Component>();
            for (int k = 0; k < wallHeight; k++)
            {
                if (wallGrid[x, k] != null)
                {
                    columnItems.Add(wallGrid[x, k]);
                    wallGrid[x, k] = null;
                }

                KeyBlock key = activeKeys.Find(kb => kb.GridX == x && kb.GridY == k);
                if (key != null)
                {
                    columnItems.Add(key);
                }
            }

            for (int k = 0; k < columnItems.Count; k++)
            {
                Component item = columnItems[k];
                if (item is Block b)
                {
                    wallGrid[x, k] = b;
                    b.UpdateGridPosition(x, k, GetWallWorldPosition(x, k));
                }
                else if (item is KeyBlock key)
                {
                    key.UpdateGridPosition(x, k, GetWallWorldPosition(x, k));
                    if (k == 0) StartCoroutine(DelayedUnlock(key, x));
                }
            }

            RefillColumn(x); // Fill the gap at the top
            CheckWinCondition();
        }

        private IEnumerator DelayedUnlock(KeyBlock key, int x)
        {
            yield return new WaitForEndOfFrame();
            TryUnlock(key, x);
        }

        private void TryUnlock(KeyBlock key, int x)
        {
            LockObstacle targetLock = activeLocks.Find(l => l.LockId == key.LockId && l.IsLocked);
            if (targetLock != null)
            {
                targetLock.Unlock();
                StartCoroutine(ExecuteUnlockSequence(key, x));
            }
        }

        private IEnumerator ExecuteUnlockSequence(KeyBlock key, int x)
        {
            yield return key.AnimateUnlock();
            activeKeys.Remove(key);
            Destroy(key.gameObject);
            FinalizeBlockDestruction(x, -1, null); // Trigger another collapse/refill
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
                //Debug.Log("🎉 VICTORY! All blocks destroyed!");
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
                        //Debug.Log("💀 GAME OVER: All Slots Full, No Matching Blocks!");
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
                //Debug.Log("💀 GAME OVER: Out of Ammo!");
                isGameOver = true;
            }
        }

        private IEnumerator VictoryRoutine()
        {
            yield return new WaitForSeconds(2.0f);
            ReturnToLevelSelect();
        }

        private void ReturnToLevelSelect()
        {
            // Clear current game state
            foreach (Transform t in wallContainer) Destroy(t.gameObject);
            foreach (Transform t in arrowContainer) Destroy(t.gameObject);
            foreach (var s in slots) s.ClearSlot();

            if (slotsContainer != null) slotsContainer.gameObject.SetActive(false);
            if (boosterUIManager != null) boosterUIManager.SetVisible(false);

            if (mainMenu != null)
            {
                mainMenu.gameObject.SetActive(true);
                mainMenu.ShowLevelPanel();
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
    }
}
