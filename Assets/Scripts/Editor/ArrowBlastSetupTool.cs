#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using ArrowBlast.Managers;
using ArrowBlast.Game;

namespace ArrowBlast.Editor
{
    /// <summary>
    /// Auto-setup tool for Arrow Blast - Creates scene hierarchy and 3D prefabs
    /// FULL 3D - No 2D sprites, only 3D meshes
    /// </summary>
    public class ArrowBlastSetupTool : EditorWindow
    {
        [MenuItem("Arrow Blast/Auto Setup Scene")]
        public static void ShowWindow()
        {
            GetWindow<ArrowBlastSetupTool>("Arrow Blast Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Arrow Blast - Auto Setup (3D)", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("This will create the complete 3D scene setup for Arrow Blast including:\n" +
                "- GameManager with containers\n" +
                "- 3D Prefabs (Block, Arrow, Slot)\n" +
                "- Portrait camera setup\n" +
                "- All 3D meshes (no 2D sprites)", MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Create Complete 3D Setup", GUILayout.Height(40)))
            {
                CreateCompleteSetup();
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Portrait Layout (Top to Bottom):\n" +
                "• Wall Container (y=6) - 3D Cubes\n" +
                "• Slots Container (y=0) - 3D Quads\n" +
                "• Arrow Container (y=-6) - 3D Meshes", MessageType.None);
        }

        private void CreateCompleteSetup()
        {
            // Create GameManager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            LevelGenerator lg = gmObj.AddComponent<LevelGenerator>();
            AutoScaler autoScaler = gmObj.AddComponent<AutoScaler>();

            // Create containers
            GameObject wallContainer = new GameObject("WallContainer");
            wallContainer.transform.position = new Vector3(0, 6, 0);

            GameObject slotsContainer = new GameObject("SlotsContainer");
            slotsContainer.transform.position = new Vector3(0, 0, 0);

            GameObject arrowContainer = new GameObject("ArrowContainer");
            arrowContainer.transform.position = new Vector3(0, -6, 0);

            // Assign to GameManager via SerializedObject
            SerializedObject so = new SerializedObject(gm);
            so.FindProperty("levelGenerator").objectReferenceValue = lg;
            so.FindProperty("wallContainer").objectReferenceValue = wallContainer.transform;
            so.FindProperty("slotsContainer").objectReferenceValue = slotsContainer.transform;
            so.FindProperty("arrowContainer").objectReferenceValue = arrowContainer.transform;

            // Assign to AutoScaler
            SerializedObject soScaler = new SerializedObject(autoScaler);
            soScaler.FindProperty("wallContainer").objectReferenceValue = wallContainer.transform;
            soScaler.FindProperty("slotsContainer").objectReferenceValue = slotsContainer.transform;
            soScaler.FindProperty("arrowContainer").objectReferenceValue = arrowContainer.transform;
            soScaler.ApplyModifiedProperties();

            // Create 3D prefabs
            Block blockPrefab = CreateBlockPrefab3D();
            Arrow arrowPrefab = CreateArrowPrefab3D();
            Slot slotPrefab = CreateSlotPrefab3D();

            so.FindProperty("blockPrefab").objectReferenceValue = blockPrefab;
            so.FindProperty("arrowPrefab").objectReferenceValue = arrowPrefab;
            so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            so.ApplyModifiedProperties();

            // Setup camera for portrait
            SetupPortraitCamera();

            Debug.Log("✓ Arrow Blast 3D setup complete with AutoScaler!");
            EditorUtility.DisplayDialog("Success", "Arrow Blast 3D scene setup complete!\n\n✓ AutoScaler added\n✓ Containers auto-positioned\n✓ Check the hierarchy and prefabs folder.", "OK");
        }

        private Block CreateBlockPrefab3D()
        {
            // Create 3D Cube for block
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "Block";
            obj.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            Block block = obj.AddComponent<Block>();
            MeshRenderer mr = obj.GetComponent<MeshRenderer>();

            // Create material
            Material mat = new Material(Shader.Find("Standard"));
            mr.material = mat;

            // Set default colors
            SerializedObject so = new SerializedObject(block);
            so.FindProperty("meshRenderer").objectReferenceValue = mr;
            
            Color[] colors = new Color[]
            {
                new Color(1f, 0.2f, 0.2f),    // Red
                new Color(0.2f, 0.5f, 1f),    // Blue
                new Color(0.3f, 1f, 0.3f),    // Green
                new Color(1f, 0.9f, 0.2f),    // Yellow
                new Color(0.8f, 0.3f, 1f),    // Purple
                new Color(1f, 0.6f, 0.2f)     // Orange
            };
            
            SerializedProperty colorProp = so.FindProperty("colorDefinitions");
            colorProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                colorProp.GetArrayElementAtIndex(i).colorValue = colors[i];
            }
            so.ApplyModifiedProperties();

            // Save as prefab
            string path = "Assets/Prefabs/Block.prefab";
            EnsureDirectory("Assets/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            DestroyImmediate(obj);

            return AssetDatabase.LoadAssetAtPath<Block>(path);
        }

        private Arrow CreateArrowPrefab3D()
        {
            GameObject obj = new GameObject("Arrow");
            Arrow arrow = obj.AddComponent<Arrow>();

            // Create 3D body (elongated cube)
            GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyObj.name = "Body";
            bodyObj.transform.SetParent(obj.transform);
            bodyObj.transform.localPosition = new Vector3(0, -0.4f, 0);
            bodyObj.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
            MeshRenderer bodyMr = bodyObj.GetComponent<MeshRenderer>();
            Material bodyMat = new Material(Shader.Find("Standard"));
            bodyMr.material = bodyMat;
            
            // Remove child colliders (we'll use parent collider)
            DestroyImmediate(bodyObj.GetComponent<Collider>());

            // Create 3D head (pyramid-like using cube scaled)
            GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headObj.name = "Head";
            headObj.transform.SetParent(obj.transform);
            headObj.transform.localPosition = new Vector3(0, 0, 0);
            headObj.transform.localScale = new Vector3(0.5f, 0.3f, 0.4f);
            MeshRenderer headMr = headObj.GetComponent<MeshRenderer>();
            Material headMat = new Material(Shader.Find("Standard"));
            headMr.material = headMat;
            
            // Remove child colliders (we'll use parent collider)
            DestroyImmediate(headObj.GetComponent<Collider>());

            // Add 3D collider to parent (BoxCollider, not BoxCollider2D)
            BoxCollider col = obj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.8f, 1.2f, 0.5f);
            col.center = new Vector3(0, -0.2f, 0); // Center on the arrow
            col.isTrigger = false; // NOT a trigger - solid collider

            Debug.Log($"Arrow prefab collider: size={col.size}, center={col.center}, isTrigger={col.isTrigger}");

            // Assign colors
            Color[] colors = new Color[]
            {
                new Color(1f, 0.2f, 0.2f), new Color(0.2f, 0.5f, 1f),
                new Color(0.3f, 1f, 0.3f), new Color(1f, 0.9f, 0.2f),
                new Color(0.8f, 0.3f, 1f), new Color(1f, 0.6f, 0.2f)
            };

            SerializedObject so = new SerializedObject(arrow);
            so.FindProperty("bodyRenderer").objectReferenceValue = bodyMr;
            so.FindProperty("headRenderer").objectReferenceValue = headMr;
            
            SerializedProperty colorProp = so.FindProperty("colorDefinitions");
            colorProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                colorProp.GetArrayElementAtIndex(i).colorValue = colors[i];
            }
            so.ApplyModifiedProperties();

            // Save as prefab
            string path = "Assets/Prefabs/Arrow.prefab";
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            DestroyImmediate(obj);

            return AssetDatabase.LoadAssetAtPath<Arrow>(path);
        }

