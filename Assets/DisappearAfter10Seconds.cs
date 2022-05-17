using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisappearAfter10Seconds : MonoBehaviour
{

    
    public float time = 10;

 

    // every 2 seconds perform the print()
    private IEnumerator WaitAndDisable(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);
            print("[DisappearAfterNSeconds] " + waitTime + " has passed! Disabling meshes!");
            
            foreach (MeshRenderer me in GetComponentsInChildren<MeshRenderer>())
            {
                me.enabled = false;
            }

            this.enabled = false; // disables itself
        }
    }

    // Update is called once per frame
    void OnEnable()
    {
        StartCoroutine(WaitAndDisable(10));
    }

    private void OnDisable()
    {
        
    }
}
