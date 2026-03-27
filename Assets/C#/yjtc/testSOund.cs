using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testSOund : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
//#elif UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
            //AudioManager.Instance.PlaySingle("clickSim");
            AudioManager.Instance.PlaySound("clickSim", false);
        }

    }
}
