Shader "Game/Color (Cull None, Depth on)" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
	}
    SubShader {
		Tags {"RenderType"="Opaque"}
        Pass {

			Cull Off
			ZTest Less
			ZWrite On

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
                return o;
            }

			float4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG

        }
    }
}