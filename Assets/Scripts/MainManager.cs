﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainManager : MonoBehaviour
{
    private static MainManager _instance;

    public static MainManager Instance { get { return _instance; } }

    public string scenePrefix = "";
    public string[] sceneNames = new string[2];

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

        sceneNames[0] = scenePrefix + "Real";
        sceneNames[1] = scenePrefix + "AR";

        ops[0] = SceneManager.LoadSceneAsync(sceneNames[0], LoadSceneMode.Additive);
        ops[1] = SceneManager.LoadSceneAsync(sceneNames[1], LoadSceneMode.Additive);

        DontDestroyOnLoad(this.gameObject);
    }

    void PerformUpdate()
    {
        if (!sceneReady)
        {
            if (ops[0].isDone && ops[1].isDone)
            {
                sceneReady = true;
                trackerManager.UpdateTrackers(sceneNames[0], sceneNames[1]);
            }
        }
        else
        {

        }
    }

    void Update()
    {
        PerformUpdate();
    }

}
