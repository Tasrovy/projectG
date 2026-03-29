using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSum : MonoBehaviour
{

    //µ¥ÀýÄ£Ê½
    private static CardSum _instance;
    public static CardSum Instance;
    public RectTransform firstCardPos;
    public float cardInterval = 0.2f;
    public float cardRise = 0.02f;

    public bool dragingCard=false;

    public bool cardPileActive=false;

    public bool selectCarding=false;

    public GameObject selectedObj = null;
    void Awake()
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
    }

    public List<GameObject> HandCardList = new List<GameObject>();
    public List<GameObject> DeckCardList = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
