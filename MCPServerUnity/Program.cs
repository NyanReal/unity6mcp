using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<HttpClient>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

// ===== MCP Tools =====

[McpServerToolType]
public static class UnityUGUITools
{
    private const string UNITY_URL = "http://localhost:6200";
    private static readonly HttpClient _httpClient = new();

    private static async Task<string> CallUnity(string command, Dictionary<string, string> args)
    {
        try
        {
            var request = new
            {
                command = command,
                args = JsonSerializer.Serialize(args)
            };

            var response = await _httpClient.PostAsJsonAsync(UNITY_URL, request);
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException)
        {
            return "{\"error\":\"Unity Editor not running or HTTP server not started\"}";
        }
        catch (Exception e)
        {
            return $"{{\"error\":\"{e.Message}\"}}";
        }
    }

    // ===== UI Layout Tools =====

    [McpServerTool, Description("Create a new Canvas-based UI prefab")]
    public static async Task<string> create_ui_prefab(
        [Description("Prefab name")] string name,
        [Description("Asset path (e.g., Assets/UI)")] string path = "Assets/UI",
        [Description("Include Canvas components (false for child UI elements)")] bool with_canvas = true)
    {
        var args = new Dictionary<string, string>
        {
            ["name"] = name,
            ["path"] = path,
            ["with_canvas"] = with_canvas.ToString().ToLower()
        };
        return await CallUnity("create_ui_prefab", args);
    }

    [McpServerTool, Description("Add a UI element (Panel, Button, Text, Image, RawImage, ScrollView, InputField) to prefab")]
    public static async Task<string> add_ui_element(
        [Description("Prefab asset path")] string prefab,
        [Description("Element type: Panel, Button, Text, Image, RawImage, ScrollView, InputField")] string type,
        [Description("Element name (use descriptive names for binding)")] string name,
        [Description("Parent element name (empty for root)")] string parent = "")
    {
        return await CallUnity("add_ui_element", new Dictionary<string, string>
        {
            ["prefab"] = prefab,
            ["type"] = type,
            ["name"] = name,
            ["parent"] = parent
        });
    }

    [McpServerTool, Description("Set position, size, anchors for a UI element")]
    public static async Task<string> set_rect_transform(
        [Description("Prefab asset path")] string prefab,
        [Description("Element name")] string element,
        [Description("Anchor preset: TopLeft, TopCenter, TopRight, MiddleLeft, Center, MiddleRight, BottomLeft, BottomCenter, BottomRight, StretchAll")] string? anchors = null,
        [Description("Position as (x, y)")] string? position = null,
        [Description("Size as (width, height)")] string? size = null,
        [Description("Pivot as (x, y), 0-1 range")] string? pivot = null)
    {
        var args = new Dictionary<string, string>
        {
            ["prefab"] = prefab,
            ["element"] = element
        };
        if (anchors != null) args["anchors"] = anchors;
        if (position != null) args["position"] = position;
        if (size != null) args["size"] = size;
        if (pivot != null) args["pivot"] = pivot;

        return await CallUnity("set_rect_transform", args);
    }

    [McpServerTool, Description("Set UI element property (text, color, fontSize, sprite, enabled)")]
    public static async Task<string> set_ui_property(
        [Description("Prefab asset path")] string prefab,
        [Description("Element name")] string element,
        [Description("Property: text, color, fontSize, sprite, enabled")] string property,
        [Description("Property value")] string value)
    {
        return await CallUnity("set_ui_property", new Dictionary<string, string>
        {
            ["prefab"] = prefab,
            ["element"] = element,
            ["property"] = property,
            ["value"] = value
        });
    }

    [McpServerTool, Description("Get UI hierarchy as JSON")]
    public static async Task<string> read_ui_hierarchy(
        [Description("Prefab asset path")] string prefab)
    {
        return await CallUnity("read_ui_hierarchy", new Dictionary<string, string>
        {
            ["prefab"] = prefab
        });
    }

    [McpServerTool, Description("Delete a UI element from prefab")]
    public static async Task<string> delete_ui_element(
        [Description("Prefab asset path")] string prefab,
        [Description("Element name to delete")] string element)
    {
        return await CallUnity("delete_ui_element", new Dictionary<string, string>
        {
            ["prefab"] = prefab,
            ["element"] = element
        });
    }

    [McpServerTool, Description("Save prefab changes and unload from memory")]
    public static async Task<string> save_prefab(
        [Description("Prefab asset path")] string prefab)
    {
        return await CallUnity("save_prefab", new Dictionary<string, string>
        {
            ["prefab"] = prefab
        });
    }

    // ===== Component & Binding Tools =====

    [McpServerTool, Description("Add a MonoBehaviour script component to an element")]
    public static async Task<string> add_component(
        [Description("Prefab asset path")] string prefab,
        [Description("Script class name")] string script_name,
        [Description("Element name (empty for root)")] string element = "")
    {
        return await CallUnity("add_component", new Dictionary<string, string>
        {
            ["prefab"] = prefab,
            ["element"] = element,
            ["script_name"] = script_name
        });
    }

    [McpServerTool, Description("Bind a SerializedField reference to a UI element")]
    public static async Task<string> bind_reference(
        [Description("Prefab asset path")] string prefab,
        [Description("SerializedField name")] string field_name,
        [Description("Target element to reference")] string target_element,
        [Description("Element with the script (empty for root)")] string element = "",
        [Description("Specific component type to reference (optional)")] string? component_type = null)
    {
        var args = new Dictionary<string, string>
        {
            ["prefab"] = prefab,
            ["element"] = element,
            ["field_name"] = field_name,
            ["target_element"] = target_element
        };
        if (component_type != null) args["component_type"] = component_type;

        return await CallUnity("bind_reference", args);
    }

    [McpServerTool, Description("Set a property on a component")]
    public static async Task<string> set_component_property(
        [Description("Prefab asset path")] string prefab,
        [Description("Element name")] string element,
        [Description("Component class name")] string component,
        [Description("Property/field name")] string property,
        [Description("Property value")] string value)
    {
        return await CallUnity("set_component_property", new Dictionary<string, string>
        {
            ["prefab"] = prefab,
            ["element"] = element,
            ["component"] = component,
            ["property"] = property,
            ["value"] = value
        });
    }

    [McpServerTool, Description("List all components on an element")]
    public static async Task<string> list_components(
        [Description("Prefab asset path")] string prefab,
        [Description("Element name")] string element)
    {
        return await CallUnity("list_components", new Dictionary<string, string>
        {
            ["prefab"] = prefab,
            ["element"] = element
        });
    }
}
