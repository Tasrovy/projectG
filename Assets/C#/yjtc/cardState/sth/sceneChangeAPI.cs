using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneChangeAPI : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void changeScene(int type)

    {

        SceneType sceneType = (SceneType)type;
        UISceneManager.Instance.SwitchToScene(sceneType);
    }


}
