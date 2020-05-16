using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlitUIWithScene : MonoBehaviour
{
    public RenderTexture scene;
    public Camera cam;
    public Material alphaBlending;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(scene, dest);
        Graphics.Blit(src, dest, alphaBlending);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