        private Slot CreateSlotPrefab3D()
        {
            // Create 3D Quad (or thin cube) for slot background
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = "Slot";
            obj.transform.localScale = new Vector3(1, 1, 1);

            Slot slot = obj.AddComponent<Slot>();
            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            
            // Create material
            Material mat = new Material(Shader.Find("Standard"));
            mr.material = mat;

            // Create TextMeshPro for ammo count (world space)
            GameObject textObj = new GameObject("AmmoText");
            textObj.transform.SetParent(obj.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.1f);
            textObj.transform.localScale = Vector3.one;
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            // Assign colors
            Color[] colors = new Color[]
            {
                new Color(1f, 0.2f, 0.2f), new Color(0.2f, 0.5f, 1f),
                new Color(0.3f, 1f, 0.3f), new Color(1f, 0.9f, 0.2f),
                new Color(0.8f, 0.3f, 1f), new Color(1f, 0.6f, 0.2f)
            };

            SerializedObject so = new SerializedObject(slot);
            so.FindProperty("bgMesh").objectReferenceValue = mr;
            so.FindProperty("ammoText").objectReferenceValue = tmp;
            
            SerializedProperty colorProp = so.FindProperty("colorDefinitions");
            colorProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                colorProp.GetArrayElementAtIndex(i).colorValue = colors[i];
            }
            so.ApplyModifiedProperties();

            // Save as prefab
            string path = "Assets/Prefabs/Slot.prefab";
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            DestroyImmediate(obj);

            return AssetDatabase.LoadAssetAtPath<Slot>(path);
        }

        private void SetupPortraitCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);

            Debug.Log("✓ Camera configured for portrait 3D view");
        }

        private void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
#endif
