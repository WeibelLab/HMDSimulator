using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class controls the simulation on the AR side of the simulator
/// 
/// This AugmentedRealityCalibrationManager focuses on solving 4x4 matrices (Rotation + Translation)
/// equations
/// 
/// </summary>
public class AugmentedRealityCalibrationManager : MonoBehaviour
{

    public enum CalibrationModality
    {
        None,
        Point,             // calibrate aligning on point at a time
        Points,            // calibrate alignment several points at once
        Hologram,           // similiar to points, but the user moves a hologram instead of a physical target
        Pattern_Count
    }

    private static AugmentedRealityCalibrationManager _instance;
    public static AugmentedRealityCalibrationManager Instance { get { return _instance; } }
    public Vector3 offset = new Vector3(100, 100, 100);

    [Header("Current state")]
    public bool initialized = false;
    public bool useGroundTruth = false;
    public bool calibrated = false;
    public CalibrationModality calibrationModality;

    [Header("User interaction")]
    [Tooltip("Used of the hologram scenario as the user hand in VR is tracked to move objects in AR")]
    private TrackedObject VRUserInteractionTracker;           // used for the hologram scenario where the user can move the hologram (not the real object)
    public Transform virtualSpheresHandle;
    public GameObject virtualCalibrationTarget;
    private PoseInterpolation virtualCalibrationTargetLerper; // let's make this fun, shall we?

    [Header("AR Objects used in calibration")]
    public GameObject targetObjectHighlight;
    public Transform virtualSphere1;
    public Transform virtualSphere2;
    public Transform virtualSphere3;
    public Transform virtualSphere4;

    [Header("Coordinate system being calibrated")]
    public Transform targetCoordinateSystemTransform;
    public Transform currentLocationSphere1;
    public Transform currentLocationSphere2;
    public Transform currentLocationSphere3;
    public Transform currentLocationSphere4;

    [Header("Coordinate system using for ground truth")]
    public Transform groundTruthCoordinateSystemTransform;
    public Transform groundTruthSphere1;
    public Transform groundTruthSphere2;
    public Transform groundTruthSphere3;
    public Transform groundTruthSphere4;

    [Header("Calibration visualization")]
    [Tooltip("Camera used to present information to the user")]
    public GameObject ARCamera;
    [Tooltip("The location of where the targets should show up - encoded in the ARCamera coordinate system")]
    public List<Vector3> virtualTargetPositions = new List<Vector3>();
    [HideInInspector]
    public List<Vector3> transformedTargetPosition = new List<Vector3>();


    private bool wasUsingGroundTruth = false;
    public int index = 0;

    [Header("User interaction details")]

    // Below we have elements that can help us run a smooth user study
    [Header("Audio feedback")]
    public bool playAudio = true;
    public AudioClip pointCollectedSound;
    public AudioClip calibrationDoneSound;
    public AudioClip newCalibrationApproachSound;
    public AudioSource audioPlayer;

    [Header("Voice feedback (Generic)")]
    public bool playVoiceFeedback = true;
    public AudioClip voiceOverResultsSaved;
    public AudioClip voiceOverCalibrated;
    public AudioClip voiceOverCollectMorePoints;
    public AudioSource audioPlayerForVoiceOver;

    [Header("Calibration data structures")]
    public AugmentedReality3D3DCalibrationExperiment calibrationExperiment;


    protected RealWorldCalibrationManager solver;
    public void SetSolver(RealWorldCalibrationManager solver)
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



