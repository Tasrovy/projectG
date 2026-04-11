using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CardFightSpeek : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private TextMeshProUGUI speakText;

    [Header("对话语录配置")]
    [TextArea(1, 4)]
    [SerializeField] private List<string> sentences = new List<string>();

    private int lastIndex = -1; // 记录上一句的索引，防止连续重复

    /// <summary>
    /// 触发一次随机喊话，保证不与上一次重复
    /// </summary>
    public void SpeakRandom()
    {
        if (speakText == null)
        {
            Debug.LogWarning("[CardFightSpeek] 未绑定TextMeshPro的UI！");
            return;
        }

        if (sentences == null || sentences.Count == 0)
        {
            return; // 暂无语录
        }

        // 如果只配了一句话，就只能一直说这一句不需要去重了
        if (sentences.Count == 1)
        {
            speakText.text = sentences[0];
            return;
        }

        // 核心防重复逻辑：一直随，直到与上次不同为止
        int randomIndex = lastIndex;
        while (randomIndex == lastIndex)
        {
            randomIndex = Random.Range(0, sentences.Count);
        }

        // 应用文本并记录索引
        lastIndex = randomIndex;
        speakText.text = sentences[randomIndex];
    }
}
