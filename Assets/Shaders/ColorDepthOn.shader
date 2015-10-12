Shader "Game/Color (Depth on)" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
	}
    SubShader {
		Tags {"RenderType"="Opaque"}
        Pass {

			ZTest Less
			ZWrite On

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float4 pos : SV_POSITION;
				float4 col : COLOR;
            };

            v2f vert (appdata_base v, float4 col : COLOR)
            {
                v2f o;
                o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				o.col = col;
                return o;
            }

			float4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color * i.col;
            }
            ENDCG

        }
    }
}