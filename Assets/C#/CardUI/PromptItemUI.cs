using UnityEngine;
using UnityEngine.UI;

public class PromptItemUI : MonoBehaviour
{
    [SerializeField] private Text textText;
    [SerializeField] private Text descriptionText;

    public void SetPromptItem(PromptItem item)
    {
        if (item == null) return;

        SetText(textText, item.text);
        SetText(descriptionText, item.description);
    }

    private static void SetText(Text target, string value)
    {
        if (target != null)
            target.text = string.IsNullOrWhiteSpace(value) ? "" : value;
    }
}
