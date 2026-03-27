using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using EventSystem;

namespace EventSystem
{
	public class EventManageData
	{
		public EventManageEnum EventManageEnum;
		public List<EventGameFun> list = new List<EventGameFun>();
	}

	public delegate void EventGameFun(Object obj);
}



public class EventManage
{
	static List<EventManageData> eventList = new List<EventManageData>();
	public static void AddEvent(EventManageEnum enumType, EventGameFun eventFun)
	{
		for (int i = 0; i < eventList.Count; i++)
		{
			if (eventList[i].EventManageEnum == enumType)
			{
				eventList[i].list.Add(eventFun);
				return;
			}
		}

		EventManageData data = new EventManageData();
		data.EventManageEnum = enumType;
		data.list.Add(eventFun);

		eventList.Add(data);
	}

	public static void RemoveEvent(EventManageEnum enumType, EventGameFun eventFun)
	{
		for (int i = 0; i < eventList.Count; i++)
		{
			if (eventList[i].EventManageEnum == enumType)
			{
				for (int j = 0; j < eventList[i].list.Count; j++)
				{
					if (eventList[i].list[j] == eventFun)
					{
						eventList[i].list.RemoveAt(j);
						return;
					}
				}
			}
		}
	}

	public static void SendEvent(EventManageEnum enumType, Object obj)
	{
		for (int i = 0; i < eventList.Count; i++)
		{
			if (eventList[i].EventManageEnum == enumType)
			{
				for (int j = eventList[i].list.Count - 1; j > -1; j--)
				{
					if (eventList[i].list[j] != null && !eventList[i].list[j].Target.IsUnityNull())
						eventList[i].list[j](obj);
					else
						eventList[i].list.RemoveAt(j);
				}
				return;
			}
		}
	}

	public static void CheckNullFun()
	{
		for (int i = 0; i < eventList.Count; i++)
		{
			for (int j = eventList[i].list.Count - 1; j > -1; j--)
			{
				if (eventList[i].list[j] == null || eventList[i].list[j].Target == null)
					eventList[i].list.RemoveAt(j);
			}
		}
	}
}
