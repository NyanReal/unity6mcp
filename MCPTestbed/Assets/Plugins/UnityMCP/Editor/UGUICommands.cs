using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UnityMCP
{
    public static class UGUICommands
    {
        private static Dictionary<string, GameObject> _loadedPrefabs = new Dictionary<string, GameObject>();

        public static string Execute(string command, string argsJson)
        {
            var args = string.IsNullOrEmpty(argsJson) ? new Dictionary<string, string>() : ParseArgs(argsJson);

            switch (command)
            {
                case "create_ui_prefab":
                    return CreateUIPrefab(args);
                case "add_ui_element":
                    return AddUIElement(args);
                case "set_rect_transform":
                    return SetRectTransform(args);
                case "set_ui_property":
                    return SetUIProperty(args);
                case "read_ui_hierarchy":
                    return ReadUIHierarchy(args);
                case "delete_ui_element":
                    return DeleteUIElement(args);
                case "save_prefab":
                    return SavePrefab(args);
                case "add_component":
                    return AddComponent(args);
                case "bind_reference":
                    return BindReference(args);
                case "set_component_property":
                    return SetComponentProperty(args);
                case "list_components":
                    return ListComponents(args);
                default:
                    return $"{{\"error\":\"Unknown command: {command}\"}}";
            }
        }

        private static Dictionary<string, string> ParseArgs(string json)
        {
            var result = new Dictionary<string, string>();
            // Simple JSON parsing for flat objects
            json = json.Trim().TrimStart('{').TrimEnd('}');
            var pairs = json.Split(',');
            foreach (var pair in pairs)
            {
                var kv = pair.Split(new[] { ':' }, 2);
                if (kv.Length == 2)
                {
                    var key = kv[0].Trim().Trim('"');
                    var value = kv[1].Trim().Trim('"');
                    result[key] = value;
                }
            }
            return result;
        }

        // ===== UI Layout Commands =====

        private static string CreateUIPrefab(Dictionary<string, string> args)
        {
            string name = args.GetValueOrDefault("name", "NewUI");
            string path = args.GetValueOrDefault("path", "Assets/UI");

            // Create Canvas GameObject
            var canvasGO = new GameObject(name);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(path))
            {
                CreateFolderRecursive(path);
            }

            // Save as prefab
            string prefabPath = $"{path}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
            UnityEngine.Object.DestroyImmediate(canvasGO);

            // Load and cache the prefab instance
            var instance = PrefabUtility.LoadPrefabContents(prefabPath);
            _loadedPrefabs[prefabPath] = instance;

            return $"{{\"success\":true,\"prefab\":\"{prefabPath}\"}}";
        }

        private static string AddUIElement(Dictionary<string, string> args)
        {
            string prefabPath = args.GetValueOrDefault("prefab", "");
            string type = args.GetValueOrDefault("type", "Panel");
            string name = args.GetValueOrDefault("name", type);
            string parent = args.GetValueOrDefault("parent", "");

            var prefabRoot = GetOrLoadPrefab(prefabPath);
            if (prefabRoot == null) return "{\"error\":\"Prefab not found\"}";

            Transform parentTransform = string.IsNullOrEmpty(parent)
                ? prefabRoot.transform
                : FindChild(prefabRoot.transform, parent);

            if (parentTransform == null) return $"{{\"error\":\"Parent '{parent}' not found\"}}";

            GameObject element = new GameObject(name);
            element.transform.SetParent(parentTransform, false);

            var rectTransform = element.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            switch (type.ToLower())
            {
                case "panel":
                case "image":
                    var img = element.AddComponent<Image>();
                    img.color = type.ToLower() == "panel" ? new Color(0, 0, 0, 0.5f) : Color.white;
                    break;
                case "button":
                    var btnImg = element.AddComponent<Image>();
                    var btn = element.AddComponent<Button>();
                    btn.targetGraphic = btnImg;
                    // Add text child
                    var textGO = new GameObject("Text");
                    textGO.transform.SetParent(element.transform, false);
                    var textRect = textGO.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    var text = textGO.AddComponent<Text>();
                    text.text = "Button";
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = Color.black;
                    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    break;
                case "text":
                    var txt = element.AddComponent<Text>();
                    txt.text = "Text";
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.color = Color.black;
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    break;
                case "rawimage":
                    element.AddComponent<RawImage>();
                    break;
                case "scrollview":
                    CreateScrollView(element);
                    break;
                case "inputfield":
                    CreateInputField(element);
                    break;
            }

            return $"{{\"success\":true,\"element\":\"{name}\"}}";
        }

        private static string SetRectTransform(Dictionary<string, string> args)
        {
            string prefabPath = args.GetValueOrDefault("prefab", "");
            string elementName = args.GetValueOrDefault("element", "");
            
            var prefabRoot = GetOrLoadPrefab(prefabPath);
            if (prefabRoot == null) return "{\"error\":\"Prefab not found\"}";

            var element = FindChild(prefabRoot.transform, elementName);
            if (element == null) return $"{{\"error\":\"Element '{elementName}' not found\"}}";

            var rect = element.GetComponent<RectTransform>();
            if (rect == null) return "{\"error\":\"RectTransform not found\"}";

            // Position
            if (args.TryGetValue("position", out string posStr))
            {
                var pos = ParseVector2(posStr);
                rect.anchoredPosition = pos;
            }

            // Size
            if (args.TryGetValue("size", out string sizeStr))
            {
                var size = ParseVector2(sizeStr);
                rect.sizeDelta = size;
            }

            // Anchors preset
            if (args.TryGetValue("anchors", out string anchorsStr))
            {
                ApplyAnchorPreset(rect, anchorsStr);
            }

            // Alignment/Pivot
            if (args.TryGetValue("pivot", out string pivotStr))
            {
                rect.pivot = ParseVector2(pivotStr);
            }

            return "{\"success\":true}";
        }

        private static string SetUIProperty(Dictionary<string, string> args)
        {
            string prefabPath = args.GetValueOrDefault("prefab", "");
            string elementName = args.GetValueOrDefault("element", "");
            string property = args.GetValueOrDefault("property", "");
            string value = args.GetValueOrDefault("value", "");

            var prefabRoot = GetOrLoadPrefab(prefabPath);
            if (prefabRoot == null) return "{\"error\":\"Prefab not found\"}";

            var element = FindChild(prefabRoot.transform, elementName);
            if (element == null) return $"{{\"error\":\"Element '{elementName}' not found\"}}";

            switch (property.ToLower())
            {
                case "text":
                    var text = element.GetComponent<Text>();
                    if (text != null) text.text = value;
                    break;
                case "color":
                    var graphic = element.GetComponent<Graphic>();
                    if (graphic != null) graphic.color = ParseColor(value);
                    break;
                case "fontsize":
                    var txtSize = element.GetComponent<Text>();
                    if (txtSize != null && int.TryParse(value, out int size)) txtSize.fontSize = size;
                    break;
                case "sprite":
                    var img = element.GetComponent<Image>();
                    if (img != null)
                    {
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(value);
                        if (sprite != null) img.sprite = sprite;
                    }
                    break;
                case "enabled":
                    element.gameObject.SetActive(value.ToLower() == "true");
                    break;
            }

            return "{\"success\":true}";
        }

        private static string ReadUIHierarchy(Dictionary<string, string> args)
        {
            string prefabPath = args.GetValueOrDefault("prefab", "");
            
            var prefabRoot = GetOrLoadPrefab(prefabPath);
            if (prefabRoot == null) return "{\"error\":\"Prefab not found\"}";

            var hierarchy = BuildHierarchy(prefabRoot.transform);
            return hierarchy;
        }

        private static string DeleteUIElement(Dictionary<string, string> args)
        {
            string prefabPath = args.GetValueOrDefault("prefab", "");
            string elementName = args.GetValueOrDefault("element", "");

            var prefabRoot = GetOrLoadPrefab(prefabPath);
            if (prefabRoot == null) return "{\"error\":\"Prefab not found\"}";

            var element = FindChild(prefabRoot.transform, elementName);
            if (element == null) return $"{{\"error\":\"Element '{elementName}' not found\"}}";

            UnityEngine.Object.DestroyImmediate(element.gameObject);
            return "{\"success\":true}";
        }

        private static string SavePrefab(Dictionary<string, string> args)
        {
            string prefabPath = args.GetValueOrDefault("prefab", "");

            if (!_loadedPrefabs.TryGetValue(prefabPath, out var instance))
            {
                return "{\"error\":\"Prefab not loaded\"}";
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            PrefabUtility.UnloadPrefabContents(instance);
            _loadedPrefabs.Remove(prefabPath);

            AssetDatabase.Refresh();
            return "{\"success\":true}";
        }

        // ===== Component Commands =====

        private static string AddComponent(Dictionary<string, string> args)
        {
            string prefabPath = args.GetValueOrDefault("prefab", "");
            string elementName = args.GetValueOrDefault("element", "");
            string scriptName = args.GetValueOrDefault("script_name", "");

            var prefabRoot = GetOrLoadPrefab(prefabPath);
            if (prefabRoot == null) return "{\"error\":\"Prefab not found\"}";

            Transform element = string.IsNullOrEmpty(elementName)
                ? prefabRoot.transform
                : FindChild(prefabRoot.transform, elementName);

            if (element == null) return $"{{\"error\":\"Element '{elementName}' not found\"}}";

            // Find script type
            Type scriptType = FindType(scriptName);
            if (scriptType == null) return $"{{\"error\":\"Script '{scriptName}' not found\"}}";

            element.gameObject.AddComponent(scriptType);
            return $"{{\"success\":true,\"component\":\"{scriptName}\"}}";
        }

        private static string BindReference(Dictionary<string, string> args)
        {
            string prefabPath = args.GetValueOrDefault("prefab", "");
            string elementName = args.GetValueOrDefault("element", "");
            string fieldName = args.GetValueOrDefault("field_name", "");
            string targetElement = args.GetValueOrDefault("target_element", "");
            string componentType = args.GetValueOrDefault("component_type", "");

            var prefabRoot = GetOrLoadPrefab(prefabPath);
            if (prefabRoot == null) return "{\"error\":\"Prefab not found\"}";

            Transform element = string.IsNullOrEmpty(elementName)
                ? prefabRoot.transform
                : FindChild(prefabRoot.transform, elementName);
            if (element == null) return $"{{\"error\":\"Element '{elementName}' not found\"}}";

            var target = FindChild(prefabRoot.transform, targetElement);
            if (target == null) return $"{{\"error\":\"Target '{targetElement}' not found\"}}";

            // Get all components and find the one with the field
            var components = element.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                var so = new SerializedObject(comp);
                var prop = so.FindProperty(fieldName);
                if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    // Determine what to assign
                    if (!string.IsNullOrEmpty(componentType))
                    {
                        var targetType = FindType(componentType);
                        if (targetType != null)
                        {
                            var targetComp = target.GetComponent(targetType);
                            prop.objectReferenceValue = targetComp;
                        }
                    }
                    else
                    {
                        // Try common UI types
                        var uiComp = target.GetComponent<Graphic>() ??
                                     target.GetComponent<Button>() as Component ??
                                     target.GetComponent<Text>() as Component ??
                                     target.gameObject as UnityEngine.Object;
                        prop.objectReferenceValue = uiComp;
                    }

                    so.ApplyModifiedProperties();
                    return "{\"success\":true}";
                }
            }

            return $"{{\"error\":\"Field '{fieldName}' not found on any component\"}}";
        }

        private static string SetComponentProperty(Dictionary<string, string> args)
        {
            string prefabPath = args.GetValueOrDefault("prefab", "");
            string elementName = args.GetValueOrDefault("element", "");
            string componentName = args.GetValueOrDefault("component", "");
            string property = args.GetValueOrDefault("property", "");
            string value = args.GetValueOrDefault("value", "");

            var prefabRoot = GetOrLoadPrefab(prefabPath);
            if (prefabRoot == null) return "{\"error\":\"Prefab not found\"}";

            Transform element = string.IsNullOrEmpty(elementName)
                ? prefabRoot.transform
                : FindChild(prefabRoot.transform, elementName);
            if (element == null) return $"{{\"error\":\"Element '{elementName}' not found\"}}";

            var compType = FindType(componentName);
            if (compType == null) return $"{{\"error\":\"Component type '{componentName}' not found\"}}";

            var comp = element.GetComponent(compType);
            if (comp == null) return $"{{\"error\":\"Component '{componentName}' not found on element\"}}";

            var so = new SerializedObject(comp);
            var prop = so.FindProperty(property);
            if (prop == null) return $"{{\"error\":\"Property '{property}' not found\"}}";

            SetSerializedPropertyValue(prop, value);
            so.ApplyModifiedProperties();

            return "{\"success\":true}";
        }

        private static string ListComponents(Dictionary<string, string> args)
        {
            string prefabPath = args.GetValueOrDefault("prefab", "");
            string elementName = args.GetValueOrDefault("element", "");

            var prefabRoot = GetOrLoadPrefab(prefabPath);
            if (prefabRoot == null) return "{\"error\":\"Prefab not found\"}";

            Transform element = string.IsNullOrEmpty(elementName)
                ? prefabRoot.transform
                : FindChild(prefabRoot.transform, elementName);
            if (element == null) return $"{{\"error\":\"Element '{elementName}' not found\"}}";

            var components = element.GetComponents<Component>();
            var names = components.Select(c => c.GetType().Name).ToArray();

            return $"{{\"components\":[{string.Join(",", names.Select(n => $"\"{n}\""))}]}}";
        }

        // ===== Helper Methods =====

        private static GameObject GetOrLoadPrefab(string path)
        {
            if (_loadedPrefabs.TryGetValue(path, out var existing))
            {
                return existing;
            }

            if (!System.IO.File.Exists(path)) return null;

            var instance = PrefabUtility.LoadPrefabContents(path);
            _loadedPrefabs[path] = instance;
            return instance;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void CreateFolderRecursive(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static Vector2 ParseVector2(string s)
        {
            s = s.Trim('(', ')', '[', ']');
            var parts = s.Split(',');
            if (parts.Length >= 2)
            {
                float.TryParse(parts[0].Trim(), out float x);
                float.TryParse(parts[1].Trim(), out float y);
                return new Vector2(x, y);
            }
            return Vector2.zero;
        }

        private static Color ParseColor(string s)
        {
            if (ColorUtility.TryParseHtmlString(s, out Color color))
                return color;
            
            s = s.Trim('(', ')', '[', ']');
            var parts = s.Split(',');
            if (parts.Length >= 3)
            {
                float.TryParse(parts[0].Trim(), out float r);
                float.TryParse(parts[1].Trim(), out float g);
                float.TryParse(parts[2].Trim(), out float b);
                float a = 1f;
                if (parts.Length >= 4) float.TryParse(parts[3].Trim(), out a);
                return new Color(r, g, b, a);
            }
            return Color.white;
        }

        private static void ApplyAnchorPreset(RectTransform rect, string preset)
        {
            switch (preset.ToLower())
            {
                case "topleft":
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);
                    break;
                case "topcenter":
                    rect.anchorMin = new Vector2(0.5f, 1);
                    rect.anchorMax = new Vector2(0.5f, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    break;
                case "topright":
                    rect.anchorMin = new Vector2(1, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(1, 1);
                    break;
                case "middleleft":
                case "centerleft":
                    rect.anchorMin = new Vector2(0, 0.5f);
                    rect.anchorMax = new Vector2(0, 0.5f);
                    rect.pivot = new Vector2(0, 0.5f);
                    break;
                case "center":
                case "middlecenter":
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "middleright":
                case "centerright":
                    rect.anchorMin = new Vector2(1, 0.5f);
                    rect.anchorMax = new Vector2(1, 0.5f);
                    rect.pivot = new Vector2(1, 0.5f);
                    break;
                case "bottomleft":
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0, 0);
                    break;
                case "bottomcenter":
                    rect.anchorMin = new Vector2(0.5f, 0);
                    rect.anchorMax = new Vector2(0.5f, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    break;
                case "bottomright":
                    rect.anchorMin = new Vector2(1, 0);
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(1, 0);
                    break;
                case "stretchall":
                case "stretch":
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    break;
            }
        }

        private static string BuildHierarchy(Transform t)
        {
            var children = new List<string>();
            foreach (Transform child in t)
            {
                children.Add(BuildHierarchy(child));
            }

            var components = t.GetComponents<Component>().Select(c => $"\"{c.GetType().Name}\"");
            
            return $"{{\"name\":\"{t.name}\",\"components\":[{string.Join(",", components)}],\"children\":[{string.Join(",", children)}]}}";
        }

        private static Type FindType(string typeName)
        {
            // Search in all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null) return type;

                // Try without namespace
                type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type != null) return type;
            }
            return null;
        }

        private static void SetSerializedPropertyValue(SerializedProperty prop, string value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (int.TryParse(value, out int i)) prop.intValue = i;
                    break;
                case SerializedPropertyType.Float:
                    if (float.TryParse(value, out float f)) prop.floatValue = f;
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = value.ToLower() == "true";
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = value;
                    break;
                case SerializedPropertyType.Color:
                    prop.colorValue = ParseColor(value);
                    break;
                case SerializedPropertyType.Vector2:
                    prop.vector2Value = ParseVector2(value);
                    break;
            }
        }

        private static void CreateScrollView(GameObject parent)
        {
            // Simplified scroll view creation
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(parent.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);

            var scrollRect = parent.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }

        private static void CreateInputField(GameObject parent)
        {
            var img = parent.AddComponent<Image>();
            img.color = Color.white;

            var textArea = new GameObject("Text Area");
            textArea.transform.SetParent(parent.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -7);

            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            var phRect = placeholder.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            var phText = placeholder.AddComponent<Text>();
            phText.text = "Enter text...";
            phText.color = new Color(0, 0, 0, 0.5f);
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textArea.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGO.AddComponent<Text>();
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.supportRichText = false;

            var inputField = parent.AddComponent<InputField>();
            inputField.textComponent = text;
            inputField.placeholder = phText;
        }
    }
}
