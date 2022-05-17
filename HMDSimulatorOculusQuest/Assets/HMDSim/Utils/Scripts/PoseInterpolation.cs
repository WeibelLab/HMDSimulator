using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This behavior moves and rotates an object
/// Based on the code available here: https://gamedevbeginner.com/the-right-way-to-lerp-in-unity-with-examples/
/// </summary>
public class PoseInterpolation : MonoBehaviour
{

    bool lerpingPosition = false;
    bool lerpingRotation = false;
    bool stopLerping = false;

    public bool isLerping = false;

    public void StartLerping(Vector3 positionToMoveTo, Quaternion targetRotation, float duration = 0.5f)
    {
        StartCoroutine(LerpRotation(targetRotation, duration));
        StartCoroutine(LerpPosition(positionToMoveTo, duration));
    }

    public void LerpPose(Vector3 positionToMoveTo, float duration = 0.5f)
    {
        StartCoroutine(LerpPosition(positionToMoveTo, duration));
    }


    public void StopLerping()
    {
        if (lerpingPosition || lerpingRotation)
            stopLerping = true;
    }

    IEnumerator LerpRotation(Quaternion endValue, float duration)
    {
        lerpingRotation = true;
        float time = 0;
        Quaternion startValue = transform.rotation;

        while (time < duration && !stopLerping)
        {
            transform.rotation = Quaternion.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        lerpingRotation = false;
        transform.rotation = endValue;

        if (!lerpingRotation && !lerpingPosition)
        {
            isLerping = false;
            stopLerping = false;
        }
    }

    IEnumerator LerpPosition(Vector3 targetPosition, float duration)
    {
        lerpingPosition = true;
        float time = 0;
        Vector3 startPosition = transform.position;

        while (time < duration && !stopLerping)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        lerpingPosition = false;
        transform.position = targetPosition;


        if (!lerpingRotation && !lerpingPosition)
        {
            isLerping = false;
            stopLerping = false;
        }
    }


}
