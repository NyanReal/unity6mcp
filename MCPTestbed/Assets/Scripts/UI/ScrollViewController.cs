using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScrollViewController : MonoBehaviour
{
    public GameObject gridItemPrefab;
    public Transform contentContainer;
    public Button closeButton;
    public GameObject panel; 

    public List<Sprite> availableIcons;

    private void Start()
    {
        Debug.Log("[GProScroll] Start called.");
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
        
        // Auto-load icons if empty in editor
#if UNITY_EDITOR
        if (availableIcons == null || availableIcons.Count == 0)
        {
             Debug.Log("[GProScroll] availableIcons is empty in Start. Attempting to load via System.IO.");
             string folderPath = "Assets/UI/Sprites/cookicons";
             if (System.IO.Directory.Exists(folderPath))
             {
                 availableIcons = new List<Sprite>();
                 string[] files = System.IO.Directory.GetFiles(folderPath, "*.png");
                 Debug.Log($"[GProScroll] Found {files.Length} files in {folderPath}");
                 
                 foreach (string filePath in files)
                 {
                     string assetPath = filePath.Replace("\\", "/");
                     // Ensure path starts with Assets/
                     if (assetPath.StartsWith(Application.dataPath))
                     {
                        assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                     }
                     
                     // Force import settings to Sprite and SINGLE mode
                     TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                     if (importer != null)
                     {
                        bool changed = false;
                        if (importer.textureType != TextureImporterType.Sprite)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            changed = true;
                        }
                        // Force Single mode (1) because Multiple (2) returns null if no sprites are defined
                        if (importer.spriteImportMode != SpriteImportMode.Single)
                        {
                            importer.spriteImportMode = SpriteImportMode.Single;
                            changed = true;
                        }

                        if (changed) importer.SaveAndReimport();
                     }

                     Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                     if (sprite != null) 
                     {
                        availableIcons.Add(sprite);
                     }
                     else
                     {
                        Debug.LogWarning($"[GProScroll] Failed to load sprite at: {assetPath} (Original: {filePath})");
                     }
                 }
                 Debug.Log($"[GProScroll] Loaded {availableIcons.Count} icons via System.IO in Start.");
             }
             else
             {
                 Debug.LogWarning($"[GProScroll] Directory not found: {folderPath}");
             }
        }
#endif

        Open();
    }

    public void Open()
    {
        Debug.Log("[GProScroll] Open called.");
        if (panel != null) panel.SetActive(true);
        GenerateGrid();
    }

    public void Close()
    {
        Debug.Log("[GProScroll] Close called.");
        if (panel != null) panel.SetActive(false);
    }

    private void GenerateGrid()
    {
        Debug.Log("[GProScroll] GenerateGrid called.");
        
        if (contentContainer == null) 
        {
            Debug.LogError("[GProScroll] contentContainer is NULL!");
            return;
        }

        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        if (availableIcons == null || availableIcons.Count == 0)
        {
            Debug.LogWarning("[GProScroll] No icons available to display in GenerateGrid.");
            return;
        }

        Debug.Log($"[GProScroll] Generating grid with {availableIcons.Count} icons.");

        if (gridItemPrefab == null)
        {
            Debug.LogError("[GProScroll] GridItemPrefab is NULL!");
            return;
        }

        foreach (var icon in availableIcons)
        {
            GameObject obj = Instantiate(gridItemPrefab, contentContainer);
            // Ensure scale is 1
            obj.transform.localScale = Vector3.one; 
            
            GridItemView view = obj.GetComponent<GridItemView>();
            if (view != null)
            {
                int quantity = Random.Range(1, 100);
                view.SetData(icon, quantity);
            }
            else
            {
                Debug.LogError("[GProScroll] GridItemView component missing on instantiated object.");
            }
        }
    }
}