using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class CharacterControl : MonoBehaviour
{
    [Header("抖动参数")]
    [SerializeField]
    [Header("抖动时长")] private float shakeDuration = 0.5f;
    [SerializeField] 
    [Header("抖动幅度")] private float shakeMagnitude = 0.1f;

    [YarnCommand("set_character_shake")]
    public void SetCharacterShake(string characterName, string shakeType)
    {
        var manager = GetComponent<CharacterHighlightManager>();
        if (manager != null)
        {
            var ch = manager.characters.Find(c => string.Equals(c.characterName, characterName, System.StringComparison.Ordinal));
            if (ch != null && ch.spriteRenderer != null)
            {
                StartCoroutine(ShakeRoutine(ch.spriteRenderer.transform, shakeDuration, shakeMagnitude, shakeType));
            }
            else
            {
                Debug.LogWarning($"[CharacterControl] 找不到名为'{characterName}'的角色配置，或未赋予SpriteRenderer！");
            }
        }
        else
        {
            Debug.LogWarning("[CharacterControl] 未在父物体找到 CharacterHighlightManager 组件！");
        }
    }

    [YarnCommand("set_character_sprite")]
    public void SetCharacterSprite(string characterName, string emotion)
    {
        ChangeCharacterSprite(characterName, emotion);
    }

    public void ChangeCharacterSprite(string characterName, string emotion)
    {
        var manager = GetComponentInParent<CharacterHighlightManager>();
        if (manager != null)
        {
            var ch = manager.characters.Find(c => string.Equals(c.characterName, characterName, System.StringComparison.Ordinal));
            if (ch != null)
            {
                if (ch.emotionSprites != null)
                {
                    var targetState = ch.emotionSprites.Find(s => string.Equals(s.emotion, emotion, System.StringComparison.OrdinalIgnoreCase));
                    if (targetState != null && targetState.sprite != null && ch.spriteRenderer != null)
                    {
                        ch.spriteRenderer.sprite = targetState.sprite;
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterControl] 未找到感情 '{emotion}' 的差分精灵图，或 SpriteRenderer/图片 未赋值！");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[CharacterControl] 未在管理器中找到名为 '{characterName}' 的角色配置！");
            }
        }
        else
        {
            Debug.LogWarning("[CharacterControl] 未在父物体找到 CharacterHighlightManager 组件！");
        }
    }

    private IEnumerator ShakeRoutine(Transform targetTransform, float duration, float magnitude, string shakeType)
    {
        Vector3 originalPos = targetTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = 0f;
            float y = 0f;

            if (shakeType == "up_down")
            {
                y = Random.Range(-1f, 1f) * magnitude;
            }
            else if (shakeType == "left_right")
            {
                x = Random.Range(-1f, 1f) * magnitude;
            }
            else
            {
                // 如果传入其他的，默认全方向抖动
                x = Random.Range(-1f, 1f) * magnitude;
                y = Random.Range(-1f, 1f) * magnitude;
            }

            targetTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetTransform.localPosition = originalPos;
    }

    #region 音频功能
    /// <summary>
    /// 读取标识符并播放音效
    /// 格式要求：sfx_charactername_soundtype 或 #sfx_charactername_soundtype
    /// </summary>
    public void PlayAudioFromTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        // 去除可能的 '#' 符号
        if (tag.StartsWith("#"))
        {
            tag = tag.Substring(1);
        }

        // 以 sfx_ 开头则认为是音效标签
        if (tag.StartsWith("sfx_"))
        {
            // 提取 sfx_ 之后的所有内容作为音效名称
            string audioName = tag.Substring(4);
            
            if (AudioManager.Instance != null)
            {
                // 通过 AudioManager 播放单次独占音效
                AudioManager.Instance.PlaySound(audioName);
            }
            else
            {
                Debug.LogWarning("[CharacterControl] 找不到 AudioManager 实例！");
            }
        }
    }
    #endregion
}
