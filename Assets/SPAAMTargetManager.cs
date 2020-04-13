using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPAAMTargetManager : MonoBehaviour
{

    private static SPAAMTargetManager _instance;
    public static SPAAMTargetManager Instance { get { return _instance; } }

    public GameObject camera;
    public GameObject templateObject;
    public bool initialized = false;

    public List<Vector3> targetPositions = new List<Vector3>();
    public List<Vector3> transformedTargetPosition = new List<Vector3>();

    private int index = 0;
    private Vector3 offset = new Vector3(100, 100, 100);

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }

        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void InitializePosition()
    {
        transformedTargetPosition.Clear();
        for (int i = 0; i < targetPositions.Count; i++)
        {
            transformedTargetPosition.Add(camera.transform.TransformPoint(targetPositions[i]));
        }
        index = 0;
        initialized = true;
        DisplayCurrentTarget();
    }

    void DisplayCurrentTarget()
    {
        templateObject.SetActive(true);
        templateObject.transform.position = transformedTargetPosition[index];
    }

    public Vector3 PerformAlignment()
    {
        Vector3 target = transformedTargetPosition[index] - offset;
        index = (index + 1) % transformedTargetPosition.Count;
        DisplayCurrentTarget();
        return target;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
