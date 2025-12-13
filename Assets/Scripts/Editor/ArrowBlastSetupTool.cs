using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class ArrowBlastSetupTool : EditorWindow
{
    private bool createPrefabsFolder = true;
    private bool setupCamera = true;
    
    [MenuItem("Tools/Arrow Blast/Setup Game Scene")]
    public static void ShowWindow()
    {
        GetWindow<ArrowBlastSetupTool>("Arrow Blast Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Arrow Blast Game Setup (3D)", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("This tool will automatically create all necessary GameObjects, Prefabs, and configure your 3D scene.", MessageType.Info);
        EditorGUILayout.Space();
        
        createPrefabsFolder = EditorGUILayout.Toggle("Create Prefabs Folder", createPrefabsFolder);
        setupCamera = EditorGUILayout.Toggle("Setup Camera", setupCamera);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🚀 Setup Everything", GUILayout.Height(40)))
        {
            SetupGame();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create Game Canvas Only", GUILayout.Height(30)))
        {
            CreateGameCanvas();
        }
        
        if (GUILayout.Button("Create GameManager Only", GUILayout.Height(30)))
        {
            CreateGameManager();
        }
        
        if (GUILayout.Button("Create All Prefabs", GUILayout.Height(30)))
        {
            CreateAllPrefabs();
        }
    }

    void SetupGame()
    {
        if (!EditorUtility.DisplayDialog("Setup Arrow Blast", 
            "This will create all GameObjects and Prefabs in the current scene. Continue?", 
            "Yes", "Cancel"))
        {
            return;
        }

        // Create folders
        if (createPrefabsFolder)
        {
            CreateFolders();
        }

        // Setup scene objects
        GameObject gameManager = CreateGameManager();
        CreateContainers(gameManager);
        CreateGameCanvas();

        // Setup camera
        if (setupCamera)
        {
            SetupMainCamera();
        }

        // Create prefabs
        CreateAllPrefabs();

        // Auto-assign prefabs to GameManager
        AutoAssignPrefabs(gameManager);

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("✅ Arrow Blast setup complete! Press Play to test.");
        EditorUtility.DisplayDialog("Success!", "Scene setup complete! Check the Hierarchy and Prefabs folder.", "OK");
    }

    void CreateFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels"))
            AssetDatabase.CreateFolder("Assets/Resources", "Levels");

        Debug.Log("📁 Folders created");
    }

    GameObject CreateGameManager()
    {
        // Check if already exists
        GameObject existing = GameObject.Find("GameManager");
        if (existing != null)
        {
            if (EditorUtility.DisplayDialog("GameManager Exists", 
                "GameManager already exists. Replace it?", "Yes", "No"))
            {
                DestroyImmediate(existing);
            }
            else
            {
                return existing;
            }
        }

        GameObject gmGO = new GameObject("GameManager");
        gmGO.AddComponent<ArrowBlast.Managers.GameManager>();
        gmGO.AddComponent<ArrowBlast.Managers.LevelGenerator>();

        Debug.Log("🎮 GameManager created");
        return gmGO;
    }

    void CreateContainers(GameObject gameManager)
    {
        // Wall Container
        GameObject wallContainer = CreateOrFind("WallContainer");
        wallContainer.transform.position = new Vector3(0, 2, 0);

        // Puzzle Container
        GameObject puzzleContainer = CreateOrFind("PuzzleContainer");
        puzzleContainer.transform.position = new Vector3(0, -3, 0);

        // Assign to GameManager
        if (gameManager != null)
        {
            ArrowBlast.Managers.GameManager gm = gameManager.GetComponent<ArrowBlast.Managers.GameManager>();
            if (gm != null)
            {
                SerializedObject serializedGM = new SerializedObject(gm);
                serializedGM.FindProperty("wallContainer").objectReferenceValue = wallContainer.transform;
                serializedGM.FindProperty("puzzleContainer").objectReferenceValue = puzzleContainer.transform;
                serializedGM.FindProperty("cellSize").floatValue = 1f;
                serializedGM.FindProperty("fireRate").floatValue = 0.2f;
                
                // Assign LevelGenerator reference
                ArrowBlast.Managers.LevelGenerator levelGen = gameManager.GetComponent<ArrowBlast.Managers.LevelGenerator>();
                serializedGM.FindProperty("levelGenerator").objectReferenceValue = levelGen;
                
                serializedGM.ApplyModifiedProperties();
            }
        }

        Debug.Log("📦 Containers created and assigned");
    }

    void CreateGameCanvas()
    {
        // Check if Canvas already exists
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null)
        {
            Debug.Log("⚠️ Canvas already exists: " + existingCanvas.name);
            return;
        }

        // Create Canvas
        GameObject canvasGO = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        
        // Configure Canvas
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Configure Canvas Scaler
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Create Slots Container as child of Canvas
        GameObject slotsContainer = new GameObject("SlotsContainer");
        slotsContainer.transform.SetParent(canvasGO.transform, false);
        
        RectTransform slotsRect = slotsContainer.AddComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0.5f, 0f);
        slotsRect.anchorMax = new Vector2(0.5f, 0f);
        slotsRect.pivot = new Vector2(0.5f, 0f);
        slotsRect.anchoredPosition = new Vector2(0, 100);
        slotsRect.sizeDelta = new Vector2(800, 200);

        // Add Horizontal Layout Group
        HorizontalLayoutGroup layoutGroup = slotsContainer.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 20;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        // Assign to GameManager
        GameObject gmGO = GameObject.Find("GameManager");
        if (gmGO != null)
        {
            ArrowBlast.Managers.GameManager gm = gmGO.GetComponent<ArrowBlast.Managers.GameManager>();
            if (gm != null)
            {
                SerializedObject serializedGM = new SerializedObject(gm);
                serializedGM.FindProperty("slotsContainer").objectReferenceValue = slotsContainer.transform;
                serializedGM.ApplyModifiedProperties();
            }
        }

        Debug.Log("🎨 Game Canvas created with proper scaling (1080x1920)");
    }

    void SetupMainCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("⚠️ Main Camera not found");
            return;
        }

        // 3D Camera settings
        mainCam.orthographic = false;
        mainCam.transform.position = new Vector3(3, 5, -15);
        mainCam.transform.rotation = Quaternion.Euler(20, 0, 0);

        Debug.Log("📷 Camera configured for 3D");
    }

    void CreateAllPrefabs()
    {
        CreateBlockPrefab();
        CreateArrowPrefab();
        CreateSlotPrefab();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void CreateBlockPrefab()
    {
        // Create 3D Cube
        GameObject blockGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockGO.name = "Block";
        blockGO.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        // Create material for color support
        Material blockMat = new Material(Shader.Find("Standard"));
        blockGO.GetComponent<MeshRenderer>().material = blockMat;

        // Add Block script
        ArrowBlast.Game.Block blockScript = blockGO.AddComponent<ArrowBlast.Game.Block>();
        
        // Set up colors using SerializedObject
        SerializedObject serializedBlock = new SerializedObject(blockScript);
        SerializedProperty colorsProp = serializedBlock.FindProperty("colorDefinitions");
        colorsProp.arraySize = 6;
        
        colorsProp.GetArrayElementAtIndex(0).colorValue = new Color(1f, 0f, 0f); // Red
        colorsProp.GetArrayElementAtIndex(1).colorValue = new Color(0f, 0f, 1f); // Blue
        colorsProp.GetArrayElementAtIndex(2).colorValue = new Color(0f, 1f, 0f); // Green
        colorsProp.GetArrayElementAtIndex(3).colorValue = new Color(1f, 1f, 0f); // Yellow
        colorsProp.GetArrayElementAtIndex(4).colorValue = new Color(1f, 0f, 1f); // Purple
        colorsProp.GetArrayElementAtIndex(5).colorValue = new Color(1f, 0.65f, 0f); // Orange
        
        // Assign mesh renderer (3D)
        serializedBlock.FindProperty("meshRenderer").objectReferenceValue = blockGO.GetComponent<MeshRenderer>();
        
        serializedBlock.ApplyModifiedProperties();

        // Save as prefab
        string prefabPath = "Assets/Prefabs/Block.prefab";
        PrefabUtility.SaveAsPrefabAsset(blockGO, prefabPath);
        DestroyImmediate(blockGO);

        Debug.Log("🟦 Block Prefab created (3D Cube)");
    }

    void CreateArrowPrefab()
    {
        // Create parent
        GameObject arrowGO = new GameObject("Arrow");
        
        // Create Head (3D sprite for this game)
        GameObject head = new GameObject("Head");
        head.transform.SetParent(arrowGO.transform);
        head.transform.localPosition = Vector3.zero;
        head.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        
        SpriteRenderer headRenderer = head.AddComponent<SpriteRenderer>();
        headRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        headRenderer.color = Color.white;

        // Create Body
        GameObject body = new GameObject("Body");
        body.transform.SetParent(arrowGO.transform);
        body.transform.localPosition = new Vector3(0, -0.5f, 0);
        body.transform.localScale = new Vector3(0.2f, 1f, 1f);
        
        SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bodyRenderer.color = Color.white;

        // Add collider to parent (3D)
        BoxCollider collider = arrowGO.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.5f, 1f, 0.1f);

        // Add Arrow script
        ArrowBlast.Game.Arrow arrowScript = arrowGO.AddComponent<ArrowBlast.Game.Arrow>();
        
        // Configure using SerializedObject
        SerializedObject serializedArrow = new SerializedObject(arrowScript);
        
        serializedArrow.FindProperty("headRenderer").objectReferenceValue = headRenderer;
        serializedArrow.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
        
        SerializedProperty colorsProp = serializedArrow.FindProperty("colorDefinitions");
        colorsProp.arraySize = 6;
        
        colorsProp.GetArrayElementAtIndex(0).colorValue = new Color(1f, 0f, 0f);
        colorsProp.GetArrayElementAtIndex(1).colorValue = new Color(0f, 0f, 1f);
        colorsProp.GetArrayElementAtIndex(2).colorValue = new Color(0f, 1f, 0f);
        colorsProp.GetArrayElementAtIndex(3).colorValue = new Color(1f, 1f, 0f);
        colorsProp.GetArrayElementAtIndex(4).colorValue = new Color(1f, 0f, 1f);
        colorsProp.GetArrayElementAtIndex(5).colorValue = new Color(1f, 0.65f, 0f);
        
        serializedArrow.ApplyModifiedProperties();

        // Save as prefab
        string prefabPath = "Assets/Prefabs/Arrow.prefab";
        PrefabUtility.SaveAsPrefabAsset(arrowGO, prefabPath);
        DestroyImmediate(arrowGO);

        Debug.Log("➡️ Arrow Prefab created (3D with Sprites)");
    }

    void CreateSlotPrefab()
    {
        // Create slot as UI Image
        GameObject slotGO = new GameObject("SlotUI");
        RectTransform rectTransform = slotGO.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(120, 150);

        // Add Image
        Image bgImage = slotGO.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        // Create Ammo Text
        GameObject textGO = new GameObject("AmmoText");
        textGO.transform.SetParent(slotGO.transform);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0);
        textRect.pivot = new Vector2(0.5f, 0);
        textRect.anchoredPosition = new Vector2(0, 10);
        textRect.sizeDelta = new Vector2(-20, 30);

        TextMeshProUGUI ammoText = textGO.AddComponent<TextMeshProUGUI>();
        ammoText.text = "0";
        ammoText.fontSize = 24;
        ammoText.alignment = TextAlignmentOptions.Center;
        ammoText.color = Color.white;

        // Add Slot script
        ArrowBlast.Game.Slot slotScript = slotGO.AddComponent<ArrowBlast.Game.Slot>();
        
        SerializedObject serializedSlot = new SerializedObject(slotScript);
        serializedSlot.FindProperty("bgImage").objectReferenceValue = bgImage;
        serializedSlot.FindProperty("ammoText").objectReferenceValue = ammoText;
        
        SerializedProperty colorsProp = serializedSlot.FindProperty("colorDefinitions");
        colorsProp.arraySize = 6;
        
        colorsProp.GetArrayElementAtIndex(0).colorValue = new Color(1f, 0f, 0f);
        colorsProp.GetArrayElementAtIndex(1).colorValue = new Color(0f, 0f, 1f);
        colorsProp.GetArrayElementAtIndex(2).colorValue = new Color(0f, 1f, 0f);
        colorsProp.GetArrayElementAtIndex(3).colorValue = new Color(1f, 1f, 0f);
        colorsProp.GetArrayElementAtIndex(4).colorValue = new Color(1f, 0f, 1f);
        colorsProp.GetArrayElementAtIndex(5).colorValue = new Color(1f, 0.65f, 0f);
        
        serializedSlot.ApplyModifiedProperties();

        // Save as prefab
        string prefabPath = "Assets/Prefabs/SlotUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(slotGO, prefabPath);
        DestroyImmediate(slotGO);

        Debug.Log("🎰 Slot Prefab created");
    }

    void AutoAssignPrefabs(GameObject gameManager)
    {
        if (gameManager == null) return;

        ArrowBlast.Managers.GameManager gm = gameManager.GetComponent<ArrowBlast.Managers.GameManager>();
        if (gm == null) return;

        // Load prefabs
        GameObject blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Block.prefab");
        GameObject arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Arrow.prefab");
        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SlotUI.prefab");

        // Assign via SerializedObject
        SerializedObject serializedGM = new SerializedObject(gm);
        serializedGM.FindProperty("blockPrefab").objectReferenceValue = blockPrefab;
        serializedGM.FindProperty("arrowPrefab").objectReferenceValue = arrowPrefab;
        serializedGM.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        serializedGM.ApplyModifiedProperties();

        Debug.Log("🔗 Prefabs auto-assigned to GameManager");
    }

    GameObject CreateOrFind(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
        }
        return go;
    }
}
