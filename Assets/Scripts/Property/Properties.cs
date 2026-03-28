using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Properties : Singleton<Properties>
{
    public int property1;
    public int property2;
    public int property3;
    public int money;

    public void SetProperties(int p1, int p2, int p3, int m)
    {
        property1 = p1;
        property2 = p2;
        property3 = p3;
        money = m;
    }

    public void ClearProperties()
    {
        property1 = 0;
        property2 = 0;
        property3 = 0;
        money = 0;
    }
}
