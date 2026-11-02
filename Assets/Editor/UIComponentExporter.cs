#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text;
using TMPro;

public class UIComponentExporter : EditorWindow
{
    private StringBuilder outputBuilder;
    private Vector2 scrollPosition;
    private string outputText = "";
    private bool includeInactive = true;
    private bool recursive = true;

    [MenuItem("GameObject/UI组件及其信息导出器", priority = 0)]
    private static void ShowWindow()
    {
        GetWindow<UIComponentExporter>("UI组件信息导出器");
    }

    private void OnGUI()
    {
        GUILayout.Label("UI组件详细信息提取器", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        includeInactive = EditorGUILayout.Toggle("包含未激活物体", includeInactive);
        recursive = EditorGUILayout.Toggle("递归包含子节点", recursive);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("提取选中此物体的UI信息", GUILayout.Height(30)))
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

    private void AnalyzeSelectedObject()
    {
        if (Selection.activeGameObject == null)
        {
            ShowNotification(new GUIContent("请先选中一个游戏对象!"));
            return;
        }

        outputBuilder = new StringBuilder();
        outputBuilder.AppendLine("=== UI组件详细信息 ===");
        outputBuilder.AppendLine($"分析目标: {Selection.activeGameObject.name}");
        outputBuilder.AppendLine();

        ProcessGameObject(Selection.activeGameObject, 0);

        outputText = outputBuilder.ToString();
        Repaint();
    }

    private void ProcessGameObject(GameObject obj, int indentLevel)
    {
        if (!includeInactive && !obj.activeSelf)
            return;

        string indent = new string(' ', indentLevel * 4);
        string status = obj.activeSelf ? "[✓]" : "[✗]";
        
        // 提取UI相关组件
        string uiComponentDetails = GetUIComponentDetails(obj, indent);
        
        if (!string.IsNullOrEmpty(uiComponentDetails))
        {
            outputBuilder.AppendLine($"{indent}{status} GameObject: {obj.name}");
            outputBuilder.Append(uiComponentDetails);
            outputBuilder.AppendLine();
        }

        if (recursive)
        {
            foreach (Transform child in obj.transform)
            {
                ProcessGameObject(child.gameObject, indentLevel + 1);
            }
        }
    }

    private string GetUIComponentDetails(GameObject obj, string baseIndent)
    {
        StringBuilder sb = new StringBuilder();
        string indent = baseIndent + "    "; // 组件级别比GameObject多缩进一层

        // RectTransform
        var rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            sb.AppendLine($"{indent}- RectTransform:");
            sb.AppendLine($"{indent}  Position: {rectTransform.anchoredPosition}  Size: {rectTransform.sizeDelta}");
            sb.AppendLine($"{indent}  Pivot: {rectTransform.pivot}  Scale: {rectTransform.localScale}");
            sb.AppendLine($"{indent}  Anchors: Min {rectTransform.anchorMin}, Max {rectTransform.anchorMax}");
        }

        // Canvas
        var canvas = obj.GetComponent<Canvas>();
        if (canvas != null)
        {
            sb.AppendLine($"{indent}- Canvas:");
            sb.AppendLine($"{indent}  Render Mode: {canvas.renderMode}  Pixel Perfect: {canvas.pixelPerfect}");
            sb.AppendLine($"{indent}  Sorting Layer: {canvas.sortingLayerName}  Order in Layer: {canvas.sortingOrder}");
        }

        // GraphicRaycaster
        var graphicRaycaster = obj.GetComponent<GraphicRaycaster>();
        if (graphicRaycaster != null)
        {
            sb.AppendLine($"{indent}- GraphicRaycaster:");
            sb.AppendLine($"{indent}  Ignore Reversed Graphics: {graphicRaycaster.ignoreReversedGraphics}");
            sb.AppendLine($"{indent}  Blocking Objects: {graphicRaycaster.blockingObjects}");
        }

        // CanvasGroup
        var canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            sb.AppendLine($"{indent}- CanvasGroup:");
            sb.AppendLine($"{indent}  Alpha: {canvasGroup.alpha}  Interactable: {canvasGroup.interactable}");
            sb.AppendLine($"{indent}  Blocks Raycasts: {canvasGroup.blocksRaycasts}");
        }

        // Image
        var image = obj.GetComponent<Image>();
        if (image != null)
        {
            sb.AppendLine($"{indent}- Image:");
            sb.AppendLine($"{indent}  Sprite: {(image.sprite != null ? image.sprite.name : "None")}");
            sb.AppendLine($"{indent}  Color: {ColorUtility.ToHtmlStringRGBA(image.color)}");
            sb.AppendLine($"{indent}  Raycast Target: {image.raycastTarget}  Type: {image.type}");
        }
        
        // RawImage
        var rawImage = obj.GetComponent<RawImage>();
        if (rawImage != null)
        {
            sb.AppendLine($"{indent}- RawImage:");
            sb.AppendLine($"{indent}  Texture: {(rawImage.texture != null ? rawImage.texture.name : "None")}");
            sb.AppendLine($"{indent}  Color: {ColorUtility.ToHtmlStringRGBA(rawImage.color)}");
        }

        // Text (Legacy)
        var text = obj.GetComponent<Text>();
        if (text != null)
        {
            sb.AppendLine($"{indent}- Text (Legacy):");
            sb.AppendLine($"{indent}  Content: \"{text.text.Replace("\n", "\\n")}\"");
            sb.AppendLine($"{indent}  Font Size: {text.fontSize}  Color: {ColorUtility.ToHtmlStringRGBA(text.color)}");
            sb.AppendLine($"{indent}  Alignment: {text.alignment}");
        }

