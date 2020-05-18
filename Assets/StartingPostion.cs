using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

public class StartingPostion : MonoBehaviour
{
    public GameObject StartingPosition;
    public GameObject head;
    public bool lockPosition = true;
    public bool lockAll = true;

    private bool init = false;
    private int countDown = 10;

    // Start is called before the first frame update
    void Start()
    {

    }

    void LateUpdate()
    {
        if (!init)
        {
            if (countDown < 0)
            {
                transform.position = StartingPosition.transform.position - InputTracking.GetLocalPosition(XRNode.CenterEye);
                init = true;
            }
            else
            {
                countDown--;
            }
        }
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
