using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    private static MainManager _instance;

    public static MainManager Instance { get { return _instance; } }
    
    public bool sceneReady = false;
    public TrackerManager trackerManager;
    AsyncOperation[] ops = new AsyncOperation[2];

    // Start is called before the first frame update
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }

        _instance = this;
        
        ops[0] = SceneManager.LoadSceneAsync("RealWorld", LoadSceneMode.Additive);
        ops[1] = SceneManager.LoadSceneAsync("ARWorld", LoadSceneMode.Additive);

        DontDestroyOnLoad(this.gameObject);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!sceneReady)
        {
            if (ops[0].isDone && ops[1].isDone)
            {
                sceneReady = true;
                trackerManager.UpdateTrackers();
            }
        }
        else
        {
            
        }
    }
}
