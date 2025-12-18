#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ArrowBlast.Core;
using ArrowBlast.Data;
using ArrowBlast.Managers;

namespace ArrowBlast.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private string levelName = "MyLevel";
        private int wallWidth = 6;
        private int wallHeight = 8;
        private int arrowRows = 8;
        private int arrowCols = 6;

        private BlockColor selectedBlockColor = BlockColor.Red;
        private Direction selectedArrowDirection = Direction.Up;
        private int selectedArrowLength = 1;

        private List<BlockData> blocks = new List<BlockData>();
        private List<ArrowData> arrows = new List<ArrowData>();

        private Vector2 scrollPos;
        private enum EditMode { Wall, Arrows }
        private EditMode currentMode = EditMode.Wall;

        [MenuItem("Arrow Blast/Level Editor")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditorWindow>("Level Editor");
        }

        private void OnGUI()
        {
            GUILayout.Label("Arrow Blast Level Editor", EditorStyles.boldLabel);
            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Level Settings
            EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
            levelName = EditorGUILayout.TextField("Level Name", levelName);
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Wall Dimensions (Portrait)", EditorStyles.miniBoldLabel);
            wallWidth = EditorGUILayout.IntSlider("Width", wallWidth, 3, 8);
            wallHeight = EditorGUILayout.IntSlider("Height", wallHeight, 5, 12);
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Arrow Grid Dimensions", EditorStyles.miniBoldLabel);
            arrowCols = EditorGUILayout.IntSlider("Columns", arrowCols, 3, 8);
            arrowRows = EditorGUILayout.IntSlider("Rows", arrowRows, 5, 12);

            GUILayout.Space(10);

            // Edit Mode
            currentMode = (EditMode)GUILayout.Toolbar((int)currentMode, new string[] { "Edit Wall", "Edit Arrows" });

            GUILayout.Space(10);

            if (currentMode == EditMode.Wall)
            {
                DrawWallEditor();
            }
            else
            {
                DrawArrowEditor();
            }

            GUILayout.Space(10);

            // Actions
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Clear All", GUILayout.Height(30)))
            {
                blocks.Clear();
                arrows.Clear();
            }

            if (GUILayout.Button("Generate Random Level", GUILayout.Height(30)))
            {
                GenerateRandomLevel();
            }

            if (GUILayout.Button("Save Level", GUILayout.Height(40)))
            {
                SaveLevel();
            }

            if (GUILayout.Button("Load Level", GUILayout.Height(30)))
            {
                LoadLevel();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawWallEditor()
        {
            EditorGUILayout.LabelField("Wall Block Editor", EditorStyles.boldLabel);
            selectedBlockColor = (BlockColor)EditorGUILayout.EnumPopup("Block Color", selectedBlockColor);

            GUILayout.Space(5);
            if (GUILayout.Button("Add Random Block"))
            {
                blocks.Add(new BlockData
                {
                    colorIndex = (int)selectedBlockColor,
                    gridX = Random.Range(0, wallWidth),
                    gridY = Random.Range(0, wallHeight)
                });
            }

            GUILayout.Space(5);
            EditorGUILayout.LabelField($"Blocks: {blocks.Count}", EditorStyles.miniLabel);

            if (GUILayout.Button("Clear Blocks"))
            {
                blocks.Clear();
            }
        }

        private void DrawArrowEditor()
        {
            EditorGUILayout.LabelField("Arrow Editor", EditorStyles.boldLabel);
            selectedBlockColor = (BlockColor)EditorGUILayout.EnumPopup("Arrow Color", selectedBlockColor);
            selectedArrowDirection = (Direction)EditorGUILayout.EnumPopup("Direction", selectedArrowDirection);
            selectedArrowLength = EditorGUILayout.IntSlider("Length", selectedArrowLength, 1, 4);

            GUILayout.Space(5);
            if (GUILayout.Button("Add Random Arrow"))
            {
                arrows.Add(new ArrowData
                {
                    colorIndex = (int)selectedBlockColor,
                    direction = (int)selectedArrowDirection,
                    length = selectedArrowLength,
                    gridX = Random.Range(0, arrowCols),
                    gridY = Random.Range(0, arrowRows)
                });
            }

            GUILayout.Space(5);
            EditorGUILayout.LabelField($"Arrows: {arrows.Count}", EditorStyles.miniLabel);

            if (GUILayout.Button("Clear Arrows"))
            {
                arrows.Clear();
            }
        }

        private void GenerateRandomLevel()
        {
            // Find or create a temp LevelGenerator
            LevelGenerator gen = FindObjectOfType<LevelGenerator>();
            if (gen == null)
            {
                GameObject tempObj = new GameObject("TempLevelGenerator");
                gen = tempObj.AddComponent<LevelGenerator>();
            }

            LevelData data = gen.GenerateRandomLevel(1, wallWidth, wallHeight, arrowRows, arrowCols);
            blocks = data.blocks;
            arrows = data.arrows;

            Debug.Log("Random level generated!");
        }

        private void SaveLevel()
        {
            LevelData level = new LevelData
            {
                levelName = levelName,
                width = wallWidth,
                height = wallHeight,
                gridRows = arrowRows,
                gridCols = arrowCols,
                blocks = blocks,
                arrows = arrows
            };

            SaveSystem.SaveLevel(level, levelName);
            EditorUtility.DisplayDialog("Success", $"Level '{levelName}' saved!", "OK");
        }

        private void LoadLevel()
        {
            LevelData level = SaveSystem.LoadLevel(levelName);
            if (level != null)
            {
                wallWidth = level.width;
                wallHeight = level.height;
                arrowRows = level.gridRows;
                arrowCols = level.gridCols;
                blocks = level.blocks;
                arrows = level.arrows;

                EditorUtility.DisplayDialog("Success", $"Level '{levelName}' loaded!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"Level '{levelName}' not found!", "OK");
            }
        }
    }
}
#endif
