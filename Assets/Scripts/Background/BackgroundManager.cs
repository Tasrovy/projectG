using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundManager : Singleton<BackgroundManager>
{
    [SerializeField] private Image backgroundImage;

    public void SetBackground(string backgroundName)
    {
        if (string.IsNullOrWhiteSpace(backgroundName))
        {
            Debug.LogError("Background name is null or empty.");
            return;
        }

        if (backgroundImage == null)
        {
            var backgroundObject = GameObject.Find("Background");
            if (backgroundObject != null)
            {
                backgroundImage = backgroundObject.GetComponent<Image>();
            }
        }

        if (backgroundImage == null)
        {
            Debug.LogError("UI Image named 'Background' was not found in scene.");
            return;
        }

        Sprite newBackground = Resources.Load<Sprite>($"Background/{backgroundName}");
        if (newBackground == null)
        {
            Debug.LogError($"Background sprite not found at Resources/Background/{backgroundName}.");
            return;
        }

        backgroundImage.sprite = newBackground;
    }
}
