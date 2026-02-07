using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class SceneSetup
{
    [MenuItem("Tools/Setup ScrollView Scene")]
    public static void Setup()
    {
        // Open Scene
        EditorSceneManager.OpenScene("Assets/Scenes/TestScrollviewGPro.unity");

        // Cleanup if rerunning
        GameObject existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null) Object.DestroyImmediate(existingCanvas);
        GameObject existingEventSystem = GameObject.Find("EventSystem");
        if (existingEventSystem != null) Object.DestroyImmediate(existingEventSystem);


        // Create Canvas & EventSystem
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem").AddComponent<UnityEngine.EventSystems.EventSystem>().gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Create Panel
        GameObject panel = new GameObject("ScrollViewPanel");
        panel.transform.SetParent(canvasGO.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.5f);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        // Create Close Button
        GameObject closeBtn = new GameObject("CloseButton");
        closeBtn.transform.SetParent(panel.transform, false);
        Image closeImg = closeBtn.AddComponent<Image>();
        closeImg.color = Color.red;
        Button closeButton = closeBtn.AddComponent<Button>();
        RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.5f, 0);
        closeRT.anchorMax = new Vector2(0.5f, 0);
        closeRT.pivot = new Vector2(0.5f, 0);
        closeRT.anchoredPosition = new Vector2(0, 50);
        closeRT.sizeDelta = new Vector2(160, 50);

        GameObject closeTextGO = new GameObject("Text");
        closeTextGO.transform.SetParent(closeBtn.transform, false);
        Text closeText = closeTextGO.AddComponent<Text>();
        closeText.text = "Close";
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeText.fontSize = 24;
        closeText.color = Color.white;
        RectTransform closeTextRT = closeTextGO.GetComponent<RectTransform>();
        closeTextRT.anchorMin = Vector2.zero;
        closeTextRT.anchorMax = Vector2.one;
        closeTextRT.offsetMin = Vector2.zero;
        closeTextRT.offsetMax = Vector2.zero;

        // Create Scroll View
        GameObject scrollView = new GameObject("ScrollView", typeof(RectTransform));
        scrollView.transform.SetParent(panel.transform, false);
        RectTransform scrollRT = scrollView.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.1f, 0.2f);
        scrollRT.anchorMax = new Vector2(0.9f, 0.9f); // Leave room for close button
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        
        Image scrollImage = scrollView.AddComponent<Image>();
        scrollImage.color = new Color(1, 1, 1, 0.39f); 
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewRT = viewport.AddComponent<RectTransform>();
        viewRT.anchorMin = Vector2.zero;
        viewRT.anchorMax = Vector2.one;
        viewRT.sizeDelta = Vector2.zero;
        viewRT.pivot = new Vector2(0, 1);
        viewport.AddComponent<Image>(); 
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 300); // Height managed by fitter

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200, 200);
        grid.spacing = new Vector2(20, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter; // Center grid

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        scrollRect.viewport = viewRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // Create Grid Item Prefab
        GameObject itemGO = new GameObject("GridItem");
        Image itemImg = itemGO.AddComponent<Image>(); // Thumbnail holder
        GridItemView itemView = itemGO.AddComponent<GridItemView>();
        itemView.iconImage = itemImg; // Self is icon

        GameObject qtyGO = new GameObject("QuantityText");
        qtyGO.transform.SetParent(itemGO.transform, false);
        Text qtyText = qtyGO.AddComponent<Text>();
        qtyText.text = "99";
        qtyText.alignment = TextAnchor.LowerRight;
        qtyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        qtyText.fontSize = 48;
        qtyText.color = Color.white;
        RectTransform qtyRT = qtyGO.GetComponent<RectTransform>();
        qtyRT.anchorMin = Vector2.zero;
        qtyRT.anchorMax = Vector2.one;
        qtyRT.offsetMin = new Vector2(5, 5);
        qtyRT.offsetMax = new Vector2(-5, -5); // Padding

        itemView.quantityText = qtyText;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(itemGO, "Assets/Prefabs/GridItem.prefab");
        Object.DestroyImmediate(itemGO);

        // Setup Controller
        ScrollViewController controller = panel.AddComponent<ScrollViewController>();
        controller.gridItemPrefab = prefab;
        controller.contentContainer = content.transform;
        controller.closeButton = closeButton;
        controller.panel = panel;

        // Auto-populate icons from cookicons folder
        // Auto-populate icons from cookicons folder
        string folderPath = "Assets/UI/Sprites/cookicons";
        if (System.IO.Directory.Exists(folderPath))
        {
             string[] files = System.IO.Directory.GetFiles(folderPath, "*.png");
             if (files.Length > 0)
             {
                 controller.availableIcons = new System.Collections.Generic.List<Sprite>();
                 foreach (string filePath in files)
                 {
                     string assetPath = filePath.Replace("\\", "/");
                     // Force import settings to Sprite if needed
                     TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                     if (importer != null && importer.textureType != TextureImporterType.Sprite)
                     {
                         importer.textureType = TextureImporterType.Sprite;
                         importer.SaveAndReimport();
                     }

                     Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                     if (sprite != null) 
                     {
                         controller.availableIcons.Add(sprite);
                     }
                 }
                 Debug.Log($"Loaded {controller.availableIcons.Count} icons directly from content.");
             }
             else
             {
                 Debug.LogError($"No .png files found in {folderPath}");
             }
        }
        else
        {
             Debug.LogError($"Directory not found: {folderPath}");
        }

        if (prefab == null) Debug.LogError("Failed to create prefab!");
        else Debug.Log("Prefab created successfully.");

        // Ensure Camera exists
        if (Object.FindObjectOfType<Camera>() == null)
        {
            GameObject cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
            cam.transform.position = new Vector3(0, 0, -10);
        }

        // Mark controller as dirty to ensure list is saved
        EditorUtility.SetDirty(controller);

        // Save Scene
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.Refresh();
    }
}