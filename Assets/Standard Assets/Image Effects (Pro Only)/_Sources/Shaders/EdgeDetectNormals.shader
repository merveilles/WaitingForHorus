
Shader "Hidden/EdgeDetectGeometry" { 
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
	}
	
Subshader {
 Pass {
	  ZTest Always Cull Off ZWrite Off
	  Fog { Mode off }      

      CGPROGRAM
	#pragma target 3.0 
	#pragma glsl
      #pragma vertex vertThin
      #pragma fragment fragThin

	#include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv[5] : TEXCOORD0;
	}; 
	
	sampler2D _MainTex;
	uniform half4 _MainTex_TexelSize;
	sampler2D _CameraDepthNormalsTexture;
	
	uniform half4 sensitivity; 
	uniform half4 _BgColor;
	uniform half _BgFade;
	
	v2f vertThin( appdata_img v )
	{
		v2f o;
		o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
		
		float2 uv = v.texcoord.xy;
		o.uv[0] = uv;
		
		// On D3D when AA is used, the main texture & scene depth texture
		// will come out in different vertical orientations.
		// So flip sampling of depth texture when that is the case (main texture
		// texel size will have negative Y)
		
		#if SHADER_API_D3D9
		if (_MainTex_TexelSize.y < 0)
			uv.y = 1-uv.y;
		#endif
		
		o.uv[1] = uv;

		return o;
	}	

	const float gx = 0.3535533845424652;
	const float gy = 0.5;
	const float gz = 0.1666666716337204;
	const float gw = 0.3333333432674408;
	 
	float3x3 G[9];
	const float3x3 g0 = float3x3( 0.3535533845424652, 0, -0.3535533845424652, 0.5, 0, -0.5, 0.3535533845424652, 0, -0.3535533845424652 );
	const float3x3 g1 = float3x3( 0.3535533845424652, 0.5, 0.3535533845424652, 0, 0, 0, -0.3535533845424652, -0.5, -0.3535533845424652 );
	const float3x3 g2 = float3x3( 0, 0.3535533845424652, -0.5, -0.3535533845424652, 0, 0.3535533845424652, 0.5, -0.3535533845424652, 0 );
	const float3x3 g3 = float3x3( 0.5, -0.3535533845424652, 0, -0.3535533845424652, 0, 0.3535533845424652, 0, 0.3535533845424652, -0.5 );
	const float3x3 g4 = float3x3( 0, -0.5, 0, 0.5, 0, 0.5, 0, -0.5, 0 );
	const float3x3 g5 = float3x3( -0.5, 0, 0.5, 0, 0, 0, 0.5, 0, -0.5 );
	const float3x3 g6 = float3x3( 0.1666666716337204, -0.3333333432674408, 0.1666666716337204, -0.3333333432674408, 0.6666666865348816, -0.3333333432674408, 0.1666666716337204, -0.3333333432674408, 0.1666666716337204 );
	const float3x3 g7 = float3x3( -0.3333333432674408, 0.1666666716337204, -0.3333333432674408, 0.1666666716337204, 0.6666666865348816, 0.1666666716337204, -0.3333333432674408, 0.1666666716337204, -0.3333333432674408 );
	const float3x3 g8 = float3x3( 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408 );
	
	half4 fragThin (v2f i) : COLOR
	{
		G[0] = float3x3( 0.3535533845424652, 0, -0.3535533845424652, 0.5, 0, -0.5, 0.3535533845424652, 0, -0.3535533845424652 );
		G[1] = float3x3( 0.3535533845424652, 0.5, 0.3535533845424652, 0, 0, 0, -0.3535533845424652, -0.5, -0.3535533845424652 );
		G[2] = float3x3( 0, 0.3535533845424652, -0.5, -0.3535533845424652, 0, 0.3535533845424652, 0.5, -0.3535533845424652, 0 );
		G[3] = float3x3( 0.5, -0.3535533845424652, 0, -0.3535533845424652, 0, 0.3535533845424652, 0, 0.3535533845424652, -0.5 );
		G[4] = float3x3( 0, -0.5, 0, 0.5, 0, 0.5, 0, -0.5, 0 );
		G[5] = float3x3( -0.5, 0, 0.5, 0, 0, 0, 0.5, 0, -0.5 );
		G[6] = float3x3( 0.1666666716337204, -0.3333333432674408, 0.1666666716337204, -0.3333333432674408, 0.6666666865348816, -0.3333333432674408, 0.1666666716337204, -0.3333333432674408, 0.1666666716337204 );
		G[7] = float3x3( -0.3333333432674408, 0.1666666716337204, -0.3333333432674408, 0.1666666716337204, 0.6666666865348816, 0.1666666716337204, -0.3333333432674408, 0.1666666716337204, -0.3333333432674408 );
		G[8] = float3x3( 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408, 0.3333333432674408 );

		half4 original = tex2D(_MainTex, i.uv[0]);
		float depth = 1.0 - tex2D( _CameraDepthNormalsTexture, i.uv[1] ).b;
		
		/*half4 center = tex2D (_CameraDepthNormalsTexture, i.uv[1]);
		half4 sample1 = tex2D (_CameraDepthNormalsTexture, i.uv[2]);
		half4 sample2 = tex2D (_CameraDepthNormalsTexture, i.uv[3]);
		
		// encoded normal
		half2 centerNormal = center.xy;
		// decoded depth
		float centerDepth = DecodeFloatRG (center.zw);*/
		
		half edge = 0.0;

		float3 sample;
		float3x3 sampleMatrix;
		for( int x = 0; x < 3; x++ ) 
		{
			for( int j = 0; j < 3; j++ ) 
			{
				sample = tex2D( _MainTex, i.uv[0].xy + _MainTex_TexelSize.xy * float2( x - 1.0, j - 1.0 ) * sensitivity.xy ).rgb;
				sampleMatrix[int(x)][int(j)] = length( sample );
			}
		}

		float cnv[9];
		for( int x = 0.0; x < 9; x++ )  
		{
			float dp3 = dot( G[x][0], sampleMatrix[0] ) + dot( G[x][1], sampleMatrix[1] ) + dot( G[x][2], sampleMatrix[2] );
			cnv[x] = dp3 * dp3;
		}

		float M = ( cnv[0] + cnv[1] ) + ( cnv[2] + cnv[3] );
		float S = ( cnv[4] + cnv[5] ) + ( cnv[6] + cnv[7] ) + ( cnv[8] + M );
		
		//edge *= CheckSame(centerNormal, centerDepth, sample1);
		//edge *= CheckSame(centerNormal, centerDepth, sample2);

		edge = sqrt( M / S ) * 10.0;
		edge *= depth;
			
		return lerp(original, original - edge, _BgFade);
	}
	
	ENDCG 
  }
}

Fallback off
	
} // shader