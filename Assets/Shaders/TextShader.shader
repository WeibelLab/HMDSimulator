// from https://answers.unity.com/questions/44598/text-mesh-font-material-renders-on-top-of-everythi.html?page=2&pageSize=5&sort=votes by marcel_z
Shader "Custom/TextShader"
{
	Properties{
		_MainTex("Font Texture", 2D) = "white" {}
		_Color("Text Color", Color) = (1,1,1,1)
	}

		SubShader{
			Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
			Lighting Off Cull Off ZWrite Off Fog { Mode Off }
			Blend SrcAlpha OneMinusSrcAlpha
			Pass {
				Color[_Color]
				SetTexture[_MainTex] {
					combine primary, texture * primary
				}
			}
		}
}