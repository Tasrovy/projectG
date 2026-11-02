using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 挂到 DialogueSystem（LinePresenter 的父物体）上。
/// 自动在子层级中找到名为 "Text" 的 TMP_Text（即 lineText，含 inactive 物体）。
/// 监听文本变化，自动解析 &lt;shake&gt; / &lt;wave&gt; 标签范围并驱动顶点动画。
/// 说话者名字 TMP 是独立对象，不会受影响。
///
/// Yarn 脚本用法：
///   伊兰莉特: <wave>花开得真好。</wave>
///   （<shake>树枝折断了</shake>）
///   伊兰莉特: 把它们<shake>做成花环</shake>戴在你头上。
/// </summary>
public class TextSpecialEffect : MonoBehaviour
{
    [Header("Wave 波浪效果 —— 字符依次上下起伏")]
    [Tooltip("振幅：字符偏移的最大像素高度，值越大波浪越夸张")]
    public float waveAmplitude  = 5f;
    [Tooltip("速度：波浪动画的播放速率，值越大起伏越快")]
    public float waveSpeed      = 2f;
    [Tooltip("间距：相邻字符之间的相位差，值越大同屏波峰越多")]
    public float waveSpacing    = 0.4f;

    [Header("Shake 抖动效果 —— 字符每帧随机偏移")]
    [Tooltip("幅度：每帧 XY 方向的最大随机偏移像素量，值越大抖动越剧烈")]
    public float shakeMagnitude = 2f;

    private TMP_Text _tmp;

    // 按可见字符索引记录哪些字符需要动画
    private readonly HashSet<int> _waveChars  = new();
    private readonly HashSet<int> _shakeChars = new();
    private bool _hasEffect;

    // ──────────────────────────────────────────────
    //  生命周期
    // ──────────────────────────────────────────────

    private void Awake()
    {
        // 从子层级（含 inactive 物体）中找名为 "Text" 的 TMP_Text（即 LinePresenter 的 lineText）
        _tmp = FindTMPInChildren(transform, "Text");
        if (_tmp == null)
            Debug.LogWarning("[TextSpecialEffect] 未在子层级中找到名为 \"Text\" 的 TMP_Text，请确认 DialogueSystem 层级结构。");
    }

    /// <summary>
    /// 递归遍历子层级（含 inactive），找到第一个名字匹配的 TMP_Text。
    /// </summary>
    private static TMP_Text FindTMPInChildren(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
            {
                TMP_Text t = child.GetComponent<TMP_Text>();
                if (t != null) return t;
            }
            TMP_Text nested = FindTMPInChildren(child, targetName);
            if (nested != null) return nested;
        }
        return null;
    }

    // 上一帧已处理过的文本（剥离标签后的版本），用于检测文本是否真正变化
    private string _lastKnownText = null;

    // maxVisibleCharacters 上一帧的值。
    // Yarn 每次开始新行时，PrepareForContent 会将 maxVisibleCharacters 重置为 0，
    // RunTypewriter 也会在开头重置，因此 maxVC 出现"减少"就是可靠的"新行"信号。
    private int _lastMaxVC = -1;

    // ──────────────────────────────────────────────
    //  文本变化检测：解析标签 + 剥离自定义标签
    // ──────────────────────────────────────────────

    private void CheckTextChanged()
    {
        int currentMaxVC = _tmp.maxVisibleCharacters;

        // maxVisibleCharacters 出现下降 = PrepareForContent/RunTypewriter 重置了计数器
        // → 必定是一句全新的 Yarn 行，无论文本内容是否相同，都要重新判断
        if (_lastMaxVC > currentMaxVC)
            _lastKnownText = null;

        _lastMaxVC = currentMaxVC;

        string current = _tmp.text;
        if (current == _lastKnownText) return;   // 文本未变，无需处理

        bool hasCustomTag = current.Contains("<shake>") || current.Contains("</shake>")
                         || current.Contains("<wave>")  || current.Contains("</wave>");

        if (!hasCustomTag)
        {
            _waveChars.Clear();
            _shakeChars.Clear();
            _hasEffect     = false;
            _lastKnownText = current;
            return;
        }

        // 1. 先解析范围（raw 含完整标签，可见字符索引计算正确）
        ParseEffectRanges(current);

        // 2. 剥离自定义标签后写回，TMP 只渲染干净内容
        string stripped = StripCustomTags(current);
        _lastKnownText  = stripped;   // 先更新，防止写回时当成新文本再次处理
        _tmp.text       = stripped;
    }

    private static string StripCustomTags(string text)
    {
        return text
            .Replace("<shake>",  "")
            .Replace("</shake>", "")
            .Replace("<wave>",   "")
            .Replace("</wave>",  "");
    }

    /// <summary>
    /// 遍历原始字符串，统计可见字符索引并记录哪些在 shake/wave 标签内。
    /// 所有 TMP 标签（包括自定义标签）均不计入可见字符索引。
    /// </summary>
    private void ParseEffectRanges(string raw)
    {
        _waveChars.Clear();
        _shakeChars.Clear();

        bool inShake = false, inWave = false;
        int visibleIdx = 0;
        int i = 0;

        while (i < raw.Length)
        {
            if (raw[i] == '<')
            {
                int closeIdx = raw.IndexOf('>', i);
                if (closeIdx < 0) { i++; continue; }   // 格式不完整，跳过

                string tag = raw.Substring(i + 1, closeIdx - i - 1).Trim().ToLowerInvariant();

                if      (tag == "shake")  inShake = true;
                else if (tag == "/shake") inShake = false;
                else if (tag == "wave")   inWave  = true;
                else if (tag == "/wave")  inWave  = false;
                // 其他 TMP 标签（color、size 等）照常跳过，不增加 visibleIdx

                i = closeIdx + 1;
            }
            else
            {
                if (inShake) _shakeChars.Add(visibleIdx);
                if (inWave)  _waveChars.Add(visibleIdx);
                visibleIdx++;
                i++;
            }
        }

        _hasEffect = _waveChars.Count > 0 || _shakeChars.Count > 0;
    }

    // ──────────────────────────────────────────────
    //  顶点动画（每帧）
    // ──────────────────────────────────────────────

    private void Update()
    {
        if (_tmp == null) return;
        CheckTextChanged();
        if (_hasEffect) ApplyVertexAnimation();
    }

    private void ApplyVertexAnimation()
    {
        _tmp.ForceMeshUpdate();
        TMP_TextInfo textInfo = _tmp.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            bool doWave  = _waveChars.Contains(i);
            bool doShake = _shakeChars.Contains(i);
            if (!doWave && !doShake) continue;

            int mi = charInfo.materialReferenceIndex;
            int vi = charInfo.vertexIndex;
            Vector3[] verts = textInfo.meshInfo[mi].vertices;

            Vector3 offset = Vector3.zero;

            if (doWave)
                offset.y += Mathf.Sin(Time.time * waveSpeed + i * waveSpacing) * waveAmplitude;

            if (doShake)
            {
                offset.x += Random.Range(-shakeMagnitude, shakeMagnitude);
                offset.y += Random.Range(-shakeMagnitude, shakeMagnitude);
            }

            // 一个字符由 4 个顶点组成，同步偏移
            for (int j = 0; j < 4; j++)
                verts[vi + j] += offset;
        }

        // 将修改后的顶点提交回 mesh
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            _tmp.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}
