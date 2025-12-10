using UnityEngine;
using UnityEditor;
using ArrowBlast.Data;
using ArrowBlast.Core;
using ArrowBlast.Managers;
using System.Collections.Generic;

namespace ArrowBlast.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private LevelData currentLevel = new LevelData();
        private string levelName = "NewLevel";
        
        // Editor State
        private BlockColor selectedColor = BlockColor.Red;
        
        // Puzzle Interaction State
        private bool isDraggingArrow = false;
        private Vector2Int arrowStartPos;
        private Vector2Int arrowCurrentPos;
        
        // Wall Interaction State
        private bool isPaintingWall = false;

        private Vector2 scrollPos;
        private bool isPuzzleTab = true;

        // Constants
        private const float CELL_SIZE = 40f;
        private const float PADDING = 2f;

        [MenuItem("ArrowBlast/Level Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(900, 1000); // Bigger window requested
        }

        private void OnEnable()
        {
            if(currentLevel.arrows == null) currentLevel.arrows = new List<ArrowData>();
            if(currentLevel.blocks == null) currentLevel.blocks = new List<BlockData>();
            
            // Defaults
            if(currentLevel.gridRows == 0) { currentLevel.gridRows = 8; currentLevel.gridCols = 6; }
            if(currentLevel.width == 0) { currentLevel.width = 6; currentLevel.height = 10; }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            DrawHeader();
            GUILayout.Space(10);
            
            // Stats & Constraints
            DrawStats();
            GUILayout.Space(10);
            
            // Tabs
            GUILayout.BeginHorizontal();
            if(GUILayout.Toggle(isPuzzleTab, "Puzzle Grid (Bottom)", EditorStyles.toolbarButton)) isPuzzleTab = true;
            if(GUILayout.Toggle(!isPuzzleTab, "Wall Grid (Top)", EditorStyles.toolbarButton)) isPuzzleTab = false;
            GUILayout.EndHorizontal();

            // Tools (Color Picker)
            DrawTools();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            if (isPuzzleTab)
            {
                DrawPuzzleGrid();
            }
            else
            {
                DrawWallGrid();
            }

            EditorGUILayout.EndScrollView();
            
            // Interaction Logic Helper
            HandleGlobalEvents();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal("box");
            if(GUILayout.Button("New", GUILayout.Width(60))) NewLevel();
            if(GUILayout.Button("Load", GUILayout.Width(60))) LoadLevel();
            if(GUILayout.Button("Save", GUILayout.Width(60))) SaveLevel();
            GUILayout.Space(20);
            GUILayout.Label("Level Name:", GUILayout.Width(80));
            levelName = EditorGUILayout.TextField(levelName);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Grid Size:", GUILayout.Width(60));
            currentLevel.gridCols = EditorGUILayout.IntField(currentLevel.gridCols, GUILayout.Width(40));
            GUILayout.Label("x");
            currentLevel.gridRows = EditorGUILayout.IntField(currentLevel.gridRows, GUILayout.Width(40));
            
            GUILayout.Space(20);
            GUILayout.Label("Wall Size:", GUILayout.Width(60));
            currentLevel.width = EditorGUILayout.IntField(currentLevel.width, GUILayout.Width(40));
            GUILayout.Label("x");
            currentLevel.height = EditorGUILayout.IntField(currentLevel.height, GUILayout.Width(40));
            GUILayout.EndHorizontal();
        }

        private void DrawStats()
        {
            int totalAmmo = CalculateTotalAmmo(null); // All colors
            int totalBlocks = currentLevel.blocks.Count;
            
            // Selected Color Stats
            int colorAmmo = CalculateTotalAmmo(selectedColor);
            int colorBlocks = 0;
            foreach(var b in currentLevel.blocks) if(b.colorIndex == (int)selectedColor) colorBlocks++;

            EditorGUILayout.HelpBox($"TOTAL: Ammo {totalAmmo} vs Blocks {totalBlocks}\n" +
                                    $"SELECTED ({selectedColor}): Ammo {colorAmmo} vs Blocks {colorBlocks} | Remaining: {colorAmmo - colorBlocks}", 
                                    (colorAmmo >= colorBlocks) ? MessageType.Info : MessageType.Error);
        }

        private void DrawTools()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Paint Color:", EditorStyles.boldLabel);
            selectedColor = (BlockColor)EditorGUILayout.EnumPopup(selectedColor, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Drag on Grid to Paint/Place");
            GUILayout.EndHorizontal();
        }

        // ---------------- PUZZLE GRID LOGIC ---------------- //

        private void DrawPuzzleGrid()
        {
            GUILayout.Label("Bottom Puzzle - Drag to create Arrow (Start -> End Direction defines Head)", EditorStyles.boldLabel);
            
            Rect gridRect = GUILayoutUtility.GetRect(currentLevel.gridCols * CELL_SIZE, currentLevel.gridRows * CELL_SIZE);
            // Draw Background
            EditorGUI.DrawRect(gridRect, Color.black);
            
            // Draw Events
            Event e = Event.current;
            Vector2Int mouseGridPos = GetGridPos(e.mousePosition, gridRect, currentLevel.gridCols, currentLevel.gridRows);

            // Handle Input
            if (gridRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    // Check if clicking existing arrow?
                    ArrowData existing = FindArrowOccupying(mouseGridPos.x, mouseGridPos.y);
                    if (existing != null)
                    {
                        currentLevel.arrows.Remove(existing); // Delete on click/drag start
                    }
                    
                    isDraggingArrow = true;
                    arrowStartPos = mouseGridPos;
                    arrowCurrentPos = mouseGridPos;
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && isDraggingArrow)
                {
                    arrowCurrentPos = mouseGridPos;
                    Repaint();
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && isDraggingArrow)
                {
                    isDraggingArrow = false;
                    FinalizeArrowPlacement();
                    Repaint();
                    e.Use();
                }
            }
            
            // Draw Cells
            for (int y = 0; y < currentLevel.gridRows; y++)
            {
                for (int x = 0; x < currentLevel.gridCols; x++)
                {
                    Rect cellRect = new Rect(gridRect.x + x * CELL_SIZE, gridRect.y + y * CELL_SIZE, CELL_SIZE - PADDING, CELL_SIZE - PADDING);
                    EditorGUI.DrawRect(cellRect, new Color(0.2f, 0.2f, 0.2f));
                }
            }

            // Draw Existing Arrows
            foreach(var arrow in currentLevel.arrows)
            {
                DrawArrow(gridRect, arrow, false);
            }

            // Draw Drag Preview
            if (isDraggingArrow)
            {
                ArrowData preview = CalculatePreviewArrow(arrowStartPos, arrowCurrentPos);
                if (preview != null) DrawArrow(gridRect, preview, true);
            }
        }
        
        private ArrowData CalculatePreviewArrow(Vector2Int start, Vector2Int current)
        {
            // Start is the HEAD/PIVOT? 
            // Request: "Drag up to 4 tile... take up exactly the amount of tile".
            // Typically "Drag to create" implies Start = Tail, Current = Head.
            // Let's implement: Start = Tail, End = Head.
            
            int dx = current.x - start.x;
            int dy = current.y - start.y;
            
            if (dx == 0 && dy == 0) return null; // Too small

            ArrowData a = new ArrowData();
            a.colorIndex = (int)selectedColor;
            
            // Determine Direction and Length
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                // Horizontal
                a.direction = dx > 0 ? (int)Direction.Right : (int)Direction.Left;
                a.length = Mathf.Min(4, Mathf.Abs(dx) + 1);
            }
            else
            {
                // Vertical
                a.direction = dy > 0 ? (int)Direction.Down : (int)Direction.Up; // Y increases Down in GUI?
                // Visual Grid: Y0 is typically Top in GUI rect.
                // Standard: Y increases Down.
                // Arrow Enum: 0:Up, 1:Right, 2:Down, 3:Left.
                // If I drag Mouse DOWN (Positive Y), Direction is DOWN.
                a.direction = dy > 0 ? (int)Direction.Down : (int)Direction.Up;
                a.length = Mathf.Min(4, Mathf.Abs(dy) + 1);
            }
            
            // Pivot Position
            // If dragging Tail -> Head.
            // The Arrow Logic usually puts Pivot at Head? No, "Escape" checks path relative to (X,Y).
            // (X,Y) is the location in the grid array.
            // If I place Arrow at (2,2) and it goes UP.
            // Check CanArrowEscape: dy = -1. Checks (2,1), (2,0).
            // So (2,2) MUST be the "Start of movement" i.e., the Head (closest to wall/exit?).
            // Wait. If Arrow is UP, it moves UP (-Y).
            // So Head is at Top. Tail is at Bottom.
            // If I drag Tail (Bottom) -> Pivot (Top).
            // The resulting Arrow Object sits at Pivot.
            
            // Let's assume (X,Y) in Data is the HEAD.
            // So if I Drag Start (Tail) -> End (Head).
            // The Arrow Data (GridX, GridY) should be 'End'.
            // Because valid movement starts from GridX, GridY.
            
            // But if I limit length to 4. Start might be far away.
            // End is always arrowCurrentPos?
            
            // Re-calc based on length limitation
            Vector2Int dirVec = GetDirectionVector(a.direction);
            // End = Start + Dir * (Length-1).
            // But we already capped Length.
            // Let's find strict Head position based on Start(Tail).
            
            a.gridX = start.x + dirVec.x * (a.length - 1);
            a.gridY = start.y + dirVec.y * (a.length - 1);
            
            return a;
        }

        private void FinalizeArrowPlacement()
        {
            ArrowData newArrow = CalculatePreviewArrow(arrowStartPos, arrowCurrentPos);
            if (newArrow != null)
            {
                // Check if valid (bounds) - GUI rect check covers it mostly but check indices
                if (newArrow.gridX >= 0 && newArrow.gridX < currentLevel.gridCols &&
                    newArrow.gridY >= 0 && newArrow.gridY < currentLevel.gridRows)
                {
                    // Check Overlaps (Body occupation)
                    // Remove any existing arrows that overlap with THIS new arrow's body
                    List<Vector2Int> occupied = GetOccupiedCells(newArrow);
                    
                    // Simple: remove anything in these cells
                    foreach(var pos in occupied)
                    {
                        ArrowData ex = FindArrowOccupying(pos.x, pos.y);
                        if(ex != null) currentLevel.arrows.Remove(ex);
                    }
                    
                    currentLevel.arrows.Add(newArrow);
                }
            }
        }

        // ---------------- WALL GRID LOGIC ---------------- //

        private void DrawWallGrid()
        {
            GUILayout.Label("Top Wall - Drag to Paint Blocks (Requires Ammo)", EditorStyles.boldLabel);
            
            Rect gridRect = GUILayoutUtility.GetRect(currentLevel.width * CELL_SIZE, currentLevel.height * CELL_SIZE);
            EditorGUI.DrawRect(gridRect, Color.black);
            
            Event e = Event.current;
            // Wall is conventionally bottom-up index in game, but top-down in visual?
            // "Top Section: The Wall". "Look at the bottom row of the wall".
            // Let's render Top-Down visually where Index 0 is Bottom? 
            // Or Index 0 is Top-Left visually?
            // In Game: y=0 is Bottom.
            // In Editor Rect: y=0 is Top.
            // So visual y = (Height - 1 - gridY) * size.
            
            Vector2Int mouseGridPos = GetGridPosWall(e.mousePosition, gridRect, currentLevel.width, currentLevel.height);

             if (gridRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    if (e.button == 0) // Left click paint
                    {
                        TryPaintBlock(mouseGridPos);
                        e.Use();
                    }
                    else if (e.button == 1) // Right click erase
                    {
                        TryRemoveBlock(mouseGridPos);
                        e.Use();
                    }
                }
            }

            // Draw Cells
            for (int y = 0; y < currentLevel.height; y++)
            {
                // y is internal gridY.
                // visualY needs conversion.
                float vy = (currentLevel.height - 1 - y) * CELL_SIZE; 
                
                for (int x = 0; x < currentLevel.width; x++)
                {
                    Rect cellRect = new Rect(gridRect.x + x * CELL_SIZE, gridRect.y + vy, CELL_SIZE - PADDING, CELL_SIZE - PADDING);
                    
                    BlockData b = FindBlock(x, y);
                    if (b != null)
                    {
                        EditorGUI.DrawRect(cellRect, GetColor(b.colorIndex));
                    }
                    else
                    {
                        EditorGUI.DrawRect(cellRect, new Color(0.3f, 0.3f, 0.3f));
                    }
                }
            }
        }
        
        private void TryPaintBlock(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= currentLevel.width || pos.y < 0 || pos.y >= currentLevel.height) return;

            // Constrain by Ammo
            int currentBlocks = 0;
            foreach(var b in currentLevel.blocks) if(b.colorIndex == (int)selectedColor) currentBlocks++;
            int maxAmmo = CalculateTotalAmmo(selectedColor);

            BlockData existing = FindBlock(pos.x, pos.y);
            
            if (existing != null)
            {
                // If overwriting same color, do nothing
                if (existing.colorIndex == (int)selectedColor) return;
                
                // If changing color, check budget
                // Remove old, add new
                if (currentBlocks < maxAmmo)
                {
                    existing.colorIndex = (int)selectedColor;
                }
            }
            else
            {
                // Add new
                 if (currentBlocks < maxAmmo)
                 {
                     BlockData nb = new BlockData();
                     nb.gridX = pos.x;
                     nb.gridY = pos.y;
                     nb.colorIndex = (int)selectedColor;
                     currentLevel.blocks.Add(nb);
                 }
            }
        }
        
        private void TryRemoveBlock(Vector2Int pos)
        {
            BlockData b = FindBlock(pos.x, pos.y);
            if(b!=null) currentLevel.blocks.Remove(b);
        }

        // ---------------- HELPERS ---------------- //

        private void DrawArrow(Rect gridRect, ArrowData a, bool preview)
        {
             // Draw Head
             Color c = GetColor(a.colorIndex);
             if(preview) c.a = 0.5f;

             // Occupied cells
             List<Vector2Int> cells = GetOccupiedCells(a);
             foreach(var pos in cells)
             {
                 Rect cellRect = new Rect(gridRect.x + pos.x * CELL_SIZE, gridRect.y + pos.y * CELL_SIZE, CELL_SIZE - PADDING, CELL_SIZE - PADDING);
                 EditorGUI.DrawRect(cellRect, c);
             }

             // Draw Head Indicator (Triangle or Dot)
             Rect headRect = new Rect(gridRect.x + a.gridX * CELL_SIZE + 10, gridRect.y + a.gridY * CELL_SIZE + 10, 20, 20);
             GUI.color = Color.black;
             GUI.Label(headRect, GetDirChar(a.direction));
             GUI.color = Color.white;
        }
        
        private string GetDirChar(int d)
        {
            switch((Direction)d) {
                case Direction.Up: return "^";
                case Direction.Down: return "v";
                case Direction.Left: return "<";
                case Direction.Right: return ">";
            }
            return "";
        }

        private Vector2Int GetGridPos(Vector2 mouse, Rect gridRect, int cols, int rows)
        {
            int x = Mathf.FloorToInt((mouse.x - gridRect.x) / CELL_SIZE);
            int y = Mathf.FloorToInt((mouse.y - gridRect.y) / CELL_SIZE);
            x = Mathf.Clamp(x, 0, cols - 1);
            y = Mathf.Clamp(y, 0, rows - 1);
            return new Vector2Int(x, y);
        }
        
        private Vector2Int GetGridPosWall(Vector2 mouse, Rect gridRect, int cols, int rows)
        {
            int x = Mathf.FloorToInt((mouse.x - gridRect.x) / CELL_SIZE);
            // Visual Y is inverted from Grid Y
            // vy = (H - 1 - gy) * size
            // gy = H - 1 - (mouseY / size)
            int vy = Mathf.FloorToInt((mouse.y - gridRect.y) / CELL_SIZE);
            int y = rows - 1 - vy;
            
            x = Mathf.Clamp(x, 0, cols - 1);
            y = Mathf.Clamp(y, 0, rows - 1);
            return new Vector2Int(x, y);
        }

        private List<Vector2Int> GetOccupiedCells(ArrowData a)
        {
            List<Vector2Int> list = new List<Vector2Int>();
            Vector2Int head = new Vector2Int(a.gridX, a.gridY);
            list.Add(head);
            
            // Extrude BACKWARDS from direction
            Vector2Int back = -GetDirectionVector(a.direction);
            
            for(int i=1; i<a.length; i++)
            {
                list.Add(head + back * i);
            }
            return list;
        }
        
        private ArrowData FindArrowOccupying(int x, int y)
        {
            foreach(var a in currentLevel.arrows)
            {
                if(GetOccupiedCells(a).Contains(new Vector2Int(x,y))) return a;
            }
            return null;
        }

        private Vector2Int GetDirectionVector(int dir)
        {
             switch((Direction)dir)
             {
                 case Direction.Up: return new Vector2Int(0, -1); // Up in Grid array is usually Y-1 if 0 is top. 
                 // Wait. Editor rendering: Y0 Top. Y+ Down.
                 // Game Logic: depends on interpretation.
                 // In `LevelGenerator`: Up (0) -> dy = 1? And `CheckWin` logic used 0=Bottom.
                 // PREVIOUS LOGIC WAS Y+ IS UP.
                 // BUT EDITOR GUI uses Y+ DOWN.
                 // We must map consistently.
                 // If I want "UP" arrow to point visually UP in Editor.
                 // Visual Up is Y minus.
                 // So Direction.Up vector in Editor coords is (0, -1).
                 // In Game Logic (LevelGenerator), Direction.Up was dy=1 (Y+).
                 // This implies Game World Y+ is Up.
                 // Editor Grid Y+ is Down.
                 // So conversion is valid.
                 return new Vector2Int(0, -1);
                 
                 case Direction.Down: return new Vector2Int(0, 1);
                 case Direction.Left: return new Vector2Int(-1, 0);
                 case Direction.Right: return new Vector2Int(1, 0);
             }
             return Vector2Int.zero;
        }

        private BlockData FindBlock(int x, int y)
        {
            return currentLevel.blocks.Find(b => b.gridX == x && b.gridY == y);
        }

        private Color GetColor(int index)
        {
            switch((BlockColor)index)
            {
                case BlockColor.Red: return Color.red;
                case BlockColor.Blue: return Color.cyan;
                case BlockColor.Green: return Color.green;
                case BlockColor.Yellow: return Color.yellow;
                case BlockColor.Purple: return new Color(0.5f, 0, 0.5f);
                case BlockColor.Orange: return new Color(1, 0.5f, 0);
                default: return Color.white;
            }
        }
        
        private int CalculateTotalAmmo(BlockColor? filter)
        {
             int total = 0;
             foreach(var a in currentLevel.arrows) {
                 if(filter != null && a.colorIndex != (int)filter) continue;
                 int val = 0;
                 switch(a.length) { case 1: val=10; break; case 2: val=20; break; case 3: val=30; break; case 4: val=40; break; }
                 total += val;
             }
             return total;
        }
        
        private void SaveLevel() { SaveSystem.SaveLevel(currentLevel, levelName); }
        private void LoadLevel() { LevelData d = SaveSystem.LoadLevel(levelName); if(d != null) currentLevel = d; }
        private void NewLevel() { currentLevel = new LevelData(); currentLevel.levelName = "New"; currentLevel.gridRows=8; currentLevel.gridCols=6; currentLevel.width=6; currentLevel.height=10; }
        private void HandleGlobalEvents() { if (Event.current.type == EventType.MouseDown) GUI.FocusControl(null); }
    }
}
