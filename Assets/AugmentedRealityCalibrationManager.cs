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

    public Transform ARCoordinateSystem;

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


    protected RealWorldCalibrationManager VRSimulation;

    /// <summary>
    /// This method should be invoked by the VR counterpart of the simulator
    /// to create a communication channel between the headset and the VR simulation
    /// </summary>
    /// <param name="solver"></param>
    public void SetRealWorldController(RealWorldCalibrationManager solver)
    {
        this.VRSimulation = solver;
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

    #region Calibration State Machine and User Interaction
    /// <summary>
    /// Changes the current calibration modality
    /// 
    /// If the calibration modality is the same as the current one, then it resets it
    /// </summary>
    /// <param name="p">New calibration modality</param>
    public void ChangeCalibrationModality(CalibrationModality p)
    {
        Debug.Log("[AugmentedRealityCalibrationManager] - Starting evaluation with " + p.ToString());

        // overrides calibration modality
        calibrationModality = p;
        calibrationExperiment.Reset(p.ToString());
        calibrated = false;

        // hide visualization that we show when calibrated
        targetObjectHighlight.SetActive(false);

        // ARCoordinate system follows the offset applied to all the structures in the AR simulation
        ARCoordinateSystem.transform.position = offset;
        ARCoordinateSystem.transform.rotation = Quaternion.identity;

        // This object starts at position (0,0,0) in the simulated space
        targetCoordinateSystemTransform.localPosition = Vector3.zero;
        targetCoordinateSystemTransform.localRotation = Quaternion.identity;

        // reset other variables
        useGroundTruth = false;
        wasUsingGroundTruth = false;

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
    /// Hides the calibration target
    /// </summary>
    protected virtual void HideTarget()
    {
        // Also hide the template object
        virtualCalibrationTarget.SetActive(false);
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
    /// <summary>
    /// Clal this method to hide calibration targetsa dn to apply an existing calibration matrix
    /// </summary>
    /// <param name="calibrated"></param>
    public void SetCalibrated()
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

            wasUsingGroundTruth = false;
            useGroundTruth = false;
            ApplyMatrixToTargetCoordinateSystem(ref calibrationExperiment.manualEquation);
        } else
        {
            Debug.LogWarning("Not calibrated!");
        }

    }

    protected void ApplyMatrixToTargetCoordinateSystem(ref Matrix4x4 m)
    {
        targetCoordinateSystemTransform.transform.localPosition = Matrix4x4Utils.ExtractTranslationFromMatrix(ref m);
        targetCoordinateSystemTransform.transform.localRotation = Matrix4x4Utils.ExtractRotationFromMatrix(ref m);
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

        calibrationExperiment.Calibrate(groundTruthCoordinateSystemTransform.localToWorldMatrix); // todo get remote coordinate system

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





    /// <summary>
    /// Stores the current alignment between real and virtual objects
    /// </summary>
    public void CollectPoints()
    {
        //
        // First - let's deal with collecting the points for each calibration modality
        //
        switch (calibrationModality)
        {
            case CalibrationModality.None:
                Debug.LogError("[AugmentedRealityCalibrationManager] Ignoring command to save alignment because, well, we are not in any alignment mode...");
                break;

            // we align only one point - the gray sphere
            case CalibrationModality.Point:
                calibrationExperiment.PerformAlignment(
                    groundTruthCoordinateSystemTransform.InverseTransformPoint(currentLocationSphere4.position),                     // position of the gray sphere as tracked by OptiTrack - it is a relative position to the ground truth in the simulator, but in real life, it is just the local position
                    targetCoordinateSystemTransform.InverseTransformPoint(virtualSphere4.position),                             // position of the augmented reality sphere as positioned by the system or user
                    targetCoordinateSystemTransform.InverseTransformPoint(groundTruthSphere4.position));
                break;

            // other two modalities calibrate four points at once
            case CalibrationModality.Points:
            case CalibrationModality.Hologram:
                calibrationExperiment.PerformAlignment(
                    groundTruthCoordinateSystemTransform.InverseTransformPoint(currentLocationSphere4.position),                     // position of the gray sphere as tracked by OptiTrack - it is a relative position
                    targetCoordinateSystemTransform.InverseTransformPoint(virtualSphere4.position),                             // position of the augmented reality sphere as positioned by the system or user
                    targetCoordinateSystemTransform.InverseTransformPoint(groundTruthSphere4.position));

                calibrationExperiment.PerformAlignment(
                    groundTruthCoordinateSystemTransform.InverseTransformPoint(currentLocationSphere1.position),                     // position of the gray sphere as tracked by OptiTrack - it is a relative position
                    targetCoordinateSystemTransform.InverseTransformPoint(virtualSphere1.position),                             // position of the augmented reality sphere as positioned by the system or user
                    targetCoordinateSystemTransform.InverseTransformPoint(groundTruthSphere1.position));

                calibrationExperiment.PerformAlignment(
                    groundTruthCoordinateSystemTransform.InverseTransformPoint(currentLocationSphere2.position),                     // position of the gray sphere as tracked by OptiTrack - it is a relative position
                    targetCoordinateSystemTransform.InverseTransformPoint(virtualSphere2.position),                             // position of the augmented reality sphere as positioned by the system or user
                    targetCoordinateSystemTransform.InverseTransformPoint(groundTruthSphere2.position));

                calibrationExperiment.PerformAlignment(
                    groundTruthCoordinateSystemTransform.InverseTransformPoint(currentLocationSphere3.position),                     // position of the gray sphere as tracked by OptiTrack - it is a relative position
                    targetCoordinateSystemTransform.InverseTransformPoint(virtualSphere3.position),                             // position of the augmented reality sphere as positioned by the system or user
                    targetCoordinateSystemTransform.InverseTransformPoint(groundTruthSphere3.position));
                break;

        }

        //
        // Second - let's deal with visualiztaion aspects of the calibration interface
        //

        switch (calibrationModality)
        {
            case CalibrationModality.None:
                break;

            case CalibrationModality.Point:
                index = (index + 1) % virtualTargetPositions.Count; // increment the target indes
                break;

            case CalibrationModality.Points:
                index = (index + 1) % virtualTargetPositions.Count; // increment the target index
                break;

            case CalibrationModality.Hologram:
                // do nothing - this is the VR simulation's part
                break;
        }

        //
        // Third - play sound to let the user know that the point was collected
        //
        if (playAudio)
        {
            audioPlayer.clip = pointCollectedSound;
            audioPlayer.Play();
        }

        //
        // Fourth - Move the virtual calibration target to a new location
        // or calibrate it!
        //
        if (calibrationExperiment.manualAlignments.Count < GetRequiredPointsToCalibrate())
        {
            DisplayCurrentTarget();
        } else
        {
            // got enough points!
            Calibrate();
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

  

    #region HelperMethods
    static string PrintVector(Vector3 v)
    {
        return String.Format("({0,7:000.0000},{1,7:000.0000},{2,7:000.0000})", v.x, v.y, v.z);
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

    #endregion

}
