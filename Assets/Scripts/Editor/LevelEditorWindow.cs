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
        private BlockColor selectedColor = BlockColor.Red;
        private Direction selectedDirection = Direction.Up;
        private int selectedLength = 1;
        private int concurrentArrows = 2; // Number of arrows handled at once for wall randomization

        // Interaction State
        private enum RandomMode { Normal, Hard, Mixed }
        private List<Vector2Int> arrowDragPath = new List<Vector2Int>();
        private ArrowData ghostArrow = null;
        private int originalDragArrowIndex = -1; // To restore if cancelled or to delete if replaced
        private BlockColor? capturedColor = null;
        private List<ArrowData> solvableArrowOrder = new List<ArrowData>(); // Stores the sequence of solvable arrows during generation
        private Vector2Int selectedBlockPos = Vector2Int.zero;
        private bool hasSelectedBlock = false;

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
            else
            {
                DrawArrowEditor();
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
            int totalBlocks = currentLevelData.blocks.Count;
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
                EditorUtility.SetDirty(currentLevelData);
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
                    
                    Color drawColor = Color.gray;
                    if (block != null)
                    {
                        drawColor = GetColorFromEnum((BlockColor)block.colorIndex);
                    }
                    
                    // Selection: click an existing colored block to select it (for applying an arrow via keyboard)
                    if (cellRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        if (block != null)
                        {
                            selectedBlockPos = new Vector2Int(x, y);
                            hasSelectedBlock = true;
                            Repaint();
                            Event.current.Use();
                        }
                        else
                        {
                            hasSelectedBlock = false;
                        }
                    }

                    // Interaction: Require Shift + Click/Drag for paint/erase actions
                    bool isAction = false;
                    if (cellRect.Contains(Event.current.mousePosition))
                    {
                        if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.shift)
                            isAction = true;
                    }

                    if (isAction)
                    {
                        Undo.RecordObject(currentLevelData, "Paint Block");
                        
                        // Left Click/Drag: Paint
                        if (Event.current.button == 0) 
                        {
                            if (block == null)
                            {
                                currentLevelData.blocks.Add(new BlockData { gridX = x, gridY = y, colorIndex = (int)selectedColor });
                            }
                            else
                            {
                                block.colorIndex = (int)selectedColor;
                            }
                            GUI.changed = true;
                        }
                        // Right Click/Drag: Erase
                        else if (Event.current.button == 1) 
                        {
                            if (block != null) currentLevelData.blocks.Remove(block);
                            GUI.changed = true;
                        }
                        
                        EditorUtility.SetDirty(currentLevelData);
                        Event.current.Use();
                    }

                    EditorGUI.DrawRect(cellRect, drawColor);

                    // Draw selection outline if this block is selected
                    if (hasSelectedBlock && selectedBlockPos == new Vector2Int(x, y))
                    {
                        Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, Color.yellow);
                    }
                    // Draw arrow overlay if an arrow exists at this cell (head)
                    var overlay = currentLevelData.arrows.Find(a => a.gridX == x && a.gridY == y);
                    if (overlay != null)
                    {
                        string dirLabel = GetDirectionArrow((Direction)overlay.direction);
                        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                        style.alignment = TextAnchor.MiddleCenter;
                        style.normal.textColor = Color.black;
                        GUI.Label(cellRect, dirLabel, style);
                        style.normal.textColor = Color.white;
                        GUI.Label(new Rect(cellRect.x - 1, cellRect.y - 1, cellRect.width, cellRect.height), dirLabel, style);
                    }
                }
            }
            
            if (GUI.changed) Repaint();
            
            // Keyboard: if a block is selected, allow arrow keys to convert it into an arrow
            if (Event.current.type == EventType.KeyDown && hasSelectedBlock)
            {
                Direction dir = Direction.Up;
                bool keyHandled = true;
                switch (Event.current.keyCode)
                {
                    case KeyCode.UpArrow: dir = Direction.Up; break;
                    case KeyCode.DownArrow: dir = Direction.Down; break;
                    case KeyCode.LeftArrow: dir = Direction.Left; break;
                    case KeyCode.RightArrow: dir = Direction.Right; break;
                    default: keyHandled = false; break;
                }

                if (keyHandled)
                {
                    // Verify selected position maps into arrow grid
                    int sx = selectedBlockPos.x;
                    int sy = selectedBlockPos.y;
                    if (sy < 0 || sy > currentLevelData.gridCols || sx < 0 || sx > currentLevelData.gridRows)
                    {
                        EditorUtility.DisplayDialog("Out of Bounds", "Selected block is outside the arrow grid and cannot become an arrow: " + sx + ", " + sy + " and the current grid size is " + currentLevelData.gridCols + " and " + currentLevelData.gridRows, "OK");
                    }
                    else
                    {
                        var blockAt = currentLevelData.blocks.Find(b => b.gridX == sx && b.gridY == sy);
                        if (blockAt == null)
                        {
                            EditorUtility.DisplayDialog("No Block", "Selected cell no longer contains a block.", "OK");
                        }
                        else
                        {
                            Undo.RecordObject(currentLevelData, "Convert Block To Arrow");

                            // Keep the block and add or update an arrow on the same cell
                            var existingArrow = currentLevelData.arrows.Find(a => a.gridX == sx && a.gridY == sy);
                            if (existingArrow != null)
                            {
                                existingArrow.direction = (int)dir;
                                existingArrow.colorIndex = blockAt.colorIndex;
                                existingArrow.length = 1;
                                existingArrow.segments = new List<Vector2Int> { new Vector2Int(sx, sy) };
                            }
                            else
                            {
                                var newArrow = new ArrowData
                                {
                                    gridX = sx,
                                    gridY = sy,
                                    colorIndex = blockAt.colorIndex,
                                    direction = (int)dir,
                                    length = 1,
                                    segments = new List<Vector2Int> { new Vector2Int(sx, sy) }
                                };
                                currentLevelData.arrows.Add(newArrow);
                            }

                            EditorUtility.SetDirty(currentLevelData);
                            hasSelectedBlock = false;
                            Repaint();
                        }
                    }
                    Event.current.Use();
                }
            }

            EditorGUILayout.HelpBox("Hold Shift + LMB to Paint, Hold Shift + RMB to Erase. Click a colored block, then press an arrow key to convert it into an arrow.", MessageType.None);
        }

        private void DrawArrowEditor()
        {
            EditorGUILayout.LabelField("Arrow Grid Editor", EditorStyles.boldLabel);
            
            // Tools
            EditorGUILayout.BeginHorizontal();
            selectedColor = (BlockColor)EditorGUILayout.EnumPopup("Color:", selectedColor);
            EditorGUILayout.EndHorizontal();
            
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

            // Grid Visualization
            float cellSize = 40f;
            float startX = 20f;
            float startY = GUILayoutUtility.GetRect(0, cellSize * currentLevelData.gridRows + 20).y + 10;

            // Process Input globally
            if (Event.current.type == EventType.MouseUp && arrowDragPath.Count > 0)
            {
                 FinalizeArrowPath();
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
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                        {
                            if (arrow != null)
                            {
                                originalDragArrowIndex = currentLevelData.arrows.IndexOf(arrow);
                                capturedColor = (BlockColor)arrow.colorIndex;
                                // Start from this cell (Head or Body)
                                // We'll reconstruct segment order if possible
                                List<Vector2Int> existing = GetArrowCells(arrow);
                                int idx = existing.IndexOf(new Vector2Int(x, y));
                                // Head is index 0. If they click body at index idx, we truncate path to [idx...tail]
                                // and then add new cells? 
                                // Actually, simpler: Start from tail up to this cell, or just start a new drag path.
                                // User said: "extend it".
                                // Let's make it so segments are TAIL to HEAD in the drag path list during drawing, 
                                // then reverse for storage.
                                arrowDragPath = new List<Vector2Int> { new Vector2Int(x, y) };
                            }
                            else
                            {
                                originalDragArrowIndex = -1;
                                capturedColor = null;
                                arrowDragPath = new List<Vector2Int> { new Vector2Int(x, y) };
                            }
                            Event.current.Use();
                        }
                        
                        if (Event.current.type == EventType.MouseDrag && arrowDragPath.Count > 0)
                        {
                            Vector2Int currentCell = new Vector2Int(x, y);
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
                                        // Backtrack path
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

                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && arrow != null)
                        {
                            Undo.RecordObject(currentLevelData, "Remove Arrow");
                            currentLevelData.arrows.Remove(arrow);
                            EditorUtility.SetDirty(currentLevelData);
                            Repaint();
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

            EditorGUILayout.HelpBox("LMB Drag to Draw Path (Last point is Head). RMB to Remove.", MessageType.None);
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

            // 1. Determine Sequence
            List<ArrowData> collectionOrder = new List<ArrowData>();
            
            // Use the pre-calculated solvable order if it matches the current arrow count
            if (solvableArrowOrder != null && solvableArrowOrder.Count > 0 && solvableArrowOrder.Count == currentLevelData.arrows.Count)
            {
                collectionOrder = new List<ArrowData>(solvableArrowOrder);
                Debug.Log("[RANDOM WALL] Using pre-calculated solvable order.");
            }
            else
            {
                // Fallback: Simulate to find an order
                var simResult = SimulateArrowCollection();
                foreach (var s in simResult) collectionOrder.Add(s.arrow);
                
                if (collectionOrder.Count < currentLevelData.arrows.Count)
                {
                    foreach (var a in currentLevelData.arrows)
                    {
                        if (!collectionOrder.Contains(a)) collectionOrder.Add(a);
                    }
                    Debug.LogWarning("[RANDOM WALL] Layout partially unsolvable. Blocks might be difficult to clear.");
                }
            }

            // 2. Prepare Block Pool
            int cols = currentLevelData.width;
            int rows = currentLevelData.height;
            int[,] wallGrid = new int[cols, rows];
            for (int x = 0; x < cols; x++) for (int y = 0; y < rows; y++) wallGrid[x, y] = -1;

            int arrowIdx = 0;
            while (arrowIdx < collectionOrder.Count)
            {
                List<int> chunkColors = new List<int>();
                for (int i = 0; i < concurrentArrows && arrowIdx < collectionOrder.Count; i++)
                {
                    ArrowData a = collectionOrder[arrowIdx++];
                    int ammo = GetArrowAmmo(a);
                    for (int k = 0; k < ammo; k++) chunkColors.Add(a.colorIndex);
                }

                // Shuffle the chunk
                for (int i = 0; i < chunkColors.Count; i++)
                {
                    int r = Random.Range(i, chunkColors.Count);
                    int t = chunkColors[i]; chunkColors[i] = chunkColors[r]; chunkColors[r] = t;
                }

                // Distribute chunk
                foreach (int color in chunkColors)
                {
                    List<int> candidateCols = new List<int>();
                    for (int x = 0; x < cols; x++)
                    {
                        if (wallGrid[x, rows - 1] == -1) candidateCols.Add(x);
                    }

                    if (candidateCols.Count == 0) break;

                    int col = candidateCols[Random.Range(0, candidateCols.Count)];
                    for (int y = 0; y < rows; y++)
                    {
                        if (wallGrid[col, y] == -1)
                        {
                            wallGrid[col, y] = color;
                            break;
                        }
                    }
                }
            }

            // 3. Finalize
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
            List<Vector2Int> cells = GetArrowCells(arrow);
            // Skip index 0 (Head)
            for(int i=1; i<cells.Count; i++)
            {
                if(cells[i].x == x && cells[i].y == y) return true;
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
