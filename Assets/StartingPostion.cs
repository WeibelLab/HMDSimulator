using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

public class StartingPostion : MonoBehaviour
{
    public GameObject StartingPosition;
    public GameObject head;
    public bool lockPosition = true;
    public bool lockAll = true;

    // Start is called before the first frame update
    void Start()
    {
        
        transform.position = StartingPosition.transform.position - InputTracking.GetLocalPosition(XRNode.CenterEye);
    }

    // Update is called once per frame
    void Update()
    {
        if (lockPosition)
        {
            transform.position = StartingPosition.transform.position - InputTracking.GetLocalPosition(XRNode.CenterEye);
        }

        if (lockAll)
        {
            transform.position = StartingPosition.transform.position;
            SteamVR.settings.trackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;
            Valve.VR.OpenVR.System.ResetSeatedZeroPose();
            Valve.VR.OpenVR.Compositor.SetTrackingSpace(Valve.VR.ETrackingUniverseOrigin
                .TrackingUniverseSeated);
        }
    }
}
