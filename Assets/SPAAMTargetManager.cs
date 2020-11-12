using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This class controls the simulation on the AR side of the simulator
/// 
/// This SPAAMTargetManager focuses on solving 4x4 matrices (Rotation + Translation)
/// equations
/// 
/// </summary>
public class SPAAMTargetManager : MonoBehaviour
{

    private static SPAAMTargetManager _instance;
    public static SPAAMTargetManager Instance { get { return _instance; } }

    public GameObject camera;
    public GameObject templateObject;
    public GameObject displayObject;
    public TrackedObject targetTrackedObject;
    public bool initialized = false;

    public List<Vector3> targetPositions = new List<Vector3>();
    public List<Vector3> transformedTargetPosition = new List<Vector3>();

    public bool useGroundTruth = false;
    public int index = 0;
    protected Vector3 offset = new Vector3(100, 100, 100);

    protected SPAAMSolver solver;
    public void SetSolver(SPAAMSolver solver)
    {
        this.solver = solver;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }

        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public virtual void InitializePosition()
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

    public void ConditionChange(SPAAMSolver.SixDofCalibrationApproach p)
    {
        Debug.Log("[SPAAMTargetManager] - Starting evaluation with SixDofCalibrationApproach " + p.ToString());
        switch (p)
        {

            case SPAAMSolver.SixDofCalibrationApproach.None:
                HideTarget();
                break;
            default:
                InitializePosition();
                break;
                // fornow, nothing to do with either

        }

        
    }

    protected virtual void HideTarget()
    {
        templateObject.SetActive(false);
    }

    protected virtual void DisplayCurrentTarget()
    {
        templateObject.SetActive(true);
        templateObject.transform.position = transformedTargetPosition[index];
    }

    public virtual Vector3 PerformAlignment()
    {
        Vector3 target = transformedTargetPosition[index] - offset;
        index = (index + 1) % transformedTargetPosition.Count;
        DisplayCurrentTarget();
        return target;
    }

    protected virtual void update()
    {
        if (solver && solver.solved)
        {
            if (!displayObject.activeInHierarchy)
            {
                displayObject.SetActive(true);
            }

            Vector4 hPoint = targetTrackedObject.transform.position;
            hPoint.w = 1.0f;

            Vector3 groundTruthResult = solver.groundTruthEquation * hPoint;
            Vector3 manualResult = solver.manualEquation * hPoint;

            if (useGroundTruth)
            {
                displayObject.transform.position = groundTruthResult + offset;
            }
            else
            {
                displayObject.transform.position = manualResult + offset;
            }

            Debug.Log("[SPAAMTargetManager] Error:" + (groundTruthResult - manualResult).magnitude);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        update();
    }
}
