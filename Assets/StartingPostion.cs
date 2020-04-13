using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartingPostion : MonoBehaviour
{
    public GameObject StartingPosition;
    public GameObject head;

    // Start is called before the first frame update
    void Start()
    {
        transform.position = StartingPosition.transform.position + new Vector3(head.transform.localPosition.x, 0, head.transform.localPosition.y) * -1;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
