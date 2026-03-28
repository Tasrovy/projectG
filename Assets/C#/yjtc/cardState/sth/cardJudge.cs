using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cardJudge : MonoBehaviour
{
    //单例模式
    private static cardJudge _instance;
    public static cardJudge Instance;




    RectTransform rectTransform;
    private Vector3[] worldCorners = new Vector3[4];
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            Instance = _instance;
            DontDestroyOnLoad(this.gameObject);
        }


        EventManage.AddEvent(EventManageEnum.ACardDragging, showMe);
        EventManage.AddEvent(EventManageEnum.ACardOutDragging, hideMe);
        rectTransform = GetComponent<RectTransform>();
        print("添加成功");
        hideMe(1);
    }

    void hideMe(object none)
    {
            this.gameObject.SetActive(false);
    }
    void showMe(object none)
    {

        this.gameObject.SetActive(true);
    }
    public bool IsMouseInsideUsingCorners()
    {
        // 获取四个角的世界坐标（顺序：左下、左上、右上、右下）
        rectTransform.GetWorldCorners(worldCorners);

        Vector2 mousePos = Input.mousePosition;

        // 方法1：使用矩形边界判定
        float minX = worldCorners[0].x;
        float maxX = worldCorners[2].x;
        float minY = worldCorners[0].y;
        float maxY = worldCorners[1].y;

        return mousePos.x >= minX && mousePos.x <= maxX &&
               mousePos.y >= minY && mousePos.y <= maxY;
    }
}
