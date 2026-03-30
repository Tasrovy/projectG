using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testSOund : MonoBehaviour
{

    public int cardId;
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

    }
}
