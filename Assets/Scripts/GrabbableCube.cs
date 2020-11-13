using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using Valve.VR;

public class GrabbableCube : GrabbableObject
{

    public bool CanGrab = true;
    public bool GoesBackToInitialPosition = false;

    //private Transform originalParent = null;
    private ParentConstraint constraint;

    Vector3 initialPosition;
    Quaternion initialRotation;

    PoseInterpolation lerper;

    // Start is called before the first frame update
    void Start()
    {
        //originalParent = transform.parent;
        constraint = GetComponent<ParentConstraint>();

        initialPosition = this.transform.position;
        initialRotation = this.transform.rotation;

        lerper = this.GetComponent<PoseInterpolation>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void Grab(Transform hand)
    {
        if (CanGrab)
        {

            // stops lerping because of the mid-air catch
            if (lerper != null && lerper.isLerping)
                lerper.StopLerping();

            ConstraintSource cs = new ConstraintSource();
            cs.sourceTransform = hand;
            cs.weight = 1;
            constraint.AddSource(cs);
            Vector3 positionOffset = hand.InverseTransformPoint(transform.position);
            Quaternion rotationOffset = Quaternion.Inverse(hand.rotation) * transform.rotation;
            constraint.SetTranslationOffset(0, positionOffset);
            constraint.SetRotationOffset(0, rotationOffset.eulerAngles);
            constraint.constraintActive = true;
        } 
    }

    public override void Release(Transform hand)
    {
        if (constraint.sourceCount > 0)
        {
            constraint.RemoveSource(0);
        }

        constraint.constraintActive = false;

        
        if (GoesBackToInitialPosition)
        {
            if (lerper == null)
            { 
                this.transform.position = initialPosition;
                this.transform.rotation = initialRotation;
            } else
            {
                lerper.StartLerping(initialPosition, initialRotation);
            }
        }
    }
}
