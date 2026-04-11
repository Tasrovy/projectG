#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text;
using TMPro;

public class UIHierarchyExporter : EditorWindow
{
    private StringBuilder outputBuilder;
    private Vector2 scrollPosition;
    private string outputText = "";
    private bool showOnlyUI = true;
    private bool includeInactive = true;

    [MenuItem("GameObject/UI层级结构导出器", priority = 0)]
    public static void ShowWindow()
    {
        GetWindow<UIHierarchyExporter>("UI层级结构导出器");
    }

    private void OnGUI()
    {
        GUILayout.Label("场景UI结构查看器", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        showOnlyUI = EditorGUILayout.Toggle("仅显示UI元素", showOnlyUI);
        includeInactive = EditorGUILayout.Toggle("包含未激活物体", includeInactive);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("分析当前场景", GUILayout.Height(30)))
        {
            AnalyzeCurrentScene();
        }
        if (GUILayout.Button("分析选中物体", GUILayout.Height(30)))
        {
            AnalyzeSelectedObject();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("复制到剪贴板", GUILayout.Height(25)))
        {
            EditorGUIUtility.systemCopyBuffer = outputText;
            ShowNotification(new GUIContent("已复制到剪贴板!"));
        }
        
        EditorGUILayout.Space();
        
        GUILayout.Label("输出结果:", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        
        GUIStyle textStyle = new GUIStyle(EditorStyles.textArea)
        {
            richText = true,
            fontSize = 11
        };
        
        EditorGUILayout.TextArea(outputText, textStyle, GUILayout.ExpandHeight(true));
        
        EditorGUILayout.EndScrollView();
    }

    private void AnalyzeCurrentScene()
    {
        outputBuilder = new StringBuilder();
        outputBuilder.AppendLine("=== 当前场景UI结构 ===");
        outputBuilder.AppendLine();

        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        
        foreach (GameObject rootObj in rootObjects)
        {
            if (!includeInactive && !rootObj.activeSelf)
                continue;
                
            ProcessGameObject(rootObj, 0);
        }

        outputText = outputBuilder.ToString();
        Repaint();
    }

    private void AnalyzeSelectedObject()
    {
        if (Selection.activeGameObject == null)
        {
            ShowNotification(new GUIContent("请先选中一个游戏对象!"));
            return;
        }

        outputBuilder = new StringBuilder();
        outputBuilder.AppendLine("=== 选中物体UI结构 ===");
        outputBuilder.AppendLine($"根节点: {Selection.activeGameObject.name}");
        outputBuilder.AppendLine();

        ProcessGameObject(Selection.activeGameObject, 0);

        outputText = outputBuilder.ToString();
        Repaint();
    }

    private void ProcessGameObject(GameObject obj, int indentLevel)
    {
        if (!includeInactive && !obj.activeSelf)
            return;

        bool isUIElement = IsUIElement(obj);
        
        if (showOnlyUI && !isUIElement)
        {
            // 即使不显示非UI元素，也要检查其子对象
            foreach (Transform child in obj.transform)
            {
                ProcessGameObject(child.gameObject, indentLevel);
            }
            return;
        }

        string indent = new string(' ', indentLevel * 2);
        string status = obj.activeSelf ? "[✓]" : "[✗]";
        string layer = $"(Layer: {LayerMask.LayerToName(obj.layer)})";
        
        outputBuilder.Append($"{indent}{status} {obj.name} {layer}");

        // 添加UI组件信息
        if (isUIElement)
        {
            string uiComponents = GetUIComponents(obj);
            if (!string.IsNullOrEmpty(uiComponents))
            {
                outputBuilder.Append($" - {uiComponents}");
            }
        }

        outputBuilder.AppendLine();

        // 递归处理子对象
        foreach (Transform child in obj.transform)
        {
            ProcessGameObject(child.gameObject, indentLevel + 1);
        }
    }

    private bool IsUIElement(GameObject obj)
    {
        return obj.GetComponent<Canvas>() != null ||
               obj.GetComponent<Graphic>() != null ||
               obj.GetComponent<CanvasRenderer>() != null ||
               obj.GetComponent<RectTransform>() != null;
    }

    private string GetUIComponents(GameObject obj)
    {
        System.Collections.Generic.List<string> components = new System.Collections.Generic.List<string>();

        if (obj.GetComponent<Canvas>() != null)
        {
            Canvas canvas = obj.GetComponent<Canvas>();
            components.Add($"Canvas({canvas.renderMode})");
        }

        if (obj.GetComponent<CanvasRenderer>() != null)
            components.Add("CanvasRenderer");

        if (obj.GetComponent<Image>() != null)
            components.Add("Image");
        
        if (obj.GetComponent<RawImage>() != null)
            components.Add("RawImage");

        if (obj.GetComponent<Text>() != null)
            components.Add("Text");
        
        if (obj.GetComponent<TextMeshProUGUI>() != null)
            components.Add("TextMeshProUGUI");

        if (obj.GetComponent<Button>() != null)
            components.Add("Button");

        if (obj.GetComponent<Toggle>() != null)
            components.Add("Toggle");

        if (obj.GetComponent<Slider>() != null)
            components.Add("Slider");

        if (obj.GetComponent<Scrollbar>() != null)
            components.Add("Scrollbar");

        if (obj.GetComponent<Dropdown>() != null)
            components.Add("Dropdown");

        if (obj.GetComponent<InputField>() != null)
            components.Add("InputField");
        
        if (obj.GetComponent<TMPro.TMP_InputField>() != null)
            components.Add("TMP_InputField");

        if (obj.GetComponent<ScrollRect>() != null)
            components.Add("ScrollRect");

        if (obj.GetComponent<GridLayoutGroup>() != null)
            components.Add("GridLayoutGroup");
        
        if (obj.GetComponent<HorizontalLayoutGroup>() != null)
            components.Add("HorizontalLayoutGroup");
        
        if (obj.GetComponent<VerticalLayoutGroup>() != null)
            components.Add("VerticalLayoutGroup");

        return string.Join(", ", components);
    }
}
#endif
