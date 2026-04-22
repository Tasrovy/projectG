using System;
using System.Reflection;

public sealed class CardEffectInvoker
{
    private readonly CardEffectLibrary _library;

    public CardEffectInvoker(CardEffectLibrary library)
    {
        _library = library;
    }

    public void Execute(string methodName, object[] parameters)
    {
        MethodInfo method = _library.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (method == null) return;

        try
        {
            method.Invoke(_library, parameters);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e);
        }
    }

    public object[] ConvertParameters(string methodName, string[] stringArgs)
    {
        MethodInfo method = _library.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (method == null) return null;

        ParameterInfo[] paramInfos = method.GetParameters();
        object[] result = new object[paramInfos.Length];

        for (int i = 0; i < paramInfos.Length; i++)
        {
            if (i < stringArgs.Length)
            {
                try
                {
                    result[i] = Convert.ChangeType(stringArgs[i].Trim(), paramInfos[i].ParameterType);
                }
                catch
                {
                    result[i] = paramInfos[i].HasDefaultValue ? paramInfos[i].DefaultValue : null;
                }
            }
            else
            {
                result[i] = paramInfos[i].HasDefaultValue ? paramInfos[i].DefaultValue : null;
            }
        }

        return result;
    }
}
