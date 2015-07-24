Shader "Horus_Base" 
{
	Properties  
	{
		_Color( "Main Color", Color ) = ( 1, 1, 1, 1 )
		_DiffuseAmount ( "Diffuse Addition", Range( 0.0, 0.5 ) ) = 0.25
		_TexAmount ( "Texture Amount", Range( 0.0, 1.0 ) ) = 0.25
		_MainTex ( "Main Texture", 2D ) = "main" {}
		
		_Contrast ( "Contrast", Range( 0.75, 1.25 ) ) = 1.0
		_Saturation ( "Saturation", Range( 0.75, 1.25 ) ) = 1.0
		_Brightness ( "Brightness", Range( 0.75, 1.5 ) ) = 1.0
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
		//#pragma target 3.0 
		#pragma surface surf Simple
        sampler2D _MainTex;
        
		float3 ContrastSaturationBrightness( float3 color, float brt, float sat, float con )
		{
			// Increase or decrease theese values to adjust r, g and b color channels seperately
			const float AvgLumR = 0.5;
			const float AvgLumG = 0.5;
			const float AvgLumB = 0.5;
			
			const float3 LumCoeff = float3( 0.2125, 0.7154, 0.0721 );
			
			float3 AvgLumin = float3( AvgLumR, AvgLumG, AvgLumB );
			float3 brtColor = color * brt;
			float3 intensity = float3( dot( brtColor, LumCoeff ) );
			float3 satColor = lerp( intensity, brtColor, sat );
			float3 conColor = lerp( AvgLumin, satColor, con );
			return conColor;
		}
        
		fixed4 LightingSimple( SurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten ) 
		{
			fixed diff = saturate( dot( s.Normal, lightDir ) );
			
			fixed4 c;
			c.rgb = ( s.Albedo * _LightColor0.rgb * diff ) * atten;
			c.a = 1.0;
			
			return c;
		}
        
		struct Input 
		{ 
			float4 color : COLOR;
			float2 uv_MainTex;
		};

		fixed3 _Color;
		fixed _TexAmount;
		fixed _DiffuseAmount;
		
		fixed _Saturation;
		fixed _Brightness;
		fixed _Contrast;

		void surf( Input IN, inout SurfaceOutput o ) 
		{
			float3 tex = lerp( tex2D( _MainTex, IN.uv_MainTex ).rgb, 0.5, _TexAmount );
			o.Albedo = ContrastSaturationBrightness ( _Color * tex + _DiffuseAmount, _Brightness, _Saturation, _Contrast );
		}
	
		ENDCG
	}
	
	Fallback "Diffuse"
}