Shader "Horus_Base" 
{
	Properties  
	{
		_Color( "Main Color", Color ) = ( 1, 1, 1, 1 )
		_DiffuseAmount ( "Diffuse Amount", Range( 0.0, 0.5 ) ) = 0.25
		_TexAmount ( "Texture Amount", Range( 0.0, 1.0 ) ) = 0.25
		_MainTex ( "Main Texture", 2D ) = "main" {}
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

		void surf( Input IN, inout SurfaceOutput o ) 
		{
			float3 tex = lerp( tex2D( _MainTex, IN.uv_MainTex ).rgb, 1.0, _TexAmount );
			o.Albedo = _Color * tex + _DiffuseAmount;
		}
	
		ENDCG
	} 
	    
	Fallback "Diffuse"
}