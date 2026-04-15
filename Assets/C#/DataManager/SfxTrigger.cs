using UnityEngine;

public static class SfxTrigger
{
    /// <summary>
    /// 触发UI音效。若path是文件夹，则随机一个clip后交给AudioManager播放。
    /// </summary>
    public static void PlaySound(string path, bool randomPitch = true)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string resolvedPath = ResolveSoundPath(path);
        AudioManager.Instance.PlaySound(resolvedPath, randomPitch);
    }

    /// <summary>
    /// 触发单次音效。若path是文件夹，则随机一个clip后交给AudioManager播放。
    /// </summary>
    public static void PlaySingle(string path, bool randomPitch = false)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string resolvedPath = ResolveSoundPath(path);
        AudioManager.Instance.PlaySingle(resolvedPath, randomPitch);
    }

    private static string ResolveSoundPath(string path)
    {
        // 先按文件路径检查
        AudioClip clip = Resources.Load<AudioClip>($"Sound/{path}");
        if (clip != null)
        {
            return path;
        }

        // 文件不存在时，尝试按文件夹加载并随机一个clip
        AudioClip[] clips = Resources.LoadAll<AudioClip>($"Sound/{path}");
        if (clips != null && clips.Length > 0)
        {
            int index = Random.Range(0, clips.Length);
            return $"{path}/{clips[index].name}";
        }

        // 兜底回传原始路径，让AudioManager保持原有报错行为
        return path;
    }
}
