Shader "Game/Map/VisibilityOverlay" {
	Properties{
		_Mask("Mask", 2D) = "white" {}
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }




	 //Fog of war pass
		Pass{
		//Offset 0, -1
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

	struct v2f {
		float4 pos : SV_POSITION;
		float2 maskTexCoord : TEXCOORD0;
	};

	sampler2D _Mask;

	v2f vert(float4 vertex : POSITION, float2 uv0 : TEXCOORD0)
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, vertex);
		o.maskTexCoord = uv0;
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		return float4(0, 0, 0, tex2D(_Mask, i.maskTexCoord).r);
	}
		ENDCG
	}//Pass



	}
}