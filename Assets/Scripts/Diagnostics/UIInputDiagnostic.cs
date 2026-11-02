using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_2019_1_OR_NEWER
using UEEventSystem = UnityEngine.EventSystems.EventSystem;
#else
using UEEventSystem = UnityEngine.EventSystems.EventSystem;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UIInputDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[UIDiag] Screen: {Screen.width}x{Screen.height} DPI:{Screen.dpi} Platform:{Application.platform}");
        var es = UEEventSystem.current;
        if (es == null)
        {
            Debug.LogWarning("[UIDiag] No EventSystem in scene.");
        }
        else
        {
            Debug.Log($"[UIDiag] EventSystem found: {es.gameObject.name}");
            var module = es.currentInputModule;
            Debug.Log($"[UIDiag] currentInputModule: {(module != null ? module.GetType().Name : "null")}");
#if ENABLE_INPUT_SYSTEM
            Debug.Log("[UIDiag] Compile: ENABLE_INPUT_SYSTEM defined");
#else
            Debug.Log("[UIDiag] Compile: ENABLE_INPUT_SYSTEM NOT defined");
#endif
        }

        var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        Debug.Log($"[UIDiag] Canvas count: {canvases.Length}");
        foreach(var c in canvases)
        {
            var gr = c.GetComponent<GraphicRaycaster>();
            Debug.Log($"[UIDiag] Canvas '{c.name}': renderMode={c.renderMode} scaleFactor={c.scaleFactor} pixelPerfect={c.pixelPerfect} worldCamera={(c.worldCamera!=null?c.worldCamera.name:"null")} raycaster={(gr!=null?gr.GetType().Name:"none")}");
        }

        var allES = Resources.FindObjectsOfTypeAll<UEEventSystem>();
        Debug.Log($"[UIDiag] Total EventSystem instances: {allES.Length}");
        for (int i=0;i<allES.Length;i++)
            Debug.Log($"[UIDiag] ES[{i}] = {allES[i].gameObject.name}");

        var buttons = Resources.FindObjectsOfTypeAll<Button>();
        Debug.Log($"[UIDiag] Button count: {buttons.Length} (printing up to 20)");
        int limit = Mathf.Min(20, buttons.Length);
        for (int i=0;i<limit;i++)
        {
            var b = buttons[i];
            RectTransform rt = b.GetComponent<RectTransform>();
            Debug.Log($"[UIDiag] Button[{i}] '{b.gameObject.name}' active={b.gameObject.activeInHierarchy} interactable={b.interactable} rect={rt.rect} worldPos={rt.position} rootCanvas={(b.GetComponentInParent<Canvas>()?b.GetComponentInParent<Canvas>().name:"null")} ");
            var cg = b.GetComponentInParent<CanvasGroup>();
            if (cg != null) Debug.Log($"[UIDiag]   Parent CanvasGroup blocksRaycasts={cg.blocksRaycasts} alpha={cg.alpha}");
            var mask = b.GetComponentInParent<Mask>();
            if (mask != null) Debug.Log("[UIDiag]   Parent Mask present (may clip)");
        }

        Debug.Log("[UIDiag] Diagnostic complete.");
    }
}
