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
        [SerializeField] private CoinSystem coinSystem;
        [SerializeField] private BoosterInventory boosterInventory;
        [SerializeField] private Transform wallContainer;
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private Transform arrowContainer;
        [SerializeField] private Transform projectileContainer;
        [SerializeField] private BoosterUIManager boosterUIManager;
        [SerializeField] private ArrowBlast.UI.MainMenu mainMenu;
        [SerializeField] private GameObject loadingPanel;

        [Header("Prefabs")]
        [SerializeField] private Block blockPrefab;
        [SerializeField] private Arrow arrowPrefab;
        [SerializeField] private Slot slotPrefab;
        [SerializeField] private KeyBlock keyBlockPrefab;
        [SerializeField] private LockObstacle lockObstaclePrefab;
        [SerializeField] private Projectile projectilePrefab;

        [Header("Settings")]
        [SerializeField] private float cellSize = 0.8f;
        [SerializeField] private float fireRate = 0.65f;
        [SerializeField] private float projectileSpeed = 2.5f;

        // Runtime Data
        private Block[,] wallGrid;
        private Arrow[,] arrowGrid;
        private List<Slot> slots = new List<Slot>();
        private List<LockObstacle> activeLocks = new List<LockObstacle>();
        private int wallWidth, wallHeight;
        private int arrowRows, arrowCols;

        private float shootTimer;
        private bool isGameOver;
        private Coroutine buildRoutine;

        // Object Pooling
        private Queue<Block> blockPool = new Queue<Block>();
        private Queue<KeyBlock> keyBlockPool = new Queue<KeyBlock>();
        [SerializeField] private List<Projectile> projectilePool = new List<Projectile>();
        private List<object>[] columnData;
        private const int ACTIVE_COL_HEIGHT = 12;
        private const int VISIBLE_COL_HEIGHT = 8;

        // Booster State
        private bool isInstantExitActive;
        private bool extraSlotUsedThisLevel;
        private int extraSlotsToLoad = 0;
        private Camera mainCamera;

        public void RestartLevel()
        {
            LoadCurrentLevel();
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = GamePalette.Background;
            }

            // Auto-find dependencies if not assigned
            if (levelManager == null) levelManager = FindObjectOfType<LevelManager>();
            if (coinSystem == null) coinSystem = FindObjectOfType<CoinSystem>();
            if (boosterInventory == null) boosterInventory = FindObjectOfType<BoosterInventory>();

            InitializeSlots();
            if (slotsContainer != null) slotsContainer.gameObject.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);

            if (boosterUIManager != null)
            {
                boosterUIManager.Initialize(this, coinSystem, boosterInventory);
                boosterUIManager.SetVisible(false);
            }
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

                if (buildRoutine != null) StopCoroutine(buildRoutine);
                buildRoutine = StartCoroutine(BuildLevelRoutine(data));
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
            int count = 5 + extraSlotsToLoad;
            for (int i = 0; i < count; i++)
            {
                Slot s = Instantiate(slotPrefab, slotsContainer);
                s.Initialize();
                slots.Add(s);
            }
            extraSlotsToLoad = 0; // Reset after use
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

            // Check if we have boosters in inventory
            if (boosterInventory == null) return;

            // If activating, check and consume from inventory
            if (!isInstantExitActive)
            {
                if (!boosterInventory.UseBooster(BoosterType.InstantExit))
                {
                    Debug.LogWarning("[GameManager] No Instant Exit boosters in inventory!");
                    return;
                }
            }

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

            // Check if we have boosters in inventory
            if (boosterInventory == null) return;

            if (!boosterInventory.UseBooster(BoosterType.ExtraSlot))
            {
                Debug.LogWarning("[GameManager] No Extra Slot boosters in inventory!");
                return;
            }

            extraSlotUsedThisLevel = true;

            Slot s = Instantiate(slotPrefab, slotsContainer);
            s.Initialize();
            slots.Add(s);
            UpdateSlotPositions();
            //Debug.Log("Booster: Extra Slot Added!");
        }

        public void RestartWithExtraSlot()
        {
            extraSlotsToLoad = 1;
            LoadCurrentLevel();
        }

        private void BuildLevel(LevelData data)
        {
            if (buildRoutine != null) StopCoroutine(buildRoutine);
            buildRoutine = StartCoroutine(BuildLevelRoutine(data));
        }

        private IEnumerator BuildLevelRoutine(LevelData data)
        {
            if (loadingPanel != null) loadingPanel.SetActive(true);

            yield return null;

            if (slotsContainer != null) slotsContainer.gameObject.SetActive(true);
            if (boosterUIManager != null) boosterUIManager.SetVisible(true);

            // Return active blocks to pool instead of destroying
            foreach (Transform t in wallContainer)
            {
                var b = t.GetComponent<Block>();
                if (b != null && t.gameObject.activeSelf) ReturnBlockToPool(b);
                else if (t.gameObject.activeSelf) Destroy(t.gameObject);
            }

            foreach (Transform t in arrowContainer) Destroy(t.gameObject);
            foreach (var p in projectilePool) if (p != null) p.gameObject.SetActive(false);

            activeLocks.Clear();
            InitializeSlots();

            yield return null;

            AutoScaler scaler = GetComponent<AutoScaler>();
            if (scaler != null) scaler.UpdateSettings(data.width, VISIBLE_COL_HEIGHT, data.gridCols, data.gridRows, cellSize);

            wallWidth = data.width;
            wallHeight = ACTIVE_COL_HEIGHT;
            wallGrid = new Block[wallWidth, wallHeight];

            InitializeColumnData(data);
            yield return null;

            for (int x = 0; x < wallWidth; x++)
            {
                RefillColumn(x);
                yield return null;
            }

            arrowRows = data.gridRows;
            arrowCols = data.gridCols;
            arrowGrid = new Arrow[arrowCols, arrowRows];

            int count = 0;
            foreach (var ad in data.arrows)
            {
                Arrow a = Instantiate(arrowPrefab, arrowContainer);
                a.Init((BlockColor)ad.colorIndex, (Direction)ad.direction, ad.length, ad.gridX, ad.gridY, ad.segments, cellSize);
                a.transform.localPosition = GetArrowWorldPosition(ad.gridX, ad.gridY);
                var occupied = a.GetOccupiedCells();
                foreach (var c in occupied)
                    if (IsValidCell(c.x, c.y)) arrowGrid[c.x, c.y] = a;

                count++;
                if (count % 2 == 0) yield return null;
            }

            foreach (var ld in data.locks)
            {
                LockObstacle l = Instantiate(lockObstaclePrefab, arrowContainer);
                Vector3 basePos = GetArrowWorldPosition(ld.gridX, ld.gridY);
                float offsetX = (ld.sizeX - 1) * 0.5f * cellSize;
                float offsetY = (ld.sizeY - 1) * 0.5f * cellSize;
                l.transform.localPosition = basePos + new Vector3(offsetX, offsetY, -0.49f);
                l.Init(ld.gridX, ld.gridY, ld.sizeX, ld.sizeY, ld.lockId, cellSize);
                activeLocks.Add(l);
            }

            if (TutorialManager.Instance != null && levelManager != null)
            {
                TutorialManager.Instance.CheckTutorials(levelManager.CurrentLevelIndex);
            }

            yield return new WaitForSeconds(0.1f);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            buildRoutine = null;
        }

        private void InitializeColumnData(LevelData data)
        {
            columnData = new List<object>[wallWidth];
            for (int i = 0; i < wallWidth; i++) columnData[i] = new List<object>();

            List<object> all = new List<object>();
            foreach (var b in data.blocks) all.Add(b);

            // Sort by gridY so lowest fall in first
            all.Sort((a, b) =>
            {
                int ya = (a is BlockData ba) ? ba.gridY : 0;
                int yb = (b is BlockData bb) ? bb.gridY : 0;
                return ya.CompareTo(yb);
            });

            foreach (var item in all)
            {
                if (item is BlockData bd)
                {
                    if (bd.gridX >= 0 && bd.gridX < wallWidth) columnData[bd.gridX].Add(bd);
                }
            }
        }

        private void RefillColumn(int x)
        {
            int currentItems = 0;
            for (int k = 0; k < ACTIVE_COL_HEIGHT; k++)
            {
                if (wallGrid[x, k] != null) currentItems++;
            }

            while (currentItems < ACTIVE_COL_HEIGHT && columnData[x].Count > 0)
            {
                BlockData bd = (BlockData)columnData[x][0];
                columnData[x].RemoveAt(0);

                Block b;
                if (bd.lockId >= 0)
                {
                    b = GetKeyBlockFromPool();
                    ((KeyBlock)b).Init((BlockColor)bd.colorIndex, x, currentItems, bd.lockId, bd.isTwoColor, (BlockColor)bd.secondaryColorIndex);
                }
                else
                {
                    b = GetBlockFromPool();
                    b.Init((BlockColor)bd.colorIndex, x, currentItems, bd.isTwoColor, (BlockColor)bd.secondaryColorIndex);
                }

                b.transform.localPosition = GetWallWorldPosition(x, ACTIVE_COL_HEIGHT);
                wallGrid[x, currentItems] = b;
                b.UpdateGridPosition(x, currentItems, GetWallWorldPosition(x, currentItems));
                currentItems++;
            }
        }

        private Block GetBlockFromPool()
        {
            while (blockPool.Count > 0)
            {
                Block b = blockPool.Dequeue();
                if (b != null)
                {
                    b.gameObject.SetActive(true);
                    return b;
                }
            }
            return Instantiate(blockPrefab, wallContainer);
        }

        private Block GetKeyBlockFromPool()
        {
            while (keyBlockPool.Count > 0)
            {
                KeyBlock k = keyBlockPool.Dequeue();
                if (k != null)
                {
                    k.gameObject.SetActive(true);
                    return k;
                }
            }
            return Instantiate(keyBlockPrefab, wallContainer);
        }

        private void ReturnBlockToPool(Block b)
        {
            if (b == null) return;
            b.gameObject.SetActive(false);
            if (b is KeyBlock kb) keyBlockPool.Enqueue(kb);
            else blockPool.Enqueue(b);
        }

        private Projectile CreateNewProjectile()
        {
            Projectile p = Instantiate(projectilePrefab, projectileContainer);
            projectilePool.Add(p);
            return p;
        }

        private Projectile GetProjectileFromPool()
        {
            foreach (var p in projectilePool)
            {
                if (p != null && !p.gameObject.activeSelf)
                {
                    p.gameObject.SetActive(true);
                    return p;
                }
            }
            return CreateNewProjectile();
        }

        private void ReturnProjectileToPool(Projectile p)
        {
            if (p == null) return;
            p.gameObject.SetActive(false);
            // No need to enqueue/add, it's already in the list
        }

        private void OnProjectileHit(Projectile p)
        {
            if (p != null && p.TargetBlock != null)
            {
                StartCoroutine(HandleBlockHit(p.GridX, p.GridY, p.TargetBlock, p.WillDestroy));
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
            if (isGameOver || buildRoutine != null) return;
            HandleInput();
            HandleShooting();
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
                // Block raycast if clicking on UI
                if (UnityEngine.EventSystems.EventSystem.current != null)
                {
                    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
                    }
                    else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                    {
                        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue())) return;
                    }
                }

                Ray ray = mainCamera.ScreenPointToRay(inputPosition);
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
            AudioManager.Instance.PlaySfx("ArrowEscape_Sfx");
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
                    if (b != null && !b.IsTargeted && b.AnimationProgress >= 0.5f) // Only target non-targeted blocks that have partially settled
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
                            proj.transform.position = slot.transform.position;
                            proj.transform.localScale = Vector3.one * 0.4f;

                            proj.Initialize(color, b, targetX, targetY, willBeDestroyed, OnProjectileHit, ReturnProjectileToPool);

                            float distance = Vector3.Distance(slot.transform.position, targetPos);
                            float duration = distance / projectileSpeed;
                            proj.Launch(targetPos, duration);

                            AudioManager.Instance.PlaySfx("Shooting_Sfx");
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
            AudioManager.Instance.TriggerHaptic();
            if (willBeDestroyed)
            {
                if (b is KeyBlock kb)
                {
                    LockObstacle targetLock = activeLocks.Find(l => l.LockId == kb.LockId && l.IsLocked);
                    if (targetLock != null)
                    {
                        yield return kb.AnimateFlyToTarget(targetLock.transform.position, 0.4f);
                        targetLock.Unlock();
                    }
                }
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
            // Column Collapse: Repack all stationary blocks to the bottom
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
                Block b = columnBlocks[k];
                wallGrid[x, k] = b;
                b.UpdateGridPosition(x, k, GetWallWorldPosition(x, k));
            }

            RefillColumn(x); // Fill the gap at the top
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
                //Debug.Log("🎉 VICTORY! All blocks destroyed!");
                isGameOver = true;
                AudioManager.Instance.PlaySfx("Win_Sfx");
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
                        AudioManager.Instance.PlaySfx("Lose_Sfx");
                        ArrowBlast.UI.GameEndUIManager.Instance.ShowLosePanel();
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
                AudioManager.Instance.PlaySfx("Lose_Sfx");
                ArrowBlast.UI.GameEndUIManager.Instance.ShowLosePanel();
            }
        }

        private IEnumerator VictoryRoutine()
        {
            yield return new WaitForSeconds(2.0f);

            // Award coins for completing the level
            if (coinSystem != null)
            {
                coinSystem.AddCoins(10);
            }

            // Unlock the next level
            if (levelManager != null)
            {
                levelManager.UnlockNextLevel();
            }

            if (ArrowBlast.UI.GameEndUIManager.Instance != null)
            {
                ArrowBlast.UI.GameEndUIManager.Instance.ShowWinPanel(10);
            }
            else
            {
                ReturnToLevelSelect();
            }
        }

        public void ReturnToLevelSelect()
        {
            isGameOver = true;
            StopAllCoroutines();
            wallGrid = null;
            arrowGrid = null;

            foreach (Transform t in wallContainer)
            {
                var b = t.GetComponent<Block>();
                if (b != null && t.gameObject.activeSelf)
                {
                    ReturnBlockToPool(b);
                }
                else if (t.gameObject.activeSelf)
                {
                    Destroy(t.gameObject);
                }
            }

            foreach (Transform t in arrowContainer) Destroy(t.gameObject);

            foreach (var p in projectilePool)
            {
                if (p != null) p.gameObject.SetActive(false);
            }

            foreach (var s in slots) s.ClearSlot();

            if (slotsContainer != null) slotsContainer.gameObject.SetActive(false);
            if (boosterUIManager != null) boosterUIManager.SetVisible(false);

            extraSlotsToLoad = 0;
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
