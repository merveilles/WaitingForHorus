Shader "AEON_Base_Texture" 
{
	Properties  
	{
		_DiffuseAmount ( "Diffuse Amount", Range( 0.0, 1.0 ) ) = 0.25
		_TexAmount ( "Texture Amount", Range( 0.0, 1.0 ) ) = 0.25
		_SpecBoost ( "Specular Amount", Range( 0.0, 2.5 ) ) = 1.0
		_SpecTexAmount ( "Spec Texture Amount", Range( 0.0, 1.0 ) ) = 0.25
		_MainMap ( "Main Texture", 2D ) = "main" {}
		_Normalmap ( "Normalmap", 2D ) = "normal" {}
		_Specmap ( "Specmap", 2D ) = "spec" {}
		_AmbientRim ( "Ambient Rim", Range( 0.0, 1.0 ) ) = 0.0
		_FresnelPower ( "Fresnel Power", Range( 0.0, 4.0 ) ) = 4.0
		_FresnelMult ( "Fresnel Multiplier", Range( 0.0, 5.0 ) ) = 0.75
		_FresnelDilute ( "Spec Main", Range( 0.0, 5.0 ) ) = 0.75
		_PrimaryBlob ( "Primary Blob", Range( 0.0, 1.0 ) ) = 1.0
		_SecondaryBlob ( "Secondary Blob", Range( 0.0, 1.0 ) ) = 0.125
		_Gloss ( "Gloss", Range( 0.0, 2.0 ) ) = 1.0
	}
	    
	SubShader 
	{
        Tags
        {
          "Queue"="Geometry+0"
          "IgnoreProjector"="False"
          "RenderType"="Opaque"
        }

        Cull Back
        ZWrite On
        ZTest LEqual

		CGPROGRAM
		#pragma target 3.0 
		#pragma surface surf SimpleSpecular noambient novertexlights
		//fullforwardshadows approxview dualforward
		
		float _Shininess;
		float _Gloss;
		float _AmbientRim;
		float _FresnelPower;
		float _FresnelMult;
		float _FresnelDilute;
		float _PrimaryBlob;
		float _SecondaryBlob;
		fixed _SpecBoost;
		
		fixed CalculateSpecular( fixed3 lDir, fixed3 vDir, fixed3 norm, fixed spec )
		{	
			float3 halfVector = normalize( lDir + vDir );
			
			float specDot = saturate( dot( halfVector, norm ) );
			float fresnelDot = min( 1.0, dot( vDir, norm ) );
			float rimCore = 1.0 - saturate( fresnelDot );
			
			float rim = pow( rimCore, _FresnelPower );
			rim *= specDot;
			float doubleSpec = ( _SecondaryBlob * pow( specDot, _Gloss * 16.0 ) ) + ( pow( specDot, _Gloss * 128.0 ) * _PrimaryBlob );
			
			return spec * ( ( rim * _AmbientRim ) + ( ( _FresnelMult * rim ) + _FresnelDilute ) * doubleSpec );
		}
		
		fixed4 LightingSimpleSpecular( SurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten ) 
		{
			fixed diff = saturate( dot( s.Normal, lightDir ) );
			fixed spec = CalculateSpecular( lightDir, viewDir, s.Normal, s.Specular );
			
			fixed4 c;
			c.rgb = ( s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec ) * atten;
			
			return c;
		}
		
		fixed4 LightingSimpleSpecular_DirLightmap( SurfaceOutput s, fixed4 color, fixed4 scale, fixed3 viewDir, bool surfFuncWritesNormal, out fixed3 specColor ) 
		{
			UNITY_DIRBASIS
			half3 scalePerBasisVector;
			
			half3 lm = DirLightmapDiffuse( unity_DirBasis, color, scale, s.Normal, surfFuncWritesNormal, scalePerBasisVector );
			half3 lightDir = normalize( scalePerBasisVector.x * unity_DirBasis[0] + scalePerBasisVector.y * unity_DirBasis[1] + scalePerBasisVector.z * unity_DirBasis[2 ]);
			
			specColor = lm * CalculateSpecular( lightDir, viewDir, s.Normal, s.Specular );
			
			return half4( lm * 0.5, 1.0 );
		}
		
		struct Input 
		{
			float2 uv_MainMap;
			float2 uv_Normalmap;
			float2 uv_Specmap;
		};
	
 		sampler2D _MainMap;
	 	sampler2D _Normalmap;
	 	sampler2D _Specmap; 
		float4 _Color;
		fixed _SpecTexAmount; 
		fixed _TexAmount;
		fixed _DiffuseAmount;

		void surf( Input IN, inout SurfaceOutput o ) 
		{
			float tex = lerp( tex2D( _MainMap, IN.uv_MainMap ).r, 1.0, _TexAmount );
			o.Albedo = _DiffuseAmount * tex;// IN.tangent; // Smuggle Tangent in Albedo;

			o.Normal = UnpackNormal( tex2D( _Normalmap, IN.uv_Normalmap ) );
			//o.Gloss = 1.0 * tex; // Smuggle Albedo in Gloss;
			o.Specular = lerp( tex2D( _Specmap, IN.uv_Specmap ).r, 1.0, _SpecTexAmount ) * _SpecBoost;
		}
	
		ENDCG
	} 
	    
	Fallback "Diffuse"
}