    public void ConditionChange(CalibrationModality p)
    {
        Debug.Log("[AugmentedRealityCalibrationManager] - Starting evaluation with " + p.ToString());

        // overrides calibration modality
        calibrationModality = p;
        calibrationExperiment.Reset(p.ToString());
        calibrated = false;

        // hide visualization that we show when calibrated
        targetObjectHighlight.SetActive(false);

        // cleans the transformation
        targetCoordinateSystemTransform.position = offset;
        targetCoordinateSystemTransform.rotation = Quaternion.identity;

        // reset other variables
        useGroundTruth = false;
        wasUsingGroundTruth = false;

        // makes sure that virtual calibration target is in the coordinate system it wants to calibrate
        virtualCalibrationTarget.transform.SetParent(targetCoordinateSystemTransform);

        // different modes require different vizualizations
        switch (calibrationModality)
        {

            case CalibrationModality.None:

                HideTarget();

                // this should not be enabled -> only for holograms
                if (VRUserInteractionTracker != null)
                    VRUserInteractionTracker.enabled = false;

                calibrationExperiment.CalibrationModalityName = "None";

                break;


            case CalibrationModality.Point:


                // this should not be enabled -> only for holograms
                if (VRUserInteractionTracker != null)
                    VRUserInteractionTracker.enabled = false;


                changeVisibility(virtualSphere1, false);
                changeVisibility(virtualSphere2, false);
                changeVisibility(virtualSphere3, false);
                changeVisibility(virtualSpheresHandle, false);

                // initialize variables
                InitializePosition();


                break;

            case CalibrationModality.Points:


                // this should not be enabled -> only for holograms
                if (VRUserInteractionTracker != null)
                    VRUserInteractionTracker.enabled = false;


                changeVisibility(virtualSphere1, true);
                changeVisibility(virtualSphere2, true);
                changeVisibility(virtualSphere3, true);
                changeVisibility(virtualSpheresHandle, true);

                // initialize variables
                InitializePosition();


                break;


            case CalibrationModality.Hologram:

                // this should not be enabled -> only for holograms
                if (VRUserInteractionTracker != null)
                {
                    VRUserInteractionTracker.enabled = true;
                }
                else
                {
                    Debug.LogError("[AugmentedRealityCalibrationManager] Hologram calibration won't work because template doesn't have a tracker!");
                }


                // shows all spheres 
                changeVisibility(virtualSphere1, true);
                changeVisibility(virtualSphere2, true);
                changeVisibility(virtualSphere3, true);
                changeVisibility(virtualSpheresHandle, true);

                // making sure that template is a child of this target manager
                virtualCalibrationTarget.transform.parent = this.transform;

                index = 0;
                initialized = true;
                DisplayCurrentTarget();

                break;
        }

        // plays an audio telling the user that something happened
        if (calibrationModality != CalibrationModality.None)
        {
            audioPlayer.clip = newCalibrationApproachSound;
            audioPlayer.Play();
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

        if (calibrationExperiment.FullCalibrationStored)
        {
            // shows visualization of the cube as seen by the AR device after calibration
            if (!targetObjectHighlight.activeInHierarchy)
            {
                targetObjectHighlight.SetActive(true);
            }

            ApplyMatrixToTargetCoordinateSystem(ref calibrationExperiment.manualEquation);
        }

    }

    protected virtual void ApplyMatrixToTargetCoordinateSystem(ref Matrix4x4 m)
    {
        targetCoordinateSystemTransform.transform.localPosition = Matrix4x4Utils.ExtractTranslationFromMatrix(ref m);
        targetCoordinateSystemTransform.transform.localRotation = Matrix4x4Utils.ExtractRotationFromMatrix(ref m);
    }

    /// <summary>
    /// Hides the calibration target
    /// </summary>
    protected virtual void HideTarget()
    {
        // Also hide the template object
        virtualCalibrationTarget.SetActive(false);
    }

    /// <summary>
    /// Displays the calibration target at a new location
    /// </summary>
    protected virtual void DisplayCurrentTarget()
    {
        switch (calibrationModality)
        {

            // move the template somewhere in front of the ARCamera (and stays there!)
            case CalibrationModality.Points:
                virtualCalibrationTarget.SetActive(true);
                if (virtualCalibrationTargetLerper != null)
                {
                    virtualCalibrationTargetLerper.StartLerping(transformedTargetPosition[index], UnityEngine.Random.rotation);

                }
                else
                {
                    virtualCalibrationTarget.transform.position = transformedTargetPosition[index];
                    virtualCalibrationTarget.transform.rotation = UnityEngine.Random.rotation;
                }

                //virtualCalibrationTarget.transform.localRotation = Quaternion.identity;

                changeVisibility(virtualSphere1, true);
                changeVisibility(virtualSphere2, true);
                changeVisibility(virtualSphere3, true);
                changeVisibility(virtualSpheresHandle, true);
                break;

            // move holograms around
            case CalibrationModality.Point:
                virtualCalibrationTarget.SetActive(true);
                if (virtualCalibrationTargetLerper != null)
                    virtualCalibrationTargetLerper.LerpPose(transformedTargetPosition[index]);
                else
                    virtualCalibrationTarget.transform.position = transformedTargetPosition[index];

                virtualCalibrationTarget.transform.localRotation = Quaternion.identity;
                changeVisibility(virtualSphere1, false);
                changeVisibility(virtualSphere2, false);
                changeVisibility(virtualSphere3, false);
                changeVisibility(virtualSpheresHandle, false);
                break;


            // move the template somewhere right in front of the user so that they pick it up
            case CalibrationModality.Hologram:
                virtualCalibrationTarget.SetActive(true);

                changeVisibility(virtualSphere1, true);
                changeVisibility(virtualSphere2, true);
                changeVisibility(virtualSphere3, true);
                changeVisibility(virtualSpheresHandle, true);

                // what we really do here is to move a real, invisible object on the VR side
                // this object has a tracker that influences the hologram here, and therefor
                // makes it appear to the user
                break;

        }

    }

    /// <summary>
    /// Displays the next point the user should interact with
    /// </summary>
    public virtual void NextPoint()
    {
        // do nothing when in the first screen
        if (calibrationModality == CalibrationModality.None)
            return;
        switch (calibrationModality)
        {
            // gets the position of the cube (somewhere in front of the headset)
            case CalibrationModality.Point:
            case CalibrationModality.Points:
                index = (index + 1) % virtualTargetPositions.Count;
                DisplayCurrentTarget();
                break;

            // basically the location of the hologram
            // (NOTE: Take a look at the RealWorldCalibrationManager -> it is moving the location
            // of the physical target on its own)
            case CalibrationModality.Hologram:
                DisplayCurrentTarget();
                break;
        }

    }

    virtual public bool Calibrate()
    {
        if (calibrationModality == CalibrationModality.None)
        {
            Debug.LogWarning("[AugmentedRealityCalibrationManager] Won't calibrate / solve when no modes are set!");
            return false;
        }

        // warns user that more points are needed
        if (calibrationExperiment.manualAlignments.Count < GetRequiredPointsToCalibrate())
        {
            audioPlayerForVoiceOver.clip = voiceOverCollectMorePoints;
            audioPlayerForVoiceOver.Play();
            return false;
        }

        calibrationExperiment.Calibrate(Matrix4x4.identity); // todo get remote coordinate system

        if (playAudio)
        {
            audioPlayer.clip = calibrationDoneSound;
            audioPlayer.Play();
        }

        if (playVoiceFeedback)
        {
            audioPlayerForVoiceOver.clip = voiceOverCalibrated;
            audioPlayerForVoiceOver.Play();
        }

        // change what the user sees in the AR world
        SetCalibrated();
        calibrated = true;
        return true;
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
        if (solver.sixDoFPattern == RealWorldCalibrationManager.SixDofCalibrationApproach.None)
            return new Vector3(0, 0, 0);
        Vector3 target;
        switch (solver.sixDoFPattern)
        {
            // gets the position of the cube (somewhere in front of the headset)
            case RealWorldCalibrationManager.SixDofCalibrationApproach.CubesHead:
                target = virtualCalibrationTarget.transform.position;
                index = (index + 1) % virtualTargetPositions.Count;
                DisplayCurrentTarget();
                break;

            // basically the position we created a while back
            case RealWorldCalibrationManager.SixDofCalibrationApproach.CubesHandHead:
                target = virtualCalibrationTarget.transform.position;
                index = (index + 1) % virtualTargetPositions.Count;
                DisplayCurrentTarget();
                break;

            // basically the location of the hologram  (NOTE: Take a look at the SpaamSolver -> This has a special thing happening there)
            case RealWorldCalibrationManager.SixDofCalibrationApproach.CubesHologram:
                target = virtualCalibrationTarget.transform.position;
                DisplayCurrentTarget();
                break;

            default:
                target = new Vector3(0, 0, 0);
                break;

        }

        return target;
    }


    /// <summary>
    /// Stores the current alignment between real and virtual objects
    /// </summary>
    public virtual void SaveCurrentAlignment()
    {
        switch (calibrationModality)
        {
            case CalibrationModality.None:
                Debug.LogError("[AugmentedRealityCalibrationManager] Ignoring command to save alignment because, well, we are not in any alignment mode...");
                break;

            // we align only one point - the gray sphere
            case CalibrationModality.Point:
                
                break;
        }

        // play sound
        if (playAudio)
        {
            audioPlayer.clip = pointCollectedSound;
            audioPlayer.Play();
        }
    }

    /// <summary>
    /// Returns the current location of the target and updates the target to the next position
    /// </summary>
    /// <returns></returns>
    public virtual Tuple<Vector3, Vector3, Vector3, Vector3> PerformAlignmentSpecial()
    {
        // do nothing when in the first screen
        if (solver.sixDoFPattern == RealWorldCalibrationManager.SixDofCalibrationApproach.None)
            return new Tuple<Vector3, Vector3, Vector3, Vector3>(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);

        switch (solver.sixDoFPattern)
        {
            // gets the position of the cube (somewhere in front of the headset)
            case RealWorldCalibrationManager.SixDofCalibrationApproach.CubesHead:
                index = (index + 1) % virtualTargetPositions.Count;
                DisplayCurrentTarget();
                return new Tuple<Vector3, Vector3, Vector3, Vector3>(virtualSphere4.position, virtualSphere1.position, virtualSphere2.position, virtualSphere3.position);


            // basically the position we created a while back
            case RealWorldCalibrationManager.SixDofCalibrationApproach.CubesHandHead:
                index = (index + 1) % virtualTargetPositions.Count;
                DisplayCurrentTarget();
                return new Tuple<Vector3, Vector3, Vector3, Vector3>(virtualSphere4.position, virtualSphere1.position, virtualSphere2.position, virtualSphere3.position);


            // basically the location of the hologram  (NOTE: Take a look at the SpaamSolver -> This has a special thing happening there)
            case RealWorldCalibrationManager.SixDofCalibrationApproach.CubesHologram:
                DisplayCurrentTarget();
                return new Tuple<Vector3, Vector3, Vector3, Vector3>(virtualSphere4.position, virtualSphere1.position, virtualSphere2.position, virtualSphere3.position);
            default:
                return new Tuple<Vector3, Vector3, Vector3, Vector3>(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);


        }


    }


    /// <summary>
    /// Returns the number of points required for calibration
    /// (RealWorldCalibrationManager implementations should override this class)
    /// </summary>
    /// <returns></returns>
    public virtual int GetRequiredPointsToCalibrate()
    {
        switch (calibrationModality)
        {
            case CalibrationModality.Point:
                return 25;

            case CalibrationModality.Points:
                return 36;

            case CalibrationModality.Hologram:
                return 36;
        }

        return 9;
    }

    // Update is called once per frame
    void Update()
    {
        update();       // other classes inherit from AugmentedRealityCalibrationManager
    }

    protected virtual void update()
    {
        // update matrix being used
        if (useGroundTruth && !wasUsingGroundTruth)
        {
            wasUsingGroundTruth = true;
            ApplyMatrixToTargetCoordinateSystem(ref calibrationExperiment.groundTruthEquation);
        }

        if (!useGroundTruth && wasUsingGroundTruth)
        {
            wasUsingGroundTruth = false;
            ApplyMatrixToTargetCoordinateSystem(ref calibrationExperiment.manualEquation);
        }
    }



    #region Virtual Target Display and user interaction
    /// <summary>
    /// Called every time there is a condition / state change so that the location
    /// of the targets are accurate with respect to the HMD
    /// </summary>
    public virtual void InitializePosition()
    {
        transformedTargetPosition.Clear();
        for (int i = 0; i < virtualTargetPositions.Count; i++)
        {
            transformedTargetPosition.Add(ARCamera.transform.TransformPoint(virtualTargetPositions[i]));
        }
        index = 0;
        initialized = true;
        DisplayCurrentTarget();
    }


    // Start is called before the first frame update
    void Start()
    {
        virtualCalibrationTargetLerper = virtualCalibrationTarget.GetComponent<PoseInterpolation>();
        VRUserInteractionTracker = virtualCalibrationTarget.GetComponent<TrackedObject>();
    }

    #endregion


    #region HelperMethods
    static string PrintVector(Vector3 v)
    {
        return String.Format("({0,7:000.0000},{1,7:000.0000},{2,7:000.0000})", v.x, v.y, v.z);
    }
    #endregion

}
