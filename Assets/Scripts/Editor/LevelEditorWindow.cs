#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ArrowBlast.Core;
using ArrowBlast.Data;

namespace ArrowBlast.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private LevelData currentLevelData;
        
        // Editor State
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private string[] tabs = { "Wall Editor", "Arrow Grid Editor" };
        
        // Tool State
        private bool isKeyMode = false;
        private bool isLockMode = false;
        private BlockColor selectedColor = BlockColor.Red;
        private Direction selectedDirection = Direction.Up;
        private int selectedLength = 1;
        private int concurrentArrows = 2; // Number of arrows handled at once for wall randomization
        private int selectedLockId = 0; // For key/lock pairing
        private int selectedLockSizeX = 2; // Lock width
        private int selectedLockSizeY = 1; // Lock height
        private int minWallCluster = 2; // Minimum same color adjacent
        private int maxWallCluster = 5; // Maximum same color adjacent

        // Interaction State
        private enum RandomMode { Normal, Hard, Mixed }
        private List<Vector2Int> arrowDragPath = new List<Vector2Int>();
        private ArrowData ghostArrow = null;
        private int originalDragArrowIndex = -1; // To restore if cancelled or to delete if replaced
        private BlockColor? capturedColor = null;
        private int keyboardSelectedArrowIndex = -1;
        private Vector2Int? keyboardSelectedCell = null;
        private List<ArrowData> solvableArrowOrder = new List<ArrowData>(); // Stores the sequence of solvable arrows during generation

        [MenuItem("Arrow Blast/Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditorWindow>("Level Editor");
        }

        private void OnGUI()
        {
            GUILayout.Label("Arrow Blast Visual Level Editor", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 1. Data Reference
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Level Data Asset:", GUILayout.Width(120));
            LevelData newData = (LevelData)EditorGUILayout.ObjectField(currentLevelData, typeof(LevelData), false);
            if (newData != currentLevelData)
            {
                currentLevelData = newData;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            if (currentLevelData == null)
            {
                EditorGUILayout.HelpBox("Please assign or create a LevelData asset to start editing.", MessageType.Info);
                if (GUILayout.Button("Create New Level Asset"))
                {
                    CreateNewLevelAsset();
                }
                return;
            }

            // 2. Main Scroll View
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 3. Level Settings & Stats
            DrawSettingsAndStats();

            GUILayout.Space(10);
            
            // 4. Tabs
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            GUILayout.Space(10);

            if (selectedTab == 0)
            {
                DrawWallEditor();
            }
            else if (selectedTab == 1)
            {
                DrawArrowEditor();
            }
            else if (selectedTab == 2)
            {
                DrawKeyEditor();
            }
            else if (selectedTab == 3)
            {
                DrawLockEditor();
            }

            // 5. Global Actions
            GUILayout.Space(20);
            if (GUILayout.Button("Save Changes (Dirty)", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(currentLevelData);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettingsAndStats()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Level Configuration", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wall Size:", GUILayout.Width(80));
            int newW = EditorGUILayout.IntSlider(currentLevelData.width, 1, 100);
            int newH = EditorGUILayout.IntSlider(currentLevelData.height, 1, 500);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Grid Size:", GUILayout.Width(80));
            int newRows = EditorGUILayout.IntSlider(currentLevelData.gridRows, 1, 100);
            int newCols = EditorGUILayout.IntSlider(currentLevelData.gridCols, 1, 500);
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevelData, "Resize Level");
                currentLevelData.width = newW;
                currentLevelData.height = newH;
                currentLevelData.gridRows = newRows;
                currentLevelData.gridCols = newCols;
                EditorUtility.SetDirty(currentLevelData);
            }

            // Stats
            int totalBlocks = 0;
            foreach (var b in currentLevelData.blocks)
            {
                totalBlocks++;
                if (b.isTwoColor) totalBlocks++;
            }
            int totalAmmo = CalculateTotalAmmo();
            
            EditorGUILayout.LabelField($"Total Stats: {totalBlocks} Blocks vs {totalAmmo} Ammo", EditorStyles.boldLabel);
            if (totalAmmo < totalBlocks)
            {
                EditorGUILayout.HelpBox("Warning: Less ammo than blocks! Level might be impossible.", MessageType.Warning);
            }

            // Detailed Stats
            EditorGUILayout.LabelField("Details per Color:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Calculate per color
            Dictionary<int, int> blocksPerColor = new Dictionary<int, int>();
            Dictionary<int, int> ammoPerColor = new Dictionary<int, int>();
            
            foreach(var b in currentLevelData.blocks)
            {
                if (!blocksPerColor.ContainsKey(b.colorIndex)) blocksPerColor[b.colorIndex] = 0;
                blocksPerColor[b.colorIndex]++;
                if (b.isTwoColor)
                {
                    if (!blocksPerColor.ContainsKey(b.secondaryColorIndex)) blocksPerColor[b.secondaryColorIndex] = 0;
                    blocksPerColor[b.secondaryColorIndex]++;
                }
            }
            
            foreach(var a in currentLevelData.arrows)
            {
                int ammo = a.length == 1 ? 10 : a.length == 2 ? 20 : a.length == 3 ? 20 : 40;
                if (!ammoPerColor.ContainsKey(a.colorIndex)) ammoPerColor[a.colorIndex] = 0;
                ammoPerColor[a.colorIndex] += ammo;
            }

            foreach(BlockColor color in System.Enum.GetValues(typeof(BlockColor)))
            {
                int cIdx = (int)color;
                int bCount = blocksPerColor.ContainsKey(cIdx) ? blocksPerColor[cIdx] : 0;
                int aCount = ammoPerColor.ContainsKey(cIdx) ? ammoPerColor[cIdx] : 0;
                
                if (bCount > 0 || aCount > 0)
                {
                    // Draw colored label
                    GUIStyle style = new GUIStyle(EditorStyles.label);
                    style.normal.textColor = GetColorFromEnum(color);
                    if (color == BlockColor.Yellow || color == BlockColor.Green) style.normal.textColor = Color.black; // Visibility fix for light colors if needed, but Editor bg is dark. Yellow is fine.
                    // Actually GetColorFromEnum returns strict colors.
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(color.ToString(), style, GUILayout.Width(60));
                    EditorGUILayout.LabelField($"Blocks: {bCount}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Ammo: {aCount}", GUILayout.Width(80));
                    if (aCount < bCount) GUILayout.Label("(!)", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void DrawWallEditor()
        {
            EditorGUILayout.LabelField("Wall Editor", EditorStyles.boldLabel);
            
            // Tools
            EditorGUILayout.BeginHorizontal();
            selectedColor = (BlockColor)EditorGUILayout.EnumPopup("Paint Color:", selectedColor);
            concurrentArrows = EditorGUILayout.IntSlider("Concurrent Arrows:", concurrentArrows, 1, 5);
            
            if (GUILayout.Button("Randomize Wall", GUILayout.Width(120)))
            {
                RandomizeWall();
            }
            if (GUILayout.Button("Clear Wall", GUILayout.Width(100)))
            {
                Undo.RecordObject(currentLevelData, "Clear Wall");
                currentLevelData.blocks.Clear();
                currentLevelData.keys.Clear();
                EditorUtility.SetDirty(currentLevelData);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            minWallCluster = EditorGUILayout.IntSlider("Min Cluster:", minWallCluster, 1, 20);
            maxWallCluster = EditorGUILayout.IntSlider("Max Cluster:", maxWallCluster, 1, 100);
            if (minWallCluster > maxWallCluster) maxWallCluster = minWallCluster;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            isKeyMode = EditorGUILayout.ToggleLeft("Paint Key Mode", isKeyMode, GUILayout.Width(120));
            if (isKeyMode)
            {
                selectedLockId = EditorGUILayout.IntField("Target Lock ID:", selectedLockId);
            }
            EditorGUILayout.EndHorizontal();

            // Grid Visualization
            float cellSize = 40f;
            float startX = 20f;
            float startY = GUILayoutUtility.GetRect(0, cellSize * currentLevelData.height + 20).y + 10;

            // Draw Background
            Rect bgRect = new Rect(startX, startY, currentLevelData.width * cellSize, currentLevelData.height * cellSize);
            EditorGUI.DrawRect(bgRect, new Color(0.2f, 0.2f, 0.2f));

            // Draw Grid Lines (White, Semi-transparent)
            Handles.color = new Color(1, 1, 1, 0.1f);
            for (int i = 0; i <= currentLevelData.width; i++)
            {
                Handles.DrawLine(new Vector3(startX + i * cellSize, startY, 0), new Vector3(startX + i * cellSize, startY + currentLevelData.height * cellSize, 0));
            }
            for (int i = 0; i <= currentLevelData.height; i++)
            {
                Handles.DrawLine(new Vector3(startX, startY + i * cellSize, 0), new Vector3(startX + currentLevelData.width * cellSize, startY + i * cellSize, 0));
            }

            // Draw Blocks
            for (int y = currentLevelData.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < currentLevelData.width; x++)
                {
                    Rect cellRect = new Rect(startX + x * cellSize + 2, startY + (currentLevelData.height - 1 - y) * cellSize + 2, cellSize - 4, cellSize - 4);
                    
                    BlockData block = currentLevelData.blocks.Find(b => b.gridX == x && b.gridY == y);
                    KeyData key = currentLevelData.keys.Find(k => k.gridX == x && k.gridY == y);
                    
                    Color drawColor = Color.gray;
                    if (key != null)
                    {
                        drawColor = new Color(1f, 0.84f, 0f); // Gold for Key
                    }
                    else if (block != null)
                    {
                        drawColor = GetColorFromEnum((BlockColor)block.colorIndex);
                    }
                    
                    // Interaction: Click or Shift+Drag
                    bool isAction = false;
                    if (cellRect.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.type == EventType.MouseDown) isAction = true;
                        if (Event.current.type == EventType.MouseDrag && Event.current.shift) isAction = true;
                    }

                    if (isAction)
                    {
                        Undo.RecordObject(currentLevelData, "Modify Wall");
                        
                        if (Event.current.button == 0) // LMB
                        {
                            if (isKeyMode)
                            {
                                // Remove block if exists
                                var existingBlock = currentLevelData.blocks.Find(b => b.gridX == x && b.gridY == y);
                                if (existingBlock != null) currentLevelData.blocks.Remove(existingBlock);
                                
                                // Add/Update Key
                                if (key == null) currentLevelData.keys.Add(new KeyData { gridX = x, gridY = y, lockId = selectedLockId });
                                else key.lockId = selectedLockId;
                            }
                            else
                            {
                                // Remove key if exists
                                var existingKey = currentLevelData.keys.Find(k => k.gridX == x && k.gridY == y);
                                if (existingKey != null) currentLevelData.keys.Remove(existingKey);

                                if (block == null)
                                {
                                    currentLevelData.blocks.Add(new BlockData { gridX = x, gridY = y, colorIndex = (int)selectedColor, isTwoColor = false });
                                }
                                else if (block.colorIndex != (int)selectedColor)
                                {
                                    block.isTwoColor = true;
                                    block.secondaryColorIndex = block.colorIndex;
                                    block.colorIndex = (int)selectedColor;
                                }
                            }
                            GUI.changed = true;
                        }
                        else if (Event.current.button == 1) // RMB
                        {
                            if (block != null) currentLevelData.blocks.Remove(block);
                            if (key != null) currentLevelData.keys.Remove(key);
                            GUI.changed = true;
                        }
                        
                        EditorUtility.SetDirty(currentLevelData);
                        Event.current.Use();
                    }

                    EditorGUI.DrawRect(cellRect, drawColor);
                    if (key != null)
                    {
                        GUI.Label(cellRect, $"K:{key.lockId}", new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter });
                    }
                    else if (block != null && block.isTwoColor)
                    {
                        float innerSize = cellSize * 0.5f;
                        Rect innerRect = new Rect(cellRect.x + (cellRect.width - innerSize) / 2f, cellRect.y + (cellRect.height - innerSize) / 2f, innerSize, innerSize);
                        EditorGUI.DrawRect(innerRect, GetColorFromEnum((BlockColor)block.secondaryColorIndex));
                    }
                }
            }
            
            if (GUI.changed) Repaint();
            
            EditorGUILayout.HelpBox("LMB to Paint, RMB to Erase. Hold Shift to Drag-Paint.", MessageType.None);
        }

        private void DrawArrowEditor()
        {
            EditorGUILayout.LabelField("Arrow Grid Editor", EditorStyles.boldLabel);
            
            // Tools
            EditorGUILayout.BeginHorizontal();
            selectedColor = (BlockColor)EditorGUILayout.EnumPopup("Color:", selectedColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            isLockMode = EditorGUILayout.ToggleLeft("Lock Placement Mode", isLockMode, GUILayout.Width(150));
            if (isLockMode)
            {
                EditorGUILayout.BeginHorizontal();
                selectedLockId = EditorGUILayout.IntField("Lock ID:", selectedLockId);
                selectedLockSizeX = EditorGUILayout.IntSlider("Size X:", selectedLockSizeX, 1, currentLevelData.gridCols);
                selectedLockSizeY = EditorGUILayout.IntSlider("Size Y:", selectedLockSizeY, 1, currentLevelData.gridRows);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Randomize (Normal)", GUILayout.Width(130)))
            {
                RandomizeArrows(RandomMode.Normal);
            }
            if (GUILayout.Button("Randomize (Hard)", GUILayout.Width(130)))
            {
                RandomizeArrows(RandomMode.Hard);
            }
            if (GUILayout.Button("Randomize (Mixed)", GUILayout.Width(130)))
            {
                RandomizeArrows(RandomMode.Mixed);
            }
            if (GUILayout.Button("Clear Arrows", GUILayout.Width(100)))
            {
                Undo.RecordObject(currentLevelData, "Clear Arrows");
                currentLevelData.arrows.Clear();
                EditorUtility.SetDirty(currentLevelData);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            HandleArrowKeyInput();

            // Grid Visualization
            float cellSize = 40f;
            float startX = 20f;
            float startY = GUILayoutUtility.GetRect(0, cellSize * currentLevelData.gridRows + 20).y + 10;

            // Process Input globally
            if (Event.current.type == EventType.MouseUp)
            {
                // Only finalize if a real drag created a multi-cell path
                if (arrowDragPath.Count > 1)
                {
                    FinalizeArrowPath();
                }
                // Clear transient drag state on simple click
                arrowDragPath.Clear();
                originalDragArrowIndex = -1;
                capturedColor = null;
                Repaint();
            }

            // Draw Background
            Rect bgRect = new Rect(startX, startY, currentLevelData.gridCols * cellSize, currentLevelData.gridRows * cellSize);
            EditorGUI.DrawRect(bgRect, new Color(0.2f, 0.2f, 0.2f));

            // Draw Connection Lines for existing arrows
            Handles.color = new Color(1, 1, 1, 0.3f);
            foreach(var arrow in currentLevelData.arrows)
            {
                List<Vector2Int> cells = GetArrowCells(arrow);
                for(int i=0; i<cells.Count - 1; i++)
                {
                    DrawSegmentLine(cells[i], cells[i+1], startX, startY, cellSize);
                }
            }

            // Draw Locks (Yellow Borders)
            foreach (var lockData in currentLevelData.locks)
            {
                Rect lRect = new Rect(
                    startX + lockData.gridX * cellSize,
                    startY + (currentLevelData.gridRows - lockData.gridY - lockData.sizeY) * cellSize,
                    lockData.sizeX * cellSize,
                    lockData.sizeY * cellSize
                );
                
                Handles.color = Color.yellow;
                Handles.DrawSolidRectangleWithOutline(lRect, new Color(1, 1, 0, 0.1f), Color.yellow);
                // Thicker outline effect
                Handles.DrawLine(new Vector3(lRect.x-1, lRect.y-1), new Vector3(lRect.xMax+1, lRect.y-1));
                Handles.DrawLine(new Vector3(lRect.x-1, lRect.yMax+1), new Vector3(lRect.xMax+1, lRect.yMax+1));
                Handles.DrawLine(new Vector3(lRect.x-1, lRect.y-1), new Vector3(lRect.x-1, lRect.yMax+1));
                Handles.DrawLine(new Vector3(lRect.xMax+1, lRect.y-1), new Vector3(lRect.xMax+1, lRect.yMax+1));
                
                GUI.Label(lRect, $"LOCK:{lockData.lockId}", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperCenter, normal = { textColor = Color.yellow } });
            }

            // Draw Cells
            for (int y = currentLevelData.gridRows - 1; y >= 0; y--)
            {
                for (int x = 0; x < currentLevelData.gridCols; x++)
                {
                    Rect cellRect = new Rect(startX + x * cellSize + 2, startY + (currentLevelData.gridRows - 1 - y) * cellSize + 2, cellSize - 4, cellSize - 4);
                    
                    ArrowData arrow = currentLevelData.arrows.Find(a => a.gridX == x && a.gridY == y); // Find head
                    if (arrow == null) arrow = currentLevelData.arrows.Find(a => IsBodyPart(a, x, y)); // Body

                    Color drawColor = new Color(0.3f, 0.3f, 0.3f);
                    string label = "";
                    
                    if (arrow != null)
                    {
                        drawColor = GetColorFromEnum((BlockColor)arrow.colorIndex);
                        if (arrow.gridX == x && arrow.gridY == y)
                        {
                            label = GetDirectionArrow((Direction)arrow.direction);
                            drawColor *= 1.2f;
                        }
                        else
                        {
                             drawColor *= 0.8f;
                        }
                    }

                    // Path Drawing Highlight
                    if (arrowDragPath.Contains(new Vector2Int(x, y)))
                    {
                        Color pathColor = GetColorFromEnum(capturedColor ?? selectedColor);
                        pathColor.a = 0.6f;
                        drawColor = Color.Lerp(drawColor, pathColor, 0.7f);
                        if (arrowDragPath[0] == new Vector2Int(x, y)) // Last point is head in our list logic
                        {
                             Direction d;
                             if(arrowDragPath.Count > 1) d = GetDirectionBetween(arrowDragPath[1], arrowDragPath[0]);
                             else d = selectedDirection; // Fallback
                             label = GetDirectionArrow(d);
                        }
                    }

                    // Interaction
                    if (bgRect.Contains(Event.current.mousePosition) && cellRect.Contains(Event.current.mousePosition))
                    {
                        Vector2Int cell = new Vector2Int(x, y);

                        // Shift-click paint/erase
                        if (Event.current.shift && Event.current.type == EventType.MouseDown)
                        {
                            if (Event.current.button == 0)
                            {
                                Undo.RecordObject(currentLevelData, "Paint Arrow Cell");
                                var target = FindArrowAtCell(x, y);
                                if (target != null)
                                {
                                    target.colorIndex = (int)selectedColor;
                                    SelectArrowForKeyboard(target, cell);
                                }
                                else
                                {
                                    var newArrow = new ArrowData
                                    {
                                        gridX = x,
                                        gridY = y,
                                        colorIndex = (int)selectedColor,
                                        direction = (int)selectedDirection,
                                        length = 1,
                                        segments = new List<Vector2Int> { cell }
                                    };
                                    currentLevelData.arrows.Add(newArrow);
                                    SelectArrowForKeyboard(newArrow, cell);
                                }

                                EditorUtility.SetDirty(currentLevelData);
                                arrowDragPath.Clear();
                                originalDragArrowIndex = -1;
                                capturedColor = null;
                                Repaint();
                                Event.current.Use();
                            }
                            else if (Event.current.button == 1)
                            {
                                var target = FindArrowAtCell(x, y);
                                if (target != null)
                                {
                                    Undo.RecordObject(currentLevelData, "Erase Arrow Cell");
                                    ClearKeyboardSelectionIf(target);
                                    currentLevelData.arrows.Remove(target);
                                    EditorUtility.SetDirty(currentLevelData);
                                    Repaint();
                                }
                                //Event.current.Use();
                            }
                        }
                        else
                        {
                            if (Event.current.type == EventType.MouseDown)
                            {
                                if (isLockMode)
                                {
                                    Undo.RecordObject(currentLevelData, "Modify Lock");
                                    if (Event.current.button == 0) // LMB
                                    {
                                        currentLevelData.locks.RemoveAll(l => l.gridX == x && l.gridY == y);
                                        currentLevelData.locks.Add(new LockData { gridX = x, gridY = y, sizeX = selectedLockSizeX, sizeY = selectedLockSizeY, lockId = selectedLockId });
                                    }
                                    else if (Event.current.button == 1) // RMB
                                    {
                                        currentLevelData.locks.RemoveAll(l => x >= l.gridX && x < l.gridX + l.sizeX && y >= l.gridY && y < l.gridY + l.sizeY);
                                    }
                                    EditorUtility.SetDirty(currentLevelData);
                                    Repaint();
                                    Event.current.Use();
                                }
                                else if (Event.current.button == 0) // LMB (Select/Start Arrow Drag)
                                {
                                    if (arrow != null)
                                    {
                                        SelectArrowForKeyboard(arrow, cell);
                                        originalDragArrowIndex = currentLevelData.arrows.IndexOf(arrow);
                                        capturedColor = (BlockColor)arrow.colorIndex;
                                        List<Vector2Int> existing = GetArrowCells(arrow);
                                        int idx = existing.IndexOf(cell);
                                        arrowDragPath = new List<Vector2Int> { cell };
                                    }
                                    else
                                    {
                                        keyboardSelectedArrowIndex = -1;
                                        keyboardSelectedCell = null;
                                        originalDragArrowIndex = -1;
                                        capturedColor = null;
                                        arrowDragPath = new List<Vector2Int> { cell };
                                    }
                                    Event.current.Use();
                                }
                            }
                            
                            if (Event.current.type == EventType.MouseDrag && arrowDragPath.Count > 0 && !isLockMode)
                            {
                                Vector2Int currentCell = cell;
                                if (currentCell != arrowDragPath[0]) // arrowDragPath[0] is most recent cell
                                {
                                    if (IsAdjacent(currentCell, arrowDragPath[0]))
                                    {
                                        if (!arrowDragPath.Contains(currentCell))
                                        {
                                            arrowDragPath.Insert(0, currentCell); // Prepend new head
                                            Repaint();
                                        }
                                        else
                                        {
                                            int idx = arrowDragPath.IndexOf(currentCell);
                                            if (idx > 0)
                                            {
                                                arrowDragPath.RemoveRange(0, idx);
                                                Repaint();
                                            }
                                        }
                                    }
                                }
                            }

                            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                            {
                                // Ignore right-click in arrow grid editor — do nothing.
                                Event.current.Use();
                            }
                        }
                    }

                    EditorGUI.DrawRect(cellRect, drawColor);
                    if (!string.IsNullOrEmpty(label))
                    {
                        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                        style.alignment = TextAnchor.MiddleCenter;
                        style.normal.textColor = Color.black;
                        GUI.Label(cellRect, label, style);
                        style.normal.textColor = Color.white;
                        GUI.Label(new Rect(cellRect.x-1, cellRect.y-1, cellRect.width, cellRect.height), label, style);
                    }
                }
            }
            
            // Draw current path lines
            Handles.color = Color.white;
            for(int i=0; i<arrowDragPath.Count - 1; i++)
            {
                DrawSegmentLine(arrowDragPath[i], arrowDragPath[i+1], startX, startY, cellSize);
            }

            EditorGUILayout.HelpBox("LMB Drag to Draw Path (Last point is Head). Shift + RMB to Remove. Shift + LMB to draw a block. Click to select and use arrow button on keyboard to assign direction.", MessageType.None);
        }

        private void HandleArrowKeyInput()
        {
            if (Event.current.type != EventType.KeyDown) return;
            Direction? newDir = null;
            switch (Event.current.keyCode)
            {
                case KeyCode.UpArrow: newDir = Direction.Up; break;
                case KeyCode.DownArrow: newDir = Direction.Down; break;
                case KeyCode.LeftArrow: newDir = Direction.Left; break;
                case KeyCode.RightArrow: newDir = Direction.Right; break;
            }

            if (!newDir.HasValue) return;
            if (keyboardSelectedArrowIndex < 0 || keyboardSelectedArrowIndex >= currentLevelData.arrows.Count) return;
            var arrow = currentLevelData.arrows[keyboardSelectedArrowIndex];
            if (arrow == null) return;

            Undo.RecordObject(currentLevelData, "Change Arrow Direction");
            arrow.direction = (int)newDir.Value;
            EditorUtility.SetDirty(currentLevelData);
            Event.current.Use();
            Repaint();
        }

        private void DrawSegmentLine(Vector2Int a, Vector2Int b, float startX, float startY, float cellSize)
        {
            Vector3 p1 = new Vector3(startX + a.x * cellSize + cellSize/2, startY + (currentLevelData.gridRows - 1 - a.y) * cellSize + cellSize/2, 0);
            Vector3 p2 = new Vector3(startX + b.x * cellSize + cellSize/2, startY + (currentLevelData.gridRows - 1 - b.y) * cellSize + cellSize/2, 0);
            Handles.DrawLine(p1, p2);
        }

        private bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        private Direction GetDirectionBetween(Vector2Int head, Vector2Int prev)
        {
            Vector2Int diff = head - prev;
            if (diff.y > 0) return Direction.Up;
            if (diff.y < 0) return Direction.Down;
            if (diff.x < 0) return Direction.Left;
            return Direction.Right;
        }

        private ArrowData FindArrowAtCell(int x, int y)
        {
            return currentLevelData.arrows.Find(a => (a.gridX == x && a.gridY == y) || IsBodyPart(a, x, y));
        }

        private void SelectArrowForKeyboard(ArrowData arrow, Vector2Int cell)
        {
            if (arrow == null) return;
            keyboardSelectedArrowIndex = currentLevelData.arrows.IndexOf(arrow);
            keyboardSelectedCell = cell;
        }

        private void ClearKeyboardSelectionIf(ArrowData arrow)
        {
            if (keyboardSelectedArrowIndex < 0) return;
            if (keyboardSelectedArrowIndex >= currentLevelData.arrows.Count) return;
            if (currentLevelData.arrows[keyboardSelectedArrowIndex] != arrow) return;
            keyboardSelectedArrowIndex = -1;
            keyboardSelectedCell = null;
        }

        private void FinalizeArrowPath()
        {
            if (arrowDragPath.Count > 0)
            {
                // In our list, index 0 is the HEAD (latest cell), and higher indices are body.
                // This matches Storage requirements for segments[0] = Head.
                
                Vector2Int head = arrowDragPath[0];
                Direction dir;
                if(arrowDragPath.Count > 1) dir = GetDirectionBetween(head, arrowDragPath[1]);
                else dir = selectedDirection;

                Undo.RecordObject(currentLevelData, "Update Arrow");

                // Remove overlapping or the one we were editing
                if (originalDragArrowIndex >= 0 && originalDragArrowIndex < currentLevelData.arrows.Count)
                {
                    // If we started from an arrow, remove it
                    currentLevelData.arrows.RemoveAt(originalDragArrowIndex);
                }
                
                // Remove anything else in the way of the new segments
                // We use a while loop because Remove might affect the collection
                bool foundAny = true;
                while (foundAny)
                {
                    foundAny = false;
                    foreach (var cell in arrowDragPath)
                    {
                        var blocking = currentLevelData.arrows.Find(a => a.gridX == cell.x && a.gridY == cell.y || IsBodyPart(a, cell.x, cell.y));
                        if (blocking != null)
                        {
                            currentLevelData.arrows.Remove(blocking);
                            foundAny = true;
                            break;
                        }
                    }
                }

                currentLevelData.arrows.Add(new ArrowData
                {
                    gridX = head.x,
                    gridY = head.y,
                    colorIndex = (int)(capturedColor ?? selectedColor),
                    direction = (int)dir,
                    length = arrowDragPath.Count,
                    segments = new List<Vector2Int>(arrowDragPath)
                });

                EditorUtility.SetDirty(currentLevelData);
            }
            arrowDragPath.Clear();
            originalDragArrowIndex = -1;
            capturedColor = null;
        }

        // --- Helpers ---

        private void CreateNewLevelAsset()
        {
            LevelData asset = CreateInstance<LevelData>();
            string path = EditorUtility.SaveFilePanelInProject("Save New Level", "NewLevel", "asset", "Please enter a file name to save the level to");
            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            currentLevelData = asset;
            Repaint();
        }

        private Color GetColorFromEnum(BlockColor color)
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

        private string GetDirectionArrow(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return "\u2191";
                case Direction.Down: return "\u2193";
                case Direction.Left: return "\u2190";
                case Direction.Right: return "\u2192";
                default: return "?";
            }
        }
        
        private int CalculateTotalAmmo()
        {
            int ammo = 0;
            foreach(var a in currentLevelData.arrows)
            {
               ammo += a.length == 1 ? 10 : a.length == 2 ? 20 : a.length == 3 ? 20 : 40;
            }
            return ammo;
        }

        // --- Logic: Randomization & Validation ---

        private void RandomizeWall()
        {
            if (currentLevelData.arrows.Count == 0)
            {
            EditorUtility.DisplayDialog("Error", "Please place some arrows first so I know what color blocks to generate!", "OK");
            return;
            }

            Undo.RecordObject(currentLevelData, "Randomize Wall");
            currentLevelData.blocks.Clear();

            // 1. Determine Sequence -> compute total ammo per color
            Dictionary<int, int> colorCounts = new Dictionary<int, int>();
            foreach (var a in currentLevelData.arrows)
            {
            int ammo = GetArrowAmmo(a);
            if (!colorCounts.ContainsKey(a.colorIndex)) colorCounts[a.colorIndex] = 0;
            colorCounts[a.colorIndex] += ammo;
            }

            int cols = currentLevelData.width;
            int rows = currentLevelData.height;
            int[,] wallGrid = new int[cols, rows];
            for (int x = 0; x < cols; x++) for (int y = 0; y < rows; y++) wallGrid[x, y] = -1;

            // Constraints: each contiguous cluster of same color should be between minCluster and maxCluster
            int minCluster = minWallCluster;
            int maxCluster = maxWallCluster;

            // Utility: available empty cells
            List<Vector2Int> GetEmptyCells()
            {
            var list = new List<Vector2Int>();
            for (int xx = 0; xx < cols; xx++)
                for (int yy = 0; yy < rows; yy++)
                if (wallGrid[xx, yy] == -1) list.Add(new Vector2Int(xx, yy));
            return list;
            }

            // Utility: get empty neighbors of a cell (4-neighborhood)
            List<Vector2Int> GetEmptyNeighbors(Vector2Int cell, HashSet<Vector2Int> exclude)
            {
            var n = new List<Vector2Int>();
            var candidates = new Vector2Int[] {
                new Vector2Int(cell.x + 1, cell.y),
                new Vector2Int(cell.x - 1, cell.y),
                new Vector2Int(cell.x, cell.y + 1),
                new Vector2Int(cell.x, cell.y - 1)
            };
            foreach (var c in candidates)
            {
                if (c.x >= 0 && c.x < cols && c.y >= 0 && c.y < rows && wallGrid[c.x, c.y] == -1 && !exclude.Contains(c))
                n.Add(c);
            }
            return n;
            }

            // For each color, create clusters sized between minCluster..maxCluster until count exhausted
            foreach (var kv in colorCounts)
            {
            int color = kv.Key;
            int remaining = kv.Value;

            // Keep creating clusters for this color
            int safeAttempts = 0;
            while (remaining > 0)
            {
                if (safeAttempts++ > cols * rows * 2) break; // safety guard to avoid infinite loops

                int desired = Random.Range(minCluster, maxCluster + 1);
                desired = Mathf.Min(desired, remaining);

                bool placed = false;
                // Try decreasing sizes if placement of desired fails
                for (int trySize = desired; trySize >= 1 && !placed; trySize--)
                {
                var emptyCells = GetEmptyCells();
                if (emptyCells.Count < trySize) break;

                // Try several random starting points
                int startAttempts = Mathf.Min(30, emptyCells.Count);
                for (int s = 0; s < startAttempts && !placed; s++)
                {
                    Vector2Int start = emptyCells[Random.Range(0, emptyCells.Count)];
                    var cluster = new List<Vector2Int> { start };
                    var visited = new HashSet<Vector2Int> { start };
                    var frontier = new List<Vector2Int>(GetEmptyNeighbors(start, visited));

                    while (cluster.Count < trySize && frontier.Count > 0)
                    {
                    int idx = Random.Range(0, frontier.Count);
                    var pick = frontier[idx];
                    frontier.RemoveAt(idx);
                    if (visited.Contains(pick)) continue;
                    cluster.Add(pick);
                    visited.Add(pick);

                    var neigh = GetEmptyNeighbors(pick, visited);
                    foreach (var n in neigh) if (!frontier.Contains(n)) frontier.Add(n);
                    }

                    if (cluster.Count == trySize)
                    {
                    // Place cluster
                    foreach (var c in cluster) wallGrid[c.x, c.y] = color;
                    remaining -= cluster.Count;
                    placed = true;
                    }
                }
                }

                if (!placed)
                {
                // Couldn't place any more clusters for this color - break to avoid infinite loop
                break;
                }
            }
            }

            // If some empty cells remain, fill them randomly with any color that still has capacity (or random color)
            var remainingEmpty = GetEmptyCells();
            var colorKeys = new List<int>(colorCounts.Keys);
            foreach (var e in remainingEmpty)
            {
            int pickColor = (colorKeys.Count > 0) ? colorKeys[Random.Range(0, colorKeys.Count)] : Random.Range(0, 6);
            wallGrid[e.x, e.y] = pickColor;
            }

            // 3. Finalize into BlockData
            for (int x = 0; x < cols; x++)
            {
            for (int y = 0; y < rows; y++)
            {
                if (wallGrid[x, y] != -1)
                {
                currentLevelData.blocks.Add(new BlockData
                {
                    gridX = x,
                    gridY = y,
                    colorIndex = wallGrid[x, y]
                });
                }
            }
            }

            EditorUtility.SetDirty(currentLevelData);
        }

        private int GetArrowAmmo(ArrowData arrow)
        {
            int len = arrow.length;
            if (arrow.segments != null && arrow.segments.Count > 0) len = arrow.segments.Count;

            switch (len)
            {
                case 1: return 10;
                case 2: return 20;
                case 3: return 20;
                case 4: return 40;
                default: return 10;
            }
        }





        private struct SimulatedArrow
        {
            public ArrowData arrow;
            public int originalIndex;
        }

        private List<SimulatedArrow> SimulateArrowCollection()
        {
            List<SimulatedArrow> result = new List<SimulatedArrow>();
            List<ArrowData> remaining = new List<ArrowData>(currentLevelData.arrows);
            
            // Reconstruct grid logic locally
            int cols = currentLevelData.gridCols;
            int rows = currentLevelData.gridRows;
            ArrowData[,] gridRef = new ArrowData[cols, rows];
            
            // Fill grid
            foreach(var a in remaining)
            {
                var cells = GetArrowCells(a);
                foreach(var c in cells) if(c.x >=0 && c.x < cols && c.y >=0 && c.y < rows) gridRef[c.x, c.y] = a;
            }
            
            bool changed = true;
            while(changed && remaining.Count > 0)
            {
                changed = false;
                // Find escaped
                for(int i = remaining.Count - 1; i >= 0; i--)
                {
                    var a = remaining[i];
                    if (CanSimulatedArrowEscape(a, gridRef))
                    {
                        // Collect
                        result.Add(new SimulatedArrow { arrow = a, originalIndex = i });
                        
                        // Remove from grid
                        var cells = GetArrowCells(a);
                        foreach(var c in cells) if(c.x >=0 && c.x < cols && c.y >=0 && c.y < rows) gridRef[c.x, c.y] = null;
                        
                        remaining.RemoveAt(i);
                        changed = true;
                    }
                }
            }
            
            return result;
        }

        private bool CanSimulatedArrowEscape(ArrowData arrow, ArrowData[,] gridRef)
        {
            var segments = GetArrowCells(arrow);
            int dx = 0, dy = 0;
            int cols = currentLevelData.gridCols;
            int rows = currentLevelData.gridRows;

            // Up = +Y in our World Space / Grid Space match
            switch ((Direction)arrow.direction)
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

                while (nx >= 0 && nx < cols && ny >= 0 && ny < rows)
                {
                    // If cell is occupied, check if it's by the SAME arrow
                    if (gridRef[nx, ny] != null && gridRef[nx, ny] != arrow)
                    {
                        return false;
                    }
                    nx += dx;
                    ny += dy;
                }
            }
            return true;
        }

        private void RandomizeArrows()
        {
            RandomizeArrows(RandomMode.Normal);
        }

        private void RandomizeArrows(RandomMode mode)
        {
            Undo.RecordObject(currentLevelData, "Randomize Arrows");
            currentLevelData.arrows.Clear();
            solvableArrowOrder.Clear();

            int cols = currentLevelData.gridCols;
            int rows = currentLevelData.gridRows;
            ArrowData[,] gridRef = new ArrowData[cols, rows];
            
            // Pass 1: Long Arrows (4 down to 2)
            for (int len = 4; len >= 2; len--)
            {
                TryFillingHoles(len, mode, gridRef);
            }

            // Pass 2: Clean up all remaining single cells
            TryFillingHoles(1, mode, gridRef);

            EditorUtility.SetDirty(currentLevelData);
            Repaint();
        }

        private void TryFillingHoles(int targetLen, RandomMode mode, ArrowData[,] gridRef)
        {
            int cols = currentLevelData.gridCols;
            int rows = currentLevelData.gridRows;

            List<Vector2Int> emptySpots = new List<Vector2Int>();
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (gridRef[x, y] == null) emptySpots.Add(new Vector2Int(x, y));
                }
            }

            // For Hard mode, we bias the spots towards those that block existing arrows
            if (mode == RandomMode.Hard)
            {
                List<Vector2Int> blockedCells = GetAllCurrentExitPaths(gridRef);
                // Move spotted cells to front of list
                for (int i = 0; i < emptySpots.Count; i++)
                {
                    if (blockedCells.Contains(emptySpots[i]))
                    {
                        var temp = emptySpots[0];
                        emptySpots[0] = emptySpots[i];
                        emptySpots[i] = temp;
                    }
                }
            }
            else
            {
                // Normal shuffle
                for (int i = 0; i < emptySpots.Count; i++)
                {
                    int r = Random.Range(i, emptySpots.Count);
                    var temp = emptySpots[i]; emptySpots[i] = emptySpots[r]; emptySpots[r] = temp;
                }
            }

            foreach (var spot in emptySpots)
            {
                if (gridRef[spot.x, spot.y] != null) continue;

                int[] dirs = { 0, 1, 2, 3 };
                for (int i = 0; i < 4; i++) {
                    int r = Random.Range(i, 4);
                    int t = dirs[i]; dirs[i] = dirs[r]; dirs[r] = t;
                }

                foreach (int d in dirs)
                {
                    Direction dir = (Direction)d;
                    List<Vector2Int> segments;
                    if (mode == RandomMode.Mixed && targetLen > 1)
                        segments = GenerateSolvableDiversePath(spot.x, spot.y, targetLen, gridRef, dir);
                    else
                        segments = GetArrowCells(spot.x, spot.y, dir, targetLen);

                    if (segments != null && segments.Count == targetLen && CanPlaceSegmentsOnGridRef(segments, gridRef))
                    {
                        if (CanEntireArrowEscape(segments, dir, gridRef))
                        {
                            var newArrow = new ArrowData
                            {
                                gridX = spot.x,
                                gridY = spot.y,
                                direction = (int)dir,
                                length = segments.Count,
                                colorIndex = Random.Range(0, 6),
                                segments = new List<Vector2Int>(segments)
                            };

                            currentLevelData.arrows.Add(newArrow);
                            foreach (var c in segments) gridRef[c.x, c.y] = newArrow;
                            solvableArrowOrder.Insert(0, newArrow);
                            break;
                        }
                    }
                }
            }
        }

        private List<Vector2Int> GetAllCurrentExitPaths(ArrowData[,] grid)
        {
            List<Vector2Int> paths = new List<Vector2Int>();
            int cols = currentLevelData.gridCols;
            int rows = currentLevelData.gridRows;

            foreach (var arrow in currentLevelData.arrows)
            {
                var segments = GetArrowCells(arrow);
                int dx = 0, dy = 0;
                switch ((Direction)arrow.direction) {
                    case Direction.Up: dy = 1; break;
                    case Direction.Down: dy = -1; break;
                    case Direction.Left: dx = -1; break;
                    case Direction.Right: dx = 1; break;
                }

                foreach (var seg in segments)
                {
                    int nx = seg.x + dx;
                    int ny = seg.y + dy;
                    while (nx >= 0 && nx < cols && ny >= 0 && ny < rows)
                    {
                        if (grid[nx, ny] == null) paths.Add(new Vector2Int(nx, ny));
                        nx += dx;
                        ny += dy;
                    }
                }
            }
            return paths;
        }

        private bool CanEntireArrowEscape(List<Vector2Int> segments, Direction dir, ArrowData[,] grid)
        {
            int dx = 0, dy = 0;
            switch (dir)
            {
                case Direction.Up: dy = 1; break;
                case Direction.Down: dy = -1; break;
                case Direction.Left: dx = -1; break;
                case Direction.Right: dx = 1; break;
            }

            int cols = currentLevelData.gridCols;
            int rows = currentLevelData.gridRows;

            foreach (var seg in segments)
            {
                int nx = seg.x + dx;
                int ny = seg.y + dy;

                while (nx >= 0 && nx < cols && ny >= 0 && ny < rows)
                {
                    // CRITICAL: An arrow CANNOT pass through its own body segments if they are in front of a segment moving in 'dir'
                    // This was the "Whack Mixed" bug. We check if the target cell is any of OUR segments.
                    if (segments.Contains(new Vector2Int(nx, ny)))
                    {
                        return false; // Pointing at its own body!
                    }

                    if (grid[nx, ny] != null)
                    {
                        return false;
                    }
                    nx += dx;
                    ny += dy;
                }
            }
            return true;
        }

        private List<Vector2Int> GenerateSolvableDiversePath(int x, int y, int len, ArrowData[,] grid, Direction exitDir)
        {
            List<Vector2Int> path = new List<Vector2Int> { new Vector2Int(x, y) };
            Vector2Int current = new Vector2Int(x, y);

            for(int i=1; i<len; i++)
            {
                List<Vector2Int> neighbors = new List<Vector2Int> {
                    new Vector2Int(current.x+1, current.y), new Vector2Int(current.x-1, current.y),
                    new Vector2Int(current.x, current.y+1), new Vector2Int(current.x, current.y-1)
                };
                
                // Shuffle
                for(int j=0; j<neighbors.Count; j++) {
                    int r = Random.Range(j, neighbors.Count);
                    var t = neighbors[j]; neighbors[j] = neighbors[r]; neighbors[r] = t;
                }
                
                bool found = false;
                foreach(var n in neighbors)
                {
                    if(n.x >= 0 && n.x < currentLevelData.gridCols && n.y >= 0 && n.y < currentLevelData.gridRows && 
                       grid[n.x, n.y] == null && !path.Contains(n))
                    {
                        path.Add(n);
                        current = n;
                        found = true;
                        break;
                    }
                }
                if(!found) break;
            }
            return path;
        }

        private bool CanPlaceSegmentsOnGridRef(List<Vector2Int> path, ArrowData[,] grid)
        {
            foreach(var c in path)
                if(c.x < 0 || c.x >= currentLevelData.gridCols || c.y < 0 || c.y >= currentLevelData.gridRows || grid[c.x, c.y] != null) return false;
            return true;
        }

        private List<Vector2Int> GenerateDiversePath(int x, int y, int len, bool[,] grid)
        {
            List<Vector2Int> path = new List<Vector2Int> { new Vector2Int(x, y) };
            Vector2Int current = new Vector2Int(x, y);
            
            // Bias towards a general direction to avoid tight knots
            Vector2Int flowDir = Vector2Int.zero;
            if (Random.value < 0.25f) flowDir = Vector2Int.up;
            else if (Random.value < 0.5f) flowDir = Vector2Int.down;
            else if (Random.value < 0.75f) flowDir = Vector2Int.left;
            else flowDir = Vector2Int.right;

            for(int i=1; i<len; i++)
            {
                List<Vector2Int> neighbors = new List<Vector2Int> {
                    new Vector2Int(current.x+1, current.y), new Vector2Int(current.x-1, current.y),
                    new Vector2Int(current.x, current.y+1), new Vector2Int(current.x, current.y-1)
                };
                
                // Shuffle neighbors but prioritize the flow direction
                neighbors.Sort((a, b) => 
                {
                    float weightA = (a - current == flowDir) ? -1 : Random.value;
                    float weightB = (b - current == flowDir) ? -1 : Random.value;
                    return weightA.CompareTo(weightB);
                });
                
                bool found = false;
                foreach(var n in neighbors)
                {
                    if(n.x >= 0 && n.x < currentLevelData.gridCols && n.y >= 0 && n.y < currentLevelData.gridRows && !grid[n.x, n.y] && !path.Contains(n))
                    {
                        path.Add(n);
                        current = n;
                        found = true;
                        break;
                    }
                }
                if(!found) break;
            }
            return path;
        }

        private bool CanPlaceSegments(List<Vector2Int> path, bool[,] grid)
        {
            foreach(var c in path)
                if(c.x < 0 || c.x >= currentLevelData.gridCols || c.y < 0 || c.y >= currentLevelData.gridRows || grid[c.x, c.y]) return false;
            return true;
        }



        private Direction PickDiverseDirection(int x, int y, bool[,] grid)
        {
             // Simple random for now, could be enhanced to check neighbors
             return (Direction)Random.Range(0, 4);
        }

        private bool CanPlaceArrow(int x, int y, Direction dir, int len)
        {
             // Check against existing arrows only (visual editor check)
             // We construct a temporary grid of occupation
             bool[,] occupied = new bool[currentLevelData.gridCols, currentLevelData.gridRows];
             foreach(var a in currentLevelData.arrows)
                MarkGrid(a.gridX, a.gridY, (Direction)a.direction, a.length, occupied, true);
                
             return CanPlaceArrowInGrid(x, y, dir, len, occupied);
        }

        private bool HasDirectConflict(int x, int y, Direction dir, int len, bool[,] grid, LevelData data)
        {
            // Simple check: Does this arrow point directly into an adjacent arrow that points back?
            // "Head to Head" conflict.
            
            // Check the cell the arrow is pointing TO
            int facingX = x;
            int facingY = y;
            Direction opposingDir = Direction.Up;

            switch (dir)
            {
                case Direction.Up: facingY = y + 1; opposingDir = Direction.Down; break;
                case Direction.Down: facingY = y - 1; opposingDir = Direction.Up; break;
                case Direction.Left: facingX = x - 1; opposingDir = Direction.Right; break;
                case Direction.Right: facingX = x + 1; opposingDir = Direction.Left; break;
            }

            if (facingX >= 0 && facingX < data.gridCols && facingY >= 0 && facingY < data.gridRows)
            {
                // Is there an arrow head at (facingX, facingY)?
                // And does it face 'opposingDir'?
                var neighbor = currentLevelData.arrows.Find(a => a.gridX == facingX && a.gridY == facingY);
                if (neighbor != null && neighbor.direction == (int)opposingDir)
                {
                    return true; // Conflict!
                }
            }
            return false;
        }
        
        private void RemoveArrowsAt(int x, int y, Direction dir, int len)
        {
             // Simple implementation: Remove any arrow that overlaps with the new one
             // This requires identifying all cells the new arrow will take
             List<Vector2Int> newCells = GetArrowCells(x, y, dir, len);
             
             for (int i = currentLevelData.arrows.Count - 1; i >= 0; i--)
             {
                 var a = currentLevelData.arrows[i];
                 List<Vector2Int> existingCells = GetArrowCells(a.gridX, a.gridY, (Direction)a.direction, a.length);
                 
                 bool overlaps = false;
                 foreach(var c1 in newCells)
                    foreach(var c2 in existingCells)
                        if(c1 == c2) overlaps = true;
                        
                 if(overlaps) currentLevelData.arrows.RemoveAt(i);
             }
        }

        private bool CanPlaceArrowInGrid(int x, int y, Direction dir, int len, bool[,] grid)
        {
            List<Vector2Int> cells = GetArrowCells(x, y, dir, len);
            foreach(var c in cells)
            {
                if (c.x < 0 || c.x >= currentLevelData.gridCols || c.y < 0 || c.y >= currentLevelData.gridRows) return false;
                if (grid[c.x, c.y]) return false;
            }
            return true;
        }

        private void MarkGrid(int x, int y, Direction dir, int len, bool[,] grid, bool state)
        {
            List<Vector2Int> cells = GetArrowCells(x, y, dir, len);
            foreach(var c in cells)
            {
                 if (c.x >= 0 && c.x < currentLevelData.gridCols && c.y >= 0 && c.y < currentLevelData.gridRows)
                    grid[c.x, c.y] = state;
            }
        }

        private void DrawKeyEditor()
        {
            EditorGUILayout.LabelField("Key Editor (Wall Grid)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            selectedLockId = EditorGUILayout.IntField("Target Lock ID:", selectedLockId);
            if (GUILayout.Button("Clear All Keys", GUILayout.Width(120)))
            {
                Undo.RecordObject(currentLevelData, "Clear Keys");
                currentLevelData.keys.Clear();
                EditorUtility.SetDirty(currentLevelData);
            }
            EditorGUILayout.EndHorizontal();

            float cellSize = 40f;
            float startX = 20f;
            float startY = GUILayoutUtility.GetRect(0, cellSize * currentLevelData.height + 20).y + 10;
            Rect bgRect = new Rect(startX, startY, currentLevelData.width * cellSize, currentLevelData.height * cellSize);
            EditorGUI.DrawRect(bgRect, new Color(0.2f, 0.2f, 0.2f));

            for (int y = currentLevelData.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < currentLevelData.width; x++)
                {
                    Rect cellRect = new Rect(startX + x * cellSize + 2, startY + (currentLevelData.height - 1 - y) * cellSize + 2, cellSize - 4, cellSize - 4);
                    KeyData key = currentLevelData.keys.Find(k => k.gridX == x && k.gridY == y);
                    
                    if (cellRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
                    {
                        Undo.RecordObject(currentLevelData, "Modify Key");
                        if (Event.current.button == 0) // LMB
                        {
                            if (key == null) currentLevelData.keys.Add(new KeyData { gridX = x, gridY = y, lockId = selectedLockId });
                            else key.lockId = selectedLockId;
                        }
                        else if (Event.current.button == 1) // RMB
                        {
                            if (key != null) currentLevelData.keys.Remove(key);
                        }
                        EditorUtility.SetDirty(currentLevelData);
                        Event.current.Use();
                    }

                    EditorGUI.DrawRect(cellRect, key != null ? new Color(1f, 0.84f, 0f) : new Color(0.3f, 0.3f, 0.3f));
                    if (key != null) GUI.Label(cellRect, key.lockId.ToString(), new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter });
                }
            }
            EditorGUILayout.HelpBox("LMB to place/edit Key, RMB to remove. Set Lock ID to pair with a Lock.", MessageType.None);
        }

        private void DrawLockEditor()
        {
            EditorGUILayout.LabelField("Lock Editor (Arrow Grid)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            selectedLockId = EditorGUILayout.IntField("Lock ID:", selectedLockId);
            selectedLockSizeX = EditorGUILayout.IntSlider("Width (Size X):", selectedLockSizeX, 1, currentLevelData.gridCols);
            selectedLockSizeY = EditorGUILayout.IntSlider("Height (Size Y):", selectedLockSizeY, 1, currentLevelData.gridRows);
            if (GUILayout.Button("Clear All Locks"))
            {
                Undo.RecordObject(currentLevelData, "Clear Locks");
                currentLevelData.locks.Clear();
                EditorUtility.SetDirty(currentLevelData);
            }
            EditorGUILayout.EndVertical();

            float cellSize = 40f;
            float startX = 20f;
            float startY = GUILayoutUtility.GetRect(0, cellSize * currentLevelData.gridRows + 20).y + 10;
            Rect bgRect = new Rect(startX, startY, currentLevelData.gridCols * cellSize, currentLevelData.gridRows * cellSize);
            EditorGUI.DrawRect(bgRect, new Color(0.15f, 0.15f, 0.15f));

            foreach (var lockData in currentLevelData.locks)
            {
                Rect lRect = new Rect(
                    startX + lockData.gridX * cellSize + 4,
                    startY + (currentLevelData.gridRows - lockData.gridY - lockData.sizeY) * cellSize + 4,
                    lockData.sizeX * cellSize - 8,
                    lockData.sizeY * cellSize - 8
                );
                EditorGUI.DrawRect(lRect, new Color(0.6f, 0.2f, 0.2f, 0.8f));
                GUI.Label(lRect, $"ID:{lockData.lockId}", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
            }

            if (bgRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
            {
                Vector2 localMouse = Event.current.mousePosition - new Vector2(startX, startY);
                int gx = Mathf.FloorToInt(localMouse.x / cellSize);
                int gy = currentLevelData.gridRows - 1 - Mathf.FloorToInt(localMouse.y / cellSize);

                Undo.RecordObject(currentLevelData, "Modify Lock");
                if (Event.current.button == 0) // LMB: Place
                {
                    currentLevelData.locks.RemoveAll(l => l.gridX == gx && l.gridY == gy);
                    currentLevelData.locks.Add(new LockData { gridX = gx, gridY = gy, sizeX = selectedLockSizeX, sizeY = selectedLockSizeY, lockId = selectedLockId });
                }
                else if (Event.current.button == 1) // RMB: Remove
                {
                    currentLevelData.locks.RemoveAll(l => gx >= l.gridX && gx < l.gridX + l.sizeX && gy >= l.gridY && gy < l.gridY + l.sizeY);
                }
                EditorUtility.SetDirty(currentLevelData);
                Event.current.Use();
            }
            EditorGUILayout.HelpBox("LMB to place Lock at position (uses Size X/Y), RMB to delete locks.", MessageType.None);
        }

        private List<Vector2Int> GetArrowCells(int x, int y, Direction dir, int len)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            Vector2Int pos = new Vector2Int(x, y);
            cells.Add(pos); // Head

            Vector2Int step = Vector2Int.zero;
            switch(dir)
            {
                case Direction.Up: step = new Vector2Int(0, -1); break;
                case Direction.Down: step = new Vector2Int(0, 1); break;
                case Direction.Left: step = new Vector2Int(1, 0); break;
                case Direction.Right: step = new Vector2Int(-1, 0); break;
            }
            
            for(int i=1; i<len; i++)
            {
                pos += step;
                cells.Add(pos);
            }
            return cells;
        }

        private bool IsBodyPart(ArrowData arrow, int x, int y)
        {
            if (arrow.segments == null) return false;
            for (int i = 1; i < arrow.segments.Count; i++)
            {
                if (arrow.segments[i].x == x && arrow.segments[i].y == y) return true;
            }
            return false;
        }

        private List<Vector2Int> GetArrowCells(ArrowData arrow)
        {
            if (arrow.segments != null && arrow.segments.Count > 0) return arrow.segments;
            return GetArrowCells(arrow.gridX, arrow.gridY, (Direction)arrow.direction, arrow.length);
        }
    }
}
#endif
