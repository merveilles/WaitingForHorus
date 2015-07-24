Shader "Horus_PerVertex" 
{
    Properties 
    {
        _Color ("Main Color", COLOR) = ( 1,1,1,1 )
        _MainTex ("Base (RGBA)", 2D) = "white" {}
        _Emission ("Emmisive Color", Color) = (0,0,0,0)
    }
    
    SubShader 
    {
        Pass 
        {
            Material 
            {
                Diffuse [_Color]
                Ambient [_Color]
                Emission ( 0.175, 0.175, 0.175, 0.175 )
            }
            Lighting On
            SetTexture [_MainTex] {
                Combine texture * primary DOUBLE, texture * primary
            }
        }
    }
}