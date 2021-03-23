using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
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

    public Transform sphere1;
    public Transform sphere2;
    public Transform sphere3;
    public Transform optitrackThingie;
    //public Transform line1;
    //public Transform line2;
    //public Transform line3;

    private PoseInterpolation templateObjectLerper; // let's make this fun, shall we?
    private TrackedObject templateInvisibleTracker; // used for the hologram scenario where the user can move the hologram (not the real object)

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
        displayObject.SetActive(false);
        

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

                // making sure that template is a child of this target manager
                templateObject.transform.parent = this.transform;


                HideTarget();

                // this should not be enabled -> only for holograms
                if (templateInvisibleTracker != null)
                    templateInvisibleTracker.enabled = false;

                // Todo: show alignment!

                

                break;

            case SPAAMSolver.SixDofCalibrationApproach.CubesHead:

                // this should not be enabled -> only for holograms
                if (templateInvisibleTracker != null)
                    templateInvisibleTracker.enabled = false;

                // template should become a child of the headset
                templateObject.transform.parent = this.transform;


                changeVisibility(sphere1, true);
                changeVisibility(sphere2, true);
                changeVisibility(sphere3, true);
                changeVisibility(optitrackThingie, true);
                //changeVisibility(line1, true);
                //changeVisibility(line2, true);
                //changeVisibility(line3, true);


                // initialize variables
                InitializePosition();


                break;

            case SPAAMSolver.SixDofCalibrationApproach.CubesHandHead:

                // this should not be enabled -> only for holograms
                if (templateInvisibleTracker != null)
                    templateInvisibleTracker.enabled = false;

                // making sure that template is a child of this target manager
                templateObject.transform.parent = this.transform;

                changeVisibility(sphere1, false);
                changeVisibility(sphere2, false);
                changeVisibility(sphere3, false);
                changeVisibility(optitrackThingie, false);
                //changeVisibility(line1, false);
                //changeVisibility(line2, false);
                //changeVisibility(line3, false);

                // initialize variables
                InitializePosition();


                break;

            case SPAAMSolver.SixDofCalibrationApproach.CubesHologram:

                // this should not be enabled -> only for holograms
                if (templateInvisibleTracker != null)
                {
                    templateInvisibleTracker.enabled = true;
                } else
                {
                    Debug.LogError("[SPAAMTargetManager] Hologram calibration won't work because template doesn't have a tracker!");
                }


                // shows all spheres 
                changeVisibility(sphere1, true);
                changeVisibility(sphere2, true);
                changeVisibility(sphere3, true);
                changeVisibility(optitrackThingie, true);
                //changeVisibility(line1, true);
                //changeVisibility(line2, true);
                //changeVisibility(line3, true);

                // making sure that template is a child of this target manager
                templateObject.transform.parent = this.transform;

                index = 0;
                initialized = true;
                DisplayCurrentTarget();

                break;
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
        switch (solver.sixDoFPattern)
        {

            // move the template somewhere in front of the camera (and stays there!)
            case SPAAMSolver.SixDofCalibrationApproach.CubesHead:
                templateObject.SetActive(true);
                if (templateObjectLerper != null)
                    templateObjectLerper.LerpPose(transformedTargetPosition[index]);
                else
                    templateObject.transform.position = transformedTargetPosition[index];

                templateObject.transform.localRotation = Quaternion.identity;

                changeVisibility(sphere1, true);
                changeVisibility(sphere2, true);
                changeVisibility(sphere3, true);
                changeVisibility(optitrackThingie, true);
                //changeVisibility(line1, true);
                //changeVisibility(line2, true);
                //changeVisibility(line3, true);
                break;

            // move holograms around
            case SPAAMSolver.SixDofCalibrationApproach.CubesHandHead:
                templateObject.SetActive(true);
                if (templateObjectLerper != null)
                    templateObjectLerper.LerpPose(transformedTargetPosition[index]);
                else
                    templateObject.transform.position = transformedTargetPosition[index];

                templateObject.transform.localRotation = Quaternion.identity;
                changeVisibility(sphere1, false);
                changeVisibility(sphere2, false);
                changeVisibility(sphere3, false);
                changeVisibility(optitrackThingie, false);
                //changeVisibility(line1, false);
                //changeVisibility(line2, false);
                //changeVisibility(line3, false);
                break;


            // move the template somewhere right in front of the user so that they pick it up
            case SPAAMSolver.SixDofCalibrationApproach.CubesHologram:
                templateObject.SetActive(true);

                changeVisibility(sphere1, true);
                changeVisibility(sphere2, true);
                changeVisibility(sphere3, true);
                changeVisibility(optitrackThingie, true);
                //changeVisibility(line1, true);
                //changeVisibility(line2, true);
                //changeVisibility(line3, true);

                // what we really do here is to move a real, invisible object on the VR side
                // this object has a tracker that influences the hologram here, and therefor
                // makes it appear to the user
                break;

        }
        
    }

    public virtual void NextPoint()
    {
        // do nothing when in the first screen
        if (solver.sixDoFPattern == SPAAMSolver.SixDofCalibrationApproach.None)
            return;
        switch (solver.sixDoFPattern)
        {
            // gets the position of the cube (somewhere in front of the headset)
            case SPAAMSolver.SixDofCalibrationApproach.CubesHead:
                index = (index + 1) % targetPositions.Count;
                DisplayCurrentTarget();
                break;

            // basically the position we created a while back
            case SPAAMSolver.SixDofCalibrationApproach.CubesHandHead:
                index = (index + 1) % targetPositions.Count;
                DisplayCurrentTarget();
                break;

            // basically the location of the hologram  (NOTE: Take a look at the SpaamSolver -> This has a special thing happening there)
            case SPAAMSolver.SixDofCalibrationApproach.CubesHologram:
                DisplayCurrentTarget();
                break;

        }

    }


    static public void changeVisibility(Transform t, bool visible)
    {
        // show / hide all meshes
        foreach (MeshRenderer me in t.GetComponentsInChildren<MeshRenderer>())
        {
            me.enabled = visible;
        }

        foreach (LineRenderer le in t.GetComponentsInChildren<LineRenderer>())
        {
            le.enabled = visible;
        }

    }



    /// <summary>
    /// Returns the current location of the target and updates the target to the next position
    /// </summary>
    /// <returns></returns>
    public virtual Vector3 PerformAlignment()
    {
        // do nothing when in the first screen
        if (solver.sixDoFPattern == SPAAMSolver.SixDofCalibrationApproach.None)
            return new Vector3(0,0,0);
        Vector3 target;
        switch (solver.sixDoFPattern)
        {
            // gets the position of the cube (somewhere in front of the headset)
            case SPAAMSolver.SixDofCalibrationApproach.CubesHead:
                target = templateObject.transform.position;
                index = (index + 1) % targetPositions.Count;
                DisplayCurrentTarget();
                break;

            // basically the position we created a while back
            case SPAAMSolver.SixDofCalibrationApproach.CubesHandHead:
                target = templateObject.transform.position;
                index = (index + 1) % targetPositions.Count;
                DisplayCurrentTarget();
                break;

            // basically the location of the hologram  (NOTE: Take a look at the SpaamSolver -> This has a special thing happening there)
            case SPAAMSolver.SixDofCalibrationApproach.CubesHologram:
                target = templateObject.transform.position;
                DisplayCurrentTarget();
                break;

            default:
                target = new Vector3(0, 0, 0);
                break;

        }

        return target;
    }

    /// <summary>
    /// Returns the current location of the target and updates the target to the next position
    /// </summary>
    /// <returns></returns>
    public virtual Tuple<Vector3, Vector3, Vector3, Vector3> PerformAlignmentSpecial()
    {
        // do nothing when in the first screen
        if (solver.sixDoFPattern == SPAAMSolver.SixDofCalibrationApproach.None)
            return new Tuple<Vector3, Vector3, Vector3, Vector3>(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
        Vector3 target;
        switch (solver.sixDoFPattern)
        {
            // gets the position of the cube (somewhere in front of the headset)
            case SPAAMSolver.SixDofCalibrationApproach.CubesHead:
                target = templateObject.transform.position;
                index = (index + 1) % targetPositions.Count;
                DisplayCurrentTarget();
                return new Tuple<Vector3, Vector3, Vector3, Vector3>(target, sphere1.position, sphere2.position, sphere3.position);
                

            // basically the position we created a while back
            case SPAAMSolver.SixDofCalibrationApproach.CubesHandHead:
                target = templateObject.transform.position;
                index = (index + 1) % targetPositions.Count;
                DisplayCurrentTarget();
                return new Tuple<Vector3, Vector3, Vector3, Vector3>(target, sphere1.position, sphere2.position, sphere3.position);
                

            // basically the location of the hologram  (NOTE: Take a look at the SpaamSolver -> This has a special thing happening there)
            case SPAAMSolver.SixDofCalibrationApproach.CubesHologram:
                target = templateObject.transform.position;
                DisplayCurrentTarget();
                return new Tuple<Vector3, Vector3, Vector3, Vector3>(target, sphere1.position, sphere2.position, sphere3.position);
                

            default:
                
                return new Tuple<Vector3, Vector3, Vector3, Vector3>(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
                

        }


    }

    protected virtual void update()
    {
        // update matrix being used
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
        templateObjectLerper = templateObject.GetComponent<PoseInterpolation>();
        templateInvisibleTracker = templateObject.GetComponent<TrackedObject>();
    }

    // Update is called once per frame
    void Update()
    {
        update();
    }
}
