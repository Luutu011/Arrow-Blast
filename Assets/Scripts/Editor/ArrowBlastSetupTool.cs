#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using ArrowBlast.Managers;
using ArrowBlast.Game;
using UnityEngine.UI;

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

        [MenuItem("Arrow Blast/Setup UI")]
        public static void SetupUI()
        {
            // 1. Create Canvas
            GameObject canvasObj = new GameObject("MainMenuCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            GameObject existingEventSystem = GameObject.Find("EventSystem");
            if (existingEventSystem == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 2. Main Menu Component
            var mainMenu = canvasObj.AddComponent<ArrowBlast.UI.MainMenu>();

            // 3. Main Panel
            GameObject mainPanel = CreatePanel("MainPanel", canvasObj.transform, new Color(0, 0, 0, 0.8f));
            
            // Title
            CreateText("Title", mainPanel.transform, "ARROW BLAST", 80, new Vector2(0, 300));

            // Buttons Container
            GameObject btnContainer = new GameObject("ButtonContainer");
            btnContainer.transform.SetParent(mainPanel.transform, false);
            var btnRt = btnContainer.AddComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(400, 500);
            var vlg = btnContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;

            Button startBtn = CreateButton("StartButton", btnContainer.transform, "PLAY", null);
            Button settingsBtn = CreateButton("SettingsButton", btnContainer.transform, "SETTINGS", null);
            Button exitBtn = CreateButton("ExitButton", btnContainer.transform, "EXIT", null);

            // 4. Level Panel
            GameObject levelPanel = CreatePanel("LevelPanel", canvasObj.transform, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            levelPanel.SetActive(false);
            CreateText("LevelTitle", levelPanel.transform, "SELECT LEVEL", 60, new Vector2(0, 400));
            
            GameObject levelGridObj = new GameObject("LevelGrid");
            levelGridObj.transform.SetParent(levelPanel.transform, false);
            var gridRt = levelGridObj.AddComponent<RectTransform>();
            gridRt.sizeDelta = new Vector2(600, 600);
            var glg = levelGridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(100, 100);
            glg.spacing = new Vector2(20, 20);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.childAlignment = TextAnchor.UpperCenter;

            Button backBtnLvl = CreateButton("BackButton", levelPanel.transform, "BACK", null);
            SetRect(backBtnLvl.GetComponent<RectTransform>(), new Vector2(0, -450), new Vector2(200, 60));

            // 5. Settings Panel
            GameObject settingsPanel = CreatePanel("SettingsPanel", canvasObj.transform, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            settingsPanel.SetActive(false);
            CreateText("SettingsTitle", settingsPanel.transform, "SETTINGS", 60, new Vector2(0, 400));
            CreateText("DummyText", settingsPanel.transform, "Music: ON\nSound: ON", 40, Vector2.zero);

            Button backBtnSet = CreateButton("BackButton", settingsPanel.transform, "BACK", null);
            SetRect(backBtnSet.GetComponent<RectTransform>(), new Vector2(0, -450), new Vector2(200, 60));

            // 6. Level Button Prefab (Generic button for grid)
            Button lvlBtnPrefab = CreateButton("LevelButtonPrefab", null, "1", null);
            lvlBtnPrefab.gameObject.SetActive(false); // Hide it, it's a prefab

            // Assign to MainMenu
            SerializedObject so = new SerializedObject(mainMenu);
            so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            so.FindProperty("levelPanel").objectReferenceValue = levelPanel;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            
            so.FindProperty("playButton").objectReferenceValue = startBtn;
            so.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            so.FindProperty("exitButton").objectReferenceValue = exitBtn;
            so.FindProperty("levelBackButton").objectReferenceValue = backBtnLvl;
            so.FindProperty("settingsBackButton").objectReferenceValue = backBtnSet;

            so.FindProperty("levelGrid").objectReferenceValue = gridRt;
            so.FindProperty("levelButtonPrefab").objectReferenceValue = lvlBtnPrefab;
            so.ApplyModifiedProperties();

            Debug.Log("✓ Arrow Blast UI Setup complete!");
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string content, int size, Vector2 pos)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            var rt = textObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(800, 200);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string label, System.Action onClick)
        {
            GameObject btnObj = new GameObject(name);
            if (parent != null) btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 80);
            
            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f);
            
            var btn = btnObj.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 32;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
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

            GUILayout.Space(10);
            if (GUILayout.Button("Setup Key & Lock Prefabs", GUILayout.Height(30)))
            {
                SetupObstacles();
            }
        }

        private void CreateCompleteSetup()
        {
            // Create GameManager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            LevelManager lm = gmObj.AddComponent<LevelManager>();
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
            so.FindProperty("levelManager").objectReferenceValue = lm;
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
            GameObject obj = new GameObject("Block");
            obj.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            RoundedCube roundedCube = obj.AddComponent<RoundedCube>();
            roundedCube.xSize = 10; roundedCube.ySize = 10; roundedCube.zSize = 10;
            roundedCube.roundness = 2f; // Increased for a softer look
            roundedCube.Generate();

            Block block = obj.AddComponent<Block>();
            obj.AddComponent<BoxCollider>().size = Vector3.one;

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

            // Create 3D body (elongated rounded cube)
            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(obj.transform);
            bodyObj.transform.localPosition = new Vector3(0, -0.4f, 0);
            bodyObj.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
            
            bodyObj.AddComponent<MeshFilter>();
            MeshRenderer bodyMr = bodyObj.AddComponent<MeshRenderer>();
            RoundedCube bodyRc = bodyObj.AddComponent<RoundedCube>();
            bodyRc.xSize = 5; bodyRc.ySize = 10; bodyRc.zSize = 5;
            bodyRc.roundness = 1.5f;
            bodyRc.Generate();
            
            Material bodyMat = new Material(Shader.Find("Standard"));
            bodyMr.material = bodyMat;

            // Create 3D head (shorter rounded cube)
            GameObject headObj = new GameObject("Head");
            headObj.name = "Head";
            headObj.transform.SetParent(obj.transform);
            headObj.transform.localPosition = new Vector3(0, 0, 0);
            headObj.transform.localScale = new Vector3(0.55f, 0.35f, 0.4f);
            
            headObj.AddComponent<MeshFilter>();
            MeshRenderer headMr = headObj.AddComponent<MeshRenderer>();
            RoundedCube headRc = headObj.AddComponent<RoundedCube>();
            headRc.xSize = 8; headRc.ySize = 5; headRc.zSize = 5;
            headRc.roundness = 1.2f;
            headRc.Generate();
            
            Material headMat = new Material(Shader.Find("Standard"));
            headMr.material = headMat;

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
            GameObject obj = new GameObject("Slot");
            obj.transform.localScale = new Vector3(1, 1, 1);

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            RoundedCube rc = obj.AddComponent<RoundedCube>();
            rc.xSize = 10; rc.ySize = 10; rc.zSize = 10;
            rc.roundness = 0.15f;
            rc.Generate();

            Slot slot = obj.AddComponent<Slot>();

            // Create material
            Material mat = new Material(Shader.Find("Standard"));
            mr.material = mat;

            // Create TextMeshPro for ammo count (world space)
            GameObject textObj = new GameObject("AmmoText");
            textObj.transform.SetParent(obj.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.6f); // Move in front of rounded surface
            textObj.transform.localScale = Vector3.one;
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;

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
            so.FindProperty("textColor").colorValue = Color.black;
            
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

        private void SetupObstacles()
        {
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm == null)
            {
                EditorUtility.DisplayDialog("Error", "GameManager not found in scene. Please run 'Create Complete 3D Setup' first or add a GameManager to your scene.", "OK");
                return;
            }

            KeyBlock keyPrefab = CreateKeyBlockPrefab3D();
            LockObstacle lockPrefab = CreateLockObstaclePrefab3D();

            SerializedObject so = new SerializedObject(gm);
            so.FindProperty("keyBlockPrefab").objectReferenceValue = keyPrefab;
            so.FindProperty("lockObstaclePrefab").objectReferenceValue = lockPrefab;
            so.ApplyModifiedProperties();

            Debug.Log("✓ Key and Lock prefabs setup and assigned to GameManager!");
            EditorUtility.DisplayDialog("Success", "Key and Lock prefabs created in Assets/Prefabs and assigned to GameManager.", "OK");
        }

        private KeyBlock CreateKeyBlockPrefab3D()
        {
            GameObject obj = new GameObject("KeyBlock");
            obj.transform.localScale = Vector3.one * 0.75f;

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            RoundedCube rc = obj.AddComponent<RoundedCube>();
            rc.xSize = 5; rc.ySize = 5; rc.zSize = 5;
            rc.roundness = 0.5f;
            rc.Generate();

            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.84f, 0f); // Gold
            mr.material = mat;

            obj.AddComponent<BoxCollider>();
            KeyBlock key = obj.AddComponent<KeyBlock>();

            SerializedObject so = new SerializedObject(key);
            so.FindProperty("meshRenderer").objectReferenceValue = mr;
            so.FindProperty("keyColor").colorValue = new Color(1f, 0.84f, 0f);
            so.ApplyModifiedProperties();

            string path = "Assets/Prefabs/KeyBlock.prefab";
            EnsureDirectory("Assets/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            DestroyImmediate(obj);

            return AssetDatabase.LoadAssetAtPath<KeyBlock>(path);
        }

        private LockObstacle CreateLockObstaclePrefab3D()
        {
            GameObject obj = new GameObject("LockObstacle");
            obj.transform.localScale = Vector3.one;

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            RoundedCube rc = obj.AddComponent<RoundedCube>();
            rc.xSize = 5; rc.ySize = 5; rc.zSize = 5;
            rc.roundness = 0.5f;
            rc.Generate();

            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.3f, 0.3f, 0.3f, 0.6f); // Dark gray semi-transparent
            
            // Setup for transparency
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            mr.material = mat;

            obj.AddComponent<BoxCollider>().isTrigger = false;
            LockObstacle lockObs = obj.AddComponent<LockObstacle>();

            SerializedObject so = new SerializedObject(lockObs);
            so.FindProperty("meshRenderer").objectReferenceValue = mr;
            so.FindProperty("lockedColor").colorValue = new Color(0.3f, 0.3f, 0.3f, 0.6f);
            so.FindProperty("unlockingColor").colorValue = new Color(0.8f, 0.8f, 0.2f, 1f);
            so.ApplyModifiedProperties();

            string path = "Assets/Prefabs/LockObstacle.prefab";
            EnsureDirectory("Assets/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            DestroyImmediate(obj);

            return AssetDatabase.LoadAssetAtPath<LockObstacle>(path);
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
