using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

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
    public Transform targetCoordinateSystem;
    public TrackedObject targetTrackedObject;
    public bool initialized = false;

    public List<Vector3> targetPositions = new List<Vector3>();
    public List<Vector3> transformedTargetPosition = new List<Vector3>();

    public bool useGroundTruth = false;
    private bool wasUsingGroundTruth = false;
    public int index = 0;
    public Vector3 offset = new Vector3(100, 100, 100);

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

        // hide visualization that we show when calibrated
        if (!displayObject.activeInHierarchy)
        {
            displayObject.SetActive(false);
        }

        // cleans the transformation
        targetCoordinateSystem.position = offset;
        targetCoordinateSystem.rotation = Quaternion.identity;

        // reset other variables
        useGroundTruth = false;
        wasUsingGroundTruth = false;

        // different modes require different vizualizations
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

    /// <summary>
    /// This method should be set when the the solver calibrated - use this to hide calibration cube
    /// </summary>
    /// <param name="calibrated"></param>
    public virtual void SetCalibrated()
    {
        // hides calibration target
        HideTarget();

        if (solver && solver.solved)
        {
            // shows visualization of the cube as seen by the AR device after calibration
            if (!displayObject.activeInHierarchy)
            {
                displayObject.SetActive(true);
            }

            //
            // (OLD) Given the position of the tracked object, apply a matrix to it
            //

            //Vector4 hPoint = targetTrackedObject.transform.(local)position;
            //hPoint.w = 1.0f;

            //Vector3 groundTruthResult = solver.groundTruthEquation * hPoint;
            //ector3 manualResult = solver.manualEquation * hPoint;

            // apply rotation and position given calibration
            ApplyMatrixToTargetCoordinateSystem(ref solver.manualEquation);


        }

    }

    protected virtual void ApplyMatrixToTargetCoordinateSystem(ref Matrix4x4 m)
    {
        targetCoordinateSystem.transform.localPosition = Matrix4x4Utils.ExtractTranslationFromMatrix(ref m);
        targetCoordinateSystem.transform.localRotation = Matrix4x4Utils.ExtractRotationFromMatrix(ref m);
    }

    /// <summary>
    /// Hides the calibration target
    /// </summary>
    protected virtual void HideTarget()
    {
        // Also hide the template object
        templateObject.SetActive(false);
    }

    /// <summary>
    /// Displays the calibration target at a new location
    /// </summary>
    protected virtual void DisplayCurrentTarget()
    {
        //switch (solver.sixDoFPattern)
        //        {

        //        }
        templateObject.SetActive(true);
        templateObject.transform.position = transformedTargetPosition[index];
    }

    public virtual Vector3 PerformAlignment()
    {
        Vector3 target = transformedTargetPosition[index];
        index = (index + 1) % transformedTargetPosition.Count;
        DisplayCurrentTarget();
        return target;
    }

    protected virtual void update()
    {
        if (useGroundTruth && !wasUsingGroundTruth)
        {
            wasUsingGroundTruth = true;
            ApplyMatrixToTargetCoordinateSystem(ref solver.groundTruthEquation);

        }

        if (!useGroundTruth && wasUsingGroundTruth)
        {
            wasUsingGroundTruth = false;
            ApplyMatrixToTargetCoordinateSystem(ref solver.manualEquation);

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
