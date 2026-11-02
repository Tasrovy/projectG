using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueUIAudio : Singleton<DialogueUIAudio>
{
    [Header("对话相关音效路径常量")]
    [HideInInspector] private const string DialogueClickAudioPath = "Sound/Switch sounds/easyClick";
    [HideInInspector] private const string DialogueOptionClickAudioPath = "Sound/Switch sounds/button4";
    [HideInInspector] private const string SkipDialogueAudioPath = "Sound/Switch sounds/button6";
    [HideInInspector] private const string ChangeSceneAudioPath = "Sound/Switch sounds/button7";

    [Header("额外界面与战斗音效常数")]
    [HideInInspector] private const string StartGameAuidioPath = "Sound/Switch sounds/button5";
    [HideInInspector] private const string CardRewardAudioPath = "Sound/intangibleClick/Collect star 4"; // 三选一卡牌奖励
    [HideInInspector] private const string ShopBuyCardAudioPath = "Sound/intangibleClick/Collect star 2"; // 商店点击购买卡牌
    [HideInInspector] private const string ShopBattleEndAudioPath = "Sound/Switch sounds/button1"; // 商店/卡牌战斗结束按钮
    [HideInInspector] private const string DefaultAudioPath = "Sound/Switch sounds/button9";
    [HideInInspector] private const string CardClickAudioPath = "Sound/Switch sounds/button2"; // 同一音效，开启随机音高


    /// <summary>
    /// 播放“对话系统的对话点击”音效
    /// </summary>
    public void PlayDialogueClick()
    {
        PlayDialogueAudio(DialogueClickAudioPath);
    }

    /// <summary>
    /// 播放“对话选项点击”音效
    /// </summary>
    public void PlayDialogueOptionClick()
    {
        PlayDialogueAudio(DialogueOptionClickAudioPath);
    }

    /// <summary>
    /// 播放“跳过对话”音效
    /// </summary>
    public void PlaySkipDialogue()
    {
        PlayDialogueAudio(SkipDialogueAudioPath);
    }

    /// <summary>
    /// 播放“切换场景播放音效”
    /// </summary>
    public void PlayChangeSceneAudio()
    {
        PlayDialogueAudio(ChangeSceneAudioPath);
    }

    /// <summary>
    /// 播放“开始游戏”音效
    /// </summary>
    public void PlayStartGameAudio()
    {
        PlayDialogueAudio(StartGameAuidioPath);
    }

    /// <summary>
    /// 播放“三选一卡牌奖励”音效
    /// </summary>
    public void PlayCardRewardAudio()
    {
        PlayDialogueAudio(CardRewardAudioPath);
    }

    /// <summary>
    /// 播放“商店点击购买卡牌”音效
    /// </summary>
    public void PlayShopBuyCardAudio()
    {
        PlayDialogueAudio(ShopBuyCardAudioPath);
    }

    /// <summary>
    /// 播放“商店/卡牌战斗结束按钮”音效
    /// </summary>
    public void PlayShopBattleEndAudio()
    {
        PlayDialogueAudio(ShopBattleEndAudioPath);
    }

    /// <summary>
    /// 播放“卡牌点击”音效
    /// </summary>
    public void PlayCardClickAudio()
    {
        PlayDialogueAudio(CardClickAudioPath, true);
    }

    /// <summary>
    /// 播放默认音效
    /// </summary>
    public void PlayDefaultAudio()
    {
        PlayDialogueAudio(DefaultAudioPath);
    }

    /// <summary>
    /// 播放对话系统音效核心方法
    /// </summary>
    /// <param name="path">相对资源路径，可以包含空格</param>
    /// <param name="pitch">是否开启随机音高</param>
    public void PlayDialogueAudio(string path, bool pitch = false)
    {
        // 兼容纠错：因为 SfxTrigger.PlaySingle 底层会拼接 "Sound/" 路径
        // 如果这里填入的常量本身就带 "Sound/"，为了防止变成 "Sound/Sound/..." 找不到资源，先自动裁离它
        if (path.StartsWith("Sound/")) path = path.Substring(6);
        else if (path.StartsWith("Sound\\")) path = path.Substring(6);

        // 参考其他UI代码，直接使用工程内已经写好且支持文件夹与空格的 SfxTrigger
        SfxTrigger.PlaySingle(path, pitch);
    }
}
