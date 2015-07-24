// Upgrade NOTE: replaced 'PositionFog()' with multiply of UNITY_MATRIX_MVP by position
// Upgrade NOTE: replaced 'V2F_POS_FOG' with 'float4 pos : SV_POSITION'

Shader "Rim Light" {

    

    Properties {

        _Color ("Main Color", Color) = (1,1,1,1)

        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)

        _MainTex ("Base (RGB)", 2D) = "white" {}

    }

    

    SubShader {

		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" } 
		Blend SrcAlpha OneMinusSrcAlpha 

        

        Pass {

            

            CGPROGRAM

                

                #pragma vertex vert

                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata {

                    float4 vertex : POSITION;

                    float3 normal : NORMAL;

                    float2 texcoord : TEXCOORD0;

                };

                

                struct v2f {

                    float4 pos : SV_POSITION;

                    float2 uv : TEXCOORD0;

                    float3 color : COLOR;

                };

                

                uniform float4 _MainTex_ST;

                uniform float4 _RimColor;

                

                v2f vert (appdata_base v) {

                    v2f o;

                    o.pos = mul (UNITY_MATRIX_MVP, v.vertex);

                    

                    float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));

                    float dotProduct = 1 - dot(v.normal, viewDir);

                    float rimWidth = 0.2;

                    o.color = smoothstep(1 - rimWidth, 1.0, dotProduct);

                    

                    o.color *= _RimColor;

                    

                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                    

                    return o;

                }

                

                uniform sampler2D _MainTex;

                uniform float4 _Color;

                

                float4 frag(v2f i) : COLOR {

                    float4 texcol = tex2D(_MainTex, i.uv);

                    texcol *= _Color;

                    texcol.rgb += i.color;

                    return texcol;

                }

                

            ENDCG

        }

    }

}