        // TextMeshProUGUI
        var tmpText = obj.GetComponent<TextMeshProUGUI>();
        if (tmpText != null)
        {
            sb.AppendLine($"{indent}- TextMeshProUGUI:");
            sb.AppendLine($"{indent}  Content: \"{tmpText.text.Replace("\n", "\\n")}\"");
            sb.AppendLine($"{indent}  Font Size: {tmpText.fontSize}  Color: {ColorUtility.ToHtmlStringRGBA(tmpText.color)}");
            sb.AppendLine($"{indent}  Alignment: {tmpText.alignment}");
        }

        // Button
        var button = obj.GetComponent<Button>();
        if (button != null)
        {
            sb.AppendLine($"{indent}- Button:");
            sb.AppendLine($"{indent}  Interactable: {button.interactable}");
            sb.AppendLine($"{indent}  Target Graphic: {(button.targetGraphic != null ? button.targetGraphic.gameObject.name : "None")}");
        }

        // Toggle
        var toggle = obj.GetComponent<Toggle>();
        if (toggle != null)
        {
            sb.AppendLine($"{indent}- Toggle:");
            sb.AppendLine($"{indent}  IsOn: {toggle.isOn}  Interactable: {toggle.interactable}");
        }

        // Slider
        var slider = obj.GetComponent<Slider>();
        if (slider != null)
        {
            sb.AppendLine($"{indent}- Slider:");
            sb.AppendLine($"{indent}  Value: {slider.value} (Min: {slider.minValue}, Max: {slider.maxValue})");
            sb.AppendLine($"{indent}  Interactable: {slider.interactable}");
        }

        // ScrollRect (ScrollView 核心组件)
        var scrollRect = obj.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            sb.AppendLine($"{indent}- ScrollRect:");
            sb.AppendLine($"{indent}  Horizontal: {scrollRect.horizontal}  Vertical: {scrollRect.vertical}");
            sb.AppendLine($"{indent}  Movement Type: {scrollRect.movementType}  Inertia: {scrollRect.inertia}");
            sb.AppendLine($"{indent}  Elasticity: {scrollRect.elasticity}  Deceleration Rate: {scrollRect.decelerationRate}");
            sb.AppendLine($"{indent}  Scroll Sensitivity: {scrollRect.scrollSensitivity}");
            sb.AppendLine($"{indent}  Content: {(scrollRect.content != null ? scrollRect.content.gameObject.name : "None")}");
            sb.AppendLine($"{indent}  Viewport: {(scrollRect.viewport != null ? scrollRect.viewport.gameObject.name : "None")}");
            sb.AppendLine($"{indent}  Horizontal Scrollbar: {(scrollRect.horizontalScrollbar != null ? scrollRect.horizontalScrollbar.gameObject.name : "None")}");
            sb.AppendLine($"{indent}  Vertical Scrollbar: {(scrollRect.verticalScrollbar != null ? scrollRect.verticalScrollbar.gameObject.name : "None")}");
        }

        // Scrollbar
        var scrollbar = obj.GetComponent<Scrollbar>();
        if (scrollbar != null)
        {
            sb.AppendLine($"{indent}- Scrollbar:");
            sb.AppendLine($"{indent}  Direction: {scrollbar.direction}");
            sb.AppendLine($"{indent}  Value: {scrollbar.value}  Size: {scrollbar.size}  Steps: {scrollbar.numberOfSteps}");
            sb.AppendLine($"{indent}  Interactable: {scrollbar.interactable}");
            sb.AppendLine($"{indent}  Handle Rect: {(scrollbar.handleRect != null ? scrollbar.handleRect.gameObject.name : "None")}");
        }

        // Mask
        var mask = obj.GetComponent<Mask>();
        if (mask != null)
        {
            sb.AppendLine($"{indent}- Mask:");
            sb.AppendLine($"{indent}  Show Mask Graphic: {mask.showMaskGraphic}");
        }

        // RectMask2D
        var rectMask2D = obj.GetComponent<RectMask2D>();
        if (rectMask2D != null)
        {
            sb.AppendLine($"{indent}- RectMask2D:");
            sb.AppendLine($"{indent}  Padding: {rectMask2D.padding}");
            sb.AppendLine($"{indent}  Softness: {rectMask2D.softness}");
        }

        // Layout Groups
        var horizontal = obj.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null) sb.AppendLine($"{indent}- HorizontalLayoutGroup: Spacing={horizontal.spacing}");
        
        var vertical = obj.GetComponent<VerticalLayoutGroup>();
        if (vertical != null) sb.AppendLine($"{indent}- VerticalLayoutGroup: Spacing={vertical.spacing}");
        
        var grid = obj.GetComponent<GridLayoutGroup>();
        if (grid != null) sb.AppendLine($"{indent}- GridLayoutGroup: CellSize={grid.cellSize}, Spacing={grid.spacing}");

        // ContentSizeFitter
        var contentSizeFitter = obj.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            sb.AppendLine($"{indent}- ContentSizeFitter:");
            sb.AppendLine($"{indent}  Horizontal Fit: {contentSizeFitter.horizontalFit}  Vertical Fit: {contentSizeFitter.verticalFit}");
        }

        // LayoutElement
        var layoutElement = obj.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            sb.AppendLine($"{indent}- LayoutElement:");
            sb.AppendLine($"{indent}  Ignore Layout: {layoutElement.ignoreLayout}");
            sb.AppendLine($"{indent}  Min: ({layoutElement.minWidth}, {layoutElement.minHeight})  Preferred: ({layoutElement.preferredWidth}, {layoutElement.preferredHeight})");
            sb.AppendLine($"{indent}  Flexible: ({layoutElement.flexibleWidth}, {layoutElement.flexibleHeight})");
        }

        return sb.ToString();
    }
}
#endif
