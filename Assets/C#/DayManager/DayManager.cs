using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayManager : Singleton<DayManager>
{
    public int dayNumber = 1;

    public void NextDay()
    {
        dayNumber++;
    }
    
    public int GetDayNumber()=>dayNumber;
}
