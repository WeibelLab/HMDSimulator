using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CalibrationEvalTargetManager manages the AR side of the calibration
/// 
/// (Given that the VR side needs to access it to communicate what changes it should apply
/// to the headset, this object is a singleton (see SPAAMTargetManager implementation) )
/// </summary>
public class CalibrationEvalTargetManager : SPAAMTargetManager
{
    public Vector2 dist;
    public List<Vector2> targetPositions2D = new List<Vector2>();
    public Transform targetSphere;
    public Transform targetCrosshair;
    public Transform calibrationArea;

    public Canvas[] displayCanvas;
    public SpriteRenderer[] template;
    public SpriteRenderer[] displayTemplate;
    public DisplayProjection[] dp;
    public Text[] textOverlay;
    public List<SpriteRenderer>[] dots = new List<SpriteRenderer>[2];

    private int[] lastIndex = new int[2];
    private Vector2[] canvasSize = new Vector2[2];

    private int side = 0;
    private int other = 1;
    public int[] indices = new int[2];
    public Vector3[] targetPositionsOutput = new Vector3[2];
    public Transform[] eyeEst = new Transform[2];
    private CalibrationEvaluation.CalibrationApproach pattern;

    public float testVal = 0;

    public override void InitializePosition()
    {
        indices[0] = 0;
        lastIndex[0] = 0;
        indices[1] = 0;
        lastIndex[1] = 0;
        side = 0;
        other = 1;
        initialized = true;
        DisplayCurrentTarget();
    }

    protected override void DisplayCurrentTarget()
    {
        if (pattern == CalibrationEvaluation.CalibrationApproach.SPAAM || pattern == CalibrationEvaluation.CalibrationApproach.Depth_SPAAM)
        {
            dots[side][lastIndex[side]].color = new Color(1, 0, 0, 1);
            dots[side][indices[side]].color = new Color(1, 1, 0, 1);
            dots[other][lastIndex[other]].color = new Color(1, 0, 0, 1);
        }
        else if(pattern == CalibrationEvaluation.CalibrationApproach.Stereo_SPAAM || pattern == CalibrationEvaluation.CalibrationApproach.Stylus_mark)
        {
            //dots[0][lastIndex[side]].color = new Color(1, 0, 0, 1);
            //dots[0][indices[side]].color = new Color(1, 1, 0, 1);
            //dots[1][lastIndex[side]].color = new Color(1, 0, 0, 1);
            //dots[1][indices[side]].color = new Color(1, 1, 0, 1);
            targetCrosshair.localPosition = new Vector3(targetPositions2D[indices[side]].x / 5.0f, targetPositions2D[indices[side]].y / 5.0f, ((CalibrationEvaluation)solver).currentDist);
        }
    }

    public override Vector3 PerformAlignment()
    {
        Vector2 target = targetPositions2D[indices[side]];
        if (pattern == CalibrationEvaluation.CalibrationApproach.SPAAM || pattern == CalibrationEvaluation.CalibrationApproach.Depth_SPAAM)
        {
            lastIndex[side] = indices[side];
            indices[side] = (indices[side] + 1) % dots[side].Count;
            side = side == 0 ? 1 : 0;
            other = other == 0 ? 1 : 0;
            DisplayCurrentTarget();
        }
        else
        {
            for (int i = 0; i < 2; i++)
            {
                //targetPositionsOutput[i] = targetPositions2D[indices[i]];
                lastIndex[i] = indices[i];
                indices[i] = (indices[i] + 1) % targetPositions2D.Count;
            }
            Vector3 ret = targetCrosshair.position - new Vector3(100, 100, 100);
            DisplayCurrentTarget();
            return ret;
        }

        return target;
    }

