using System.Text.RegularExpressions;

public sealed class CardDescriptionFormatter
{
    public string Format(Card card)
    {
        if (card == null || string.IsNullOrEmpty(card.description))
        {
            return string.Empty;
        }

        return Regex.Replace(card.description, @"\{([^}]+)\}", match =>
        {
            if (match.Success && match.Groups.Count > 1)
            {
                return GetFieldValue(card, match.Groups[1].Value);
            }

            return match.Value;
        });
    }

    private string GetFieldValue(Card card, string fieldName)
    {
        switch (fieldName.ToLower())
        {
            case "id": return card.id.ToString();
            case "dialog": return card.dialog.ToString();
            case "nature1": return card.nature1.ToString();
            case "nature2": return card.nature2.ToString();
            case "nature3": return card.nature3.ToString();
            case "name": return card.name ?? string.Empty;
            case "sale": return card.sale.ToString();
            case "made": return card.made ?? string.Empty;
            case "broken": return card.broken ?? string.Empty;
            case "added": return card.added ?? string.Empty;
            case "buff": return card.buff ?? string.Empty;
            case "trigger": return card.trigger ?? string.Empty;
            case "nextturn": return card.nextTurn ?? string.Empty;
            case "description": return card.description ?? string.Empty;
            default: return string.Empty;
        }
    }
}
