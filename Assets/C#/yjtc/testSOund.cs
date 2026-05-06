using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testSOund : MonoBehaviour
{

    public int cardId;

    [Header("测试摄像机放大功能")]
    public float posX;
    public float posY;
    public float multiple;
    public float lastTime;

    [Header("测试角色大小功能")]
    public string objectName;
    public int size;
    public float Ypoint;
    public float chaLastTime;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {

            //GetCardDataById
            //GetFieldValue
            CardManager.Instance.AddCard(cardId, 1);


        }
        if(Input.GetKeyDown(KeyCode.A))
        {
            CharacterControl.ResetArtZoomStatic(lastTime);
        }

        if(Input.GetKeyDown(KeyCode.Z))
        {
            CharacterControl.ZoomArtStatic(posX,posY,multiple,lastTime);
        }

        if(Input.GetKeyDown(KeyCode.X))
        {
            CharacterControl.SetCharacterSizeStatic(objectName,size,Ypoint,chaLastTime);
        }
    }
}