    public void ConditionChange(CalibrationEvaluation.CalibrationApproach p)
    {

        Debug.Log("[CalibrationEvalTargetManager] - Starting evaluation with pattern " + p.ToString());
        switch (p)
        {
            case CalibrationEvaluation.CalibrationApproach.None:
                targetCrosshair.gameObject.SetActive(false);
                SwitchTargetPosition(false);
                break;


            case CalibrationEvaluation.CalibrationApproach.SPAAM:

                targetCrosshair.gameObject.SetActive(false);
                SwitchTargetPosition(false);
                break;
            case CalibrationEvaluation.CalibrationApproach.Depth_SPAAM:

                targetCrosshair.gameObject.SetActive(false);
                SwitchTargetPosition(true);
                break;
            case CalibrationEvaluation.CalibrationApproach.Stereo_SPAAM:
                //calibrationArea.GetComponent<TrackedObject>().enabled = false;

                targetCrosshair.gameObject.SetActive(true);
                SwitchTargetPosition(true, true);
                break;
            case CalibrationEvaluation.CalibrationApproach.Stylus_mark:

                targetCrosshair.gameObject.SetActive(true);
                //calibrationArea.GetComponent<TrackedObject>().enabled = true;
                SwitchTargetPosition(true, true);
                break;
        }

        pattern = p;
        

        InitializePosition();
    }

    void SwitchTargetPosition(bool depth, bool size = false)
    {
        targetPositions2D.Clear();
        if (!depth)
        {
            // Figure 3: condition - original SPAAM
            targetPositions2D.Add(new Vector2(0, 0)); // 1
            targetPositions2D.Add(new Vector2(-dist.x, 0)); // 2
            targetPositions2D.Add(new Vector2(dist.x, 0)); // 3
            targetPositions2D.Add(new Vector2(0, dist.y)); // 4
            targetPositions2D.Add(new Vector2(-dist.x, dist.y)); // 5
            targetPositions2D.Add(new Vector2(dist.x, dist.y)); // 6
            targetPositions2D.Add(new Vector2(0, -dist.y)); // 7
            targetPositions2D.Add(new Vector2(-dist.x, -dist.y)); // 8
            targetPositions2D.Add(new Vector2(dist.x, -dist.y)); // 9
        }
        else
        {
            // Figure 4: condition - magic SPAAM
            targetPositions2D.Add(new Vector2(0, dist.y)); // 1
            targetPositions2D.Add(new Vector2(-dist.x, -dist.y)); // 2
            targetPositions2D.Add(new Vector2(dist.x, 0)); // 3
            targetPositions2D.Add(new Vector2(dist.x, -dist.y)); // 4
            targetPositions2D.Add(new Vector2(0, 0)); // 5
            targetPositions2D.Add(new Vector2(-dist.x, dist.y)); // 6
            targetPositions2D.Add(new Vector2(-dist.x, 0)); // 7
            targetPositions2D.Add(new Vector2(dist.x, dist.y)); // 8
            targetPositions2D.Add(new Vector2(0, -dist.y)); // 9
        }

        foreach (var dot in dots[0])
        {
            Destroy(dot);
        }

        foreach (var dot in dots[1])
        {
            Destroy(dot);
        }

        dots[0].Clear();
        dots[1].Clear();

        template[0].gameObject.SetActive(true);
        template[1].gameObject.SetActive(true);

        float baseSize = 1.0f;
        float step = 0.1f;

        float baseGap = 0.0f;
        float gapStep = 0.001f;

        for (int i = 0; i < targetPositions2D.Count; i++)
        {
            Vector2 curr = targetPositions2D[i];

            //dots[0].Add(Instantiate(template[0], displayCanvas[0].transform));

            //dots[1].Add(Instantiate(template[1], displayCanvas[1].transform));

            if (size)
            {
                //float oldSize = dots[0][i].transform.localScale.x;
                //float newSize = oldSize * (baseSize - i * step);
                //dots[0][i].transform.localPosition = new Vector3((curr.x - i * gapStep) * canvasSize[0].x, curr.y * canvasSize[0].y, 0);
                //dots[1][i].transform.localPosition = new Vector3((curr.x + i * gapStep) * canvasSize[1].x, curr.y * canvasSize[1].y, 0);

                //dots[0][i].transform.localScale = new Vector3(newSize, newSize, 1);
                //dots[1][i].transform.localScale = new Vector3(newSize, newSize, 1);
            }
            else
            {
                dots[0].Add(Instantiate(template[0], displayCanvas[0].transform));

                dots[1].Add(Instantiate(template[1], displayCanvas[1].transform));

                dots[0][i].transform.localPosition = new Vector3((curr.x) * canvasSize[0].x, curr.y * canvasSize[0].y, 0);
                dots[1][i].transform.localPosition = new Vector3((curr.x) * canvasSize[1].x, curr.y * canvasSize[1].y, 0);

            }
        }

        template[0].gameObject.SetActive(false);
        template[1].gameObject.SetActive(false);
    }

