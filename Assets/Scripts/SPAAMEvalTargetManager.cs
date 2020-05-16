using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SPAAMEvalTargetManager : SPAAMTargetManager
{
    public Vector2Int count;
    public Vector2 dist;
    public Canvas displayCanvas;
    public SpriteRenderer template;
    public List<Vector2> targetPositions2D = new List<Vector2>();
    public List<SpriteRenderer> dots = new List<SpriteRenderer>();
    public DisplayProjection dp;
    public Transform targetSphere;
    public Text textOverlay;
    public bool useGroundTruth = false;
    private SPAAM2DSolver solver;
    private int lastIndex = 0;
    private Vector2 canvasSize;

    public void SetSolver(SPAAM2DSolver solver)
    {
        this.solver = solver;
    }

    public override void InitializePosition()
    {
        index = 0;
        lastIndex = 0;
        initialized = true;
        DisplayCurrentTarget();
    }

    protected override void DisplayCurrentTarget()
    {
        dots[lastIndex].color = new Color(1, 0, 0, 1);
        dots[index].color = new Color(1, 1, 0, 1);
    }

    public override Vector3 PerformAlignment()
    {
        Vector2 target = targetPositions2D[index];
        lastIndex = index;
        index = (index + 1) % dots.Count;
        DisplayCurrentTarget();
        return target;
    }

    void Start()
    {
        RectTransform parentCanvas = displayCanvas.GetComponent<RectTransform>();
        canvasSize = new Vector2(parentCanvas.rect.width / 2, parentCanvas.rect.height / 2);

        Vector2 start = -(count / 2) * dist;
        for (int i = 0; i < count.y; i++)
        {
            for (int j = 0; j < count.x; j++)
            {
                Vector2 pos = start + new Vector2(j*dist.x, i * dist.y);

                targetPositions2D.Add(pos);
            }
        }
        
        for (int i = 0; i < targetPositions2D.Count; i++)
        {
            Vector2 curr = targetPositions2D[i];
            dots.Add(Instantiate(template, displayCanvas.transform));
            dots[i].transform.localPosition = new Vector3(curr.x * canvasSize.x, curr.y * canvasSize.y, 0);
        }
        template.gameObject.SetActive(false);
    }


    void Update()
    {
        if (solver && solver.solved)
        {
            textOverlay.gameObject.SetActive(true);
            textOverlay.transform.parent = template.transform;
            template.color = new Color(0,0,1,1);
            template.gameObject.SetActive(true);
            Vector4 hPoint = camera.transform.InverseTransformPoint(targetSphere.position);
            hPoint.w = 1.0f;
            Vector3 groundTruthResult = solver.groundTruthEquation * hPoint;
            groundTruthResult.x = groundTruthResult.x / groundTruthResult.z * canvasSize.x; //
            groundTruthResult.y = groundTruthResult.y / groundTruthResult.z * canvasSize.y; //
            Vector3 manualResult = solver.manualEquation * hPoint;
            manualResult.x = manualResult.x / manualResult.z * canvasSize.x; //
            manualResult.y = manualResult.y / manualResult.z * canvasSize.y; //

            if (useGroundTruth)
            {
                template.transform.localPosition = new Vector3(groundTruthResult.x, groundTruthResult.y);
            }
            else
            {
                template.transform.localPosition = new Vector3(manualResult.x, manualResult.y);
            }

            Debug.Log("Error:" + (groundTruthResult - manualResult).magnitude);
        }
    }
}
