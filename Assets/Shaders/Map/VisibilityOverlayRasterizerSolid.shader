Shader "Game/Map/VisMapRasterizerSolid" {
	Properties{
	}
	SubShader{
		Tags{ "Queue" = "Transparent" }
		Pass{

		ZWrite Off
		Cull Off

		CGPROGRAM

#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

	struct v2f {
		float4 pos : SV_POSITION;
		float4 color : COLOR;
	};

	v2f vert(appdata_base v, float4 c : COLOR)
	{
		v2f o;
		o.pos = v.vertex;
		o.pos.x = o.pos.x * 2 - 1;
		o.pos.y = -o.pos.y * 2 + 1;
		o.color = c;
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		return i.color;
	}
		ENDCG

	}
	}
}