    void Start()
    {
        RectTransform parentCanvas = displayCanvas[0].GetComponent<RectTransform>();
        canvasSize[0] = new Vector2(parentCanvas.rect.width / 2, parentCanvas.rect.height / 2);

        parentCanvas = displayCanvas[1].GetComponent<RectTransform>();
        canvasSize[1] = new Vector2(parentCanvas.rect.width / 2, parentCanvas.rect.height / 2);

        dots[0] = new List<SpriteRenderer>();
        dots[1] = new List<SpriteRenderer>();

        SwitchTargetPosition(false);

    }

    protected override void update()
    {
        
        if (solver && solver.solved)
        {
            for (int side = 0; side < 2; side++)
            {
                textOverlay[side].gameObject.SetActive(true);
                textOverlay[side].transform.parent = displayTemplate[side].transform;
                displayTemplate[side].color = new Color(0, 0, 1, 1);
                displayTemplate[side].gameObject.SetActive(true);
                Vector4 hPoint = camera.transform.InverseTransformPoint(targetSphere.position);
                hPoint.w = 1.0f;
                Vector3 groundTruthResult = ((CalibrationEvaluation) solver).groundTruthEquationBoth[side] * hPoint;
                groundTruthResult.x = groundTruthResult.x / groundTruthResult.z * canvasSize[side].x; //
                groundTruthResult.y = groundTruthResult.y / groundTruthResult.z * canvasSize[side].y; //
                Vector3 manualResult = ((CalibrationEvaluation)solver).manualEquationBoth[side] * hPoint;
                manualResult.x = manualResult.x / manualResult.z * canvasSize[side].x; //
                manualResult.y = manualResult.y / manualResult.z * canvasSize[side].y; //

                //hPoint = dp[side].transform.InverseTransformPoint(targetSphere.position);
                ////hPoint.x += dp[side].localPos.x;
                //Vector3 manualResult = dp[side].newProj * hPoint;
                //if (side == 0)
                //{
                //    Debug.Log(manualResult);
                //}

                //manualResult.x = (manualResult.x / -manualResult.z) * canvasSize[side].x; //
                //manualResult.y = (manualResult.y / -manualResult.z) * canvasSize[side].y; //

                //// === Without matrix
                //Vector3 direction = targetSphere.position - eyeEst[side].position;

                //Vector3 dirToLen = dp[side].transform.InverseTransformDirection(direction.normalized);
                //dirToLen /= dirToLen.z;
                //dirToLen *= Mathf.Abs(eyeEst[side].localPosition.z);
                ////Debug.Log(dirToLen);
                //Vector3 manualResult = dirToLen + eyeEst[side].localPosition;

                //manualResult.x /= (0.0568f / 2.0f);
                //manualResult.y /= 0.015f;
                ////Debug.Log(manualResult);

                //manualResult.x = (manualResult.x) * canvasSize[side].x; //
                //manualResult.y = (manualResult.y) * canvasSize[side].y; //



                if (useGroundTruth)
                {
                    displayTemplate[side].transform.localPosition = new Vector3(groundTruthResult.x, groundTruthResult.y);
                }
                else
                {
                    displayTemplate[side].transform.localPosition = new Vector3(manualResult.x, manualResult.y);
                }

                Debug.Log("Error:" + (groundTruthResult - manualResult).magnitude);
                ((CalibrationEvaluation)solver).SetError(manualResult - groundTruthResult, side);
            }
        }
        else
        {
            displayTemplate[0].gameObject.SetActive(false);
            displayTemplate[1].gameObject.SetActive(false);
        }
    }

    void Update()
    {
        update();
    }
}
