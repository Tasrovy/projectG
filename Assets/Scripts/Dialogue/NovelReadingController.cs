using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yarn.Unity;

/// <summary>DialogueHandler 自动安装。只改变渲染与射线，不关闭剧情对象。</summary>
public sealed class NovelReadingController : MonoBehaviour
{
    public enum DisplayMode { Full, Reading, Artwork }
    public DisplayMode Mode { get; private set; }
    public int lettersPerSecond = 30;
    private DialogueRunner runner;
    private LinePresenter presenter;
    private readonly Dictionary<Graphic, bool> hidden = new Dictionary<Graphic, bool>();
    private readonly List<RaycastResult> hits = new List<RaycastResult>();

    public void Initialize(DialogueRunner dialogueRunner)
    {
        runner = dialogueRunner;
        if (runner == null) return;
        presenter = runner.GetComponentInChildren<LinePresenter>(true);
        if (presenter == null)
            presenter = FindFirstObjectByType<LinePresenter>(FindObjectsInactive.Include);
        if (presenter != null)
        {
            presenter.autoAdvance = false;
            presenter.lettersPerSecond = lettersPerSecond;
        }
    }

    private void Update()
    {
        if (runner == null || !runner.IsDialogueRunning)
        {
            if (Mode != DisplayMode.Full) Restore();
            Mode = DisplayMode.Full;
            return;
        }
        if (GetComponent<NovelSkipController>()?.BlocksReading == true) return;
        if (MousePressed(1))
        {
            Restore();
            Mode = (DisplayMode)(((int)Mode + 1) % 3);
        }
        if (Mode == DisplayMode.Artwork || !MousePressed(0) || presenter == null
            || presenter.lineText == null || !presenter.gameObject.activeInHierarchy
            || IsOverControl()) return;
        var continueHandler = runner.GetComponentInChildren<NovelContinueHandler>(true);
        if (continueHandler == null)
            continueHandler = FindFirstObjectByType<NovelContinueHandler>();
        if (continueHandler != null)
        {
            continueHandler.Advance();
            return;
        }
        // 没有正文按钮处理器的独立测试场景使用 TMP 可见字数判断。
        if (presenter.lineText.maxVisibleCharacters < presenter.lineText.textInfo.characterCount)
            runner.RequestHurryUpLine();
        else
            runner.RequestNextLine();
    }

    private void LateUpdate()
    {
        if (Mode == DisplayMode.Full || runner == null || !runner.IsDialogueRunning) return;
        foreach (Graphic graphic in FindObjectsByType<Graphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            bool stats = HasAncestor(graphic.transform, "targets")
                || HasAncestor(graphic.transform, "money_charm")
                || HasAncestor(graphic.transform, "propIcon_1")
                || HasAncestor(graphic.transform, "propIcon_2")
                || HasAncestor(graphic.transform, "propIcon_3")
                || HasAncestor(graphic.transform, "propIcon_4");
            bool dialogueUi = HasAncestor(graphic.transform, "Dialogue System");
            bool calendar = HasAncestor(graphic.transform, "calendar") || HasAncestor(graphic.transform, "showCal");
            if (stats || calendar || (Mode == DisplayMode.Artwork && (dialogueUi || calendar
                || HasAncestor(graphic.transform, "properties"))))
            {
                if (!hidden.ContainsKey(graphic)) hidden.Add(graphic, graphic.raycastTarget);
                graphic.canvasRenderer.SetAlpha(0);
                graphic.raycastTarget = false;
            }
        }
    }

    private bool IsOverControl()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;
        var data = new PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = MousePosition() };
        hits.Clear();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(data, hits);
        foreach (var hit in hits)
            if (hit.gameObject.GetComponentInParent<Selectable>() != null) return true;
        return false;
    }

    private static bool HasAncestor(Transform node, string name)
    {
        for (; node != null; node = node.parent)
            if (string.Equals(node.name, name, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private void Restore()
    {
        foreach (var entry in hidden)
            if (entry.Key != null)
            {
                entry.Key.canvasRenderer.SetAlpha(1);
                entry.Key.raycastTarget = entry.Value;
            }
        hidden.Clear();
    }
    private void OnDisable() { Restore(); Mode = DisplayMode.Full; }

    private static bool MousePressed(int button)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        return mouse != null && (button == 0 ? mouse.leftButton.wasPressedThisFrame : mouse.rightButton.wasPressedThisFrame);
#else
        return Input.GetMouseButtonDown(button);
#endif
    }
    private static Vector2 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
