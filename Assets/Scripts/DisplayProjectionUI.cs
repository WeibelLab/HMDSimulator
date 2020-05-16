using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayProjectionUI : DisplayProjection
{
    public Texture2D texture;
    //void OnRenderImage(RenderTexture src, RenderTexture dest)
    //{
    //    if (!texture)
    //    {
    //        texture = new Texture2D(src.width, src.height);

    //    }
    //    else if (texture.width != src.width || texture.height != src.height)
    //    {
    //        texture.Resize(src.width, src.height);
    //    }

    //    //cam.projectionMatrix = oldProj;
    //    RenderTexture old = RenderTexture.active;
    //    RenderTexture.active = src;
    //    //don't forget that you need to specify rendertexture before you call readpixels
    //    //otherwise it will read screen pixels.
    //    texture.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
    //    //Color[] colors = texture.GetPixels();
    //    for (int i = 0; i < 10; i++)
    //    {
    //        for (int j = 0; j < 10; j++)
    //        {
    //            //colors[(src.height / 2 - 5 + i) * src.height + (src.width / 2 - 5 + j)] = new Color(0, 1, 0, 1);
    //            texture.SetPixel((src.width / 2 - 5 + i), src.height / 2 - 5 + j, new Color(0, 1, 0, 1));
    //        }
    //    }

    //    //texture.SetPixels(colors);
    //    texture.Apply();
    //    Graphics.Blit(texture, dest);
    //    RenderTexture.active = old; //don't forget to set it back to null once you finished playing with it.
    //}

    

    // Start is called before the first frame update
    void Awake()
    {
        }

    // Update is called once per frame
    void Update()
    {
        
    }
}
