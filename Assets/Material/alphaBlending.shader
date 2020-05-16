Shader "Custom/alphaBlending"
{
  Properties{
      _MainTex("Texture to blend", 2D) = "black" {}
  }
    SubShader{
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            SetTexture[_MainTex] { combine texture }
        }
  }
}
