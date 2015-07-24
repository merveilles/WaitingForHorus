Shader "FX/Water" { 
Properties {
	_DesatuScale ("Desat scale", Range (0.0,1.0)) = 0.75
	_AlphaScale ("Alpha scale", Range (0.0,0.175)) = 0.175
	_RefrColor ("Refraction color", COLOR)  = ( .34, .85, .92, 1)
	_Fresnel ("Fresnel (A) ", 2D) = "gray" {}
	_ReflectiveColor ("Reflective color (RGB) fresnel (A) ", 2D) = "" {}
	_ReflectiveColorCube ("Reflective color cube (RGB) fresnel (A)", Cube) = "" { TexGen CubeReflect }
	_ReflectionTex ("Internal Reflection", 2D) = "" {}
}


// -----------------------------------------------------------
// Fragment program cards


Subshader { 
	Tags { "Queue" = "Transparent" "WaterMode"="Refractive" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	AlphaTest Greater .01
	ColorMask RGB
	Cull Back Lighting Off ZWrite Off Fog { Mode Off }
	Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest 

#include "UnityCG.cginc"

struct appdata {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
};

struct v2f {
	float4 pos : SV_POSITION;
	float4 ref : TEXCOORD0;
	float3 viewDir : TEXCOORD1;
	float3 normal : TEXCOORD2;
};

v2f vert(appdata v)
{
	v2f o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
	o.ref = ComputeScreenPos(o.pos);
	o.normal = v.normal;
	
	return o;
}

sampler2D _ReflectionTex;
sampler2D _ReflectiveColor;
sampler2D _Fresnel;
uniform float4 _RefrColor;
uniform float _DesatuScale;
uniform float _AlphaScale;

half4 frag( v2f i ) : COLOR
{
	i.viewDir = normalize(i.viewDir);
	
	// fresnel factor
	half fresnelFac = dot( i.viewDir, i.normal );
	half4 refl = tex2Dproj( _ReflectionTex, UNITY_PROJ_COORD(i.ref) );//lerp( + 0.5, 1.0, _DesatuScale );
	
	half4 color;
	half fresnel = UNITY_SAMPLE_1CHANNEL( _Fresnel, float2(fresnelFac,fresnelFac) );
	color = lerp( 1.0, refl, fresnel );
	
	return float4( refl.xyz, _AlphaScale );
}
ENDCG

	}
}

}
