using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper method that shows/hides an entire transform based on
/// a key press
/// </summary>
public class KeyboardToggle : MonoBehaviour
{

    [Tooltip("This string gets logged every time the key is pressed")]
    public string logName = "KeyboardToggle";

    [Tooltip("What key should be used to toggle")]
    public KeyCode toggleKey;

    [Tooltip("The initial state. If checked, objects are displayed at start / hidden otherwise")]
    public bool startEnabled = true;

    [Tooltip("Toggles all scripts besides this one")]
    public bool toggleMeshes = true;

    [Tooltip("Toggles all scripts besides this one")]
    public bool toggleScripts = false;

    [Tooltip("If true, toggles this script too")]
    public bool affectSelf = false;

    [Tooltip("The current stat of the toggle button")]
    public bool currentState;



    // Start is called before the first frame update
    void Start()
    {
        currentState = startEnabled;
        enableAllMeshes(startEnabled);
        enableAllScripts(startEnabled);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            currentState = !currentState;
            Debug.Log(string.Format("[KeyboardToggle - {0}] Key {1} pressed, toggling {2} to {3}", logName, toggleKey,
               (toggleMeshes ? "meshes" : "") + (toggleScripts && toggleMeshes ? " and " : "") + (toggleScripts ? "scripts" : ""), currentState));

            if (toggleMeshes)
            {
                enableAllMeshes(currentState);
            }

            if (toggleScripts)
            {
                enableAllScripts(currentState);

                if (affectSelf)
                    this.enabled = currentState;
            }


        }
    }

    void enableAllMeshes(bool state)
    {
        foreach (MeshRenderer me in GetComponentsInChildren<MeshRenderer>())
        {
            me.enabled = state;
        }
    }

    void enableAllScripts(bool state)
    {
        foreach (MonoBehaviour script in GetComponentsInChildren<MonoBehaviour>())
        {
            if (script == this && !affectSelf)
                continue;
            
            script.enabled = state;
        }
    }

}
