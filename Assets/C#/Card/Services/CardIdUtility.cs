using System;

public static class CardIdUtility
{
    public static int GetCardType(int id)
    {
        string s = Math.Abs(id).ToString();
        return s.Length >= 1 ? int.Parse(s[0].ToString()) : 0;
    }

    public static int GetCardRarity(int id)
    {
        string s = Math.Abs(id).ToString();
        return s.Length >= 2 ? int.Parse(s[1].ToString()) : 0;
    }
}
