Shader "Toon/Water"
{
    Properties
    {
		
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        // just write to the depth buffer and don't render anything

        Pass
        {
            ZWrite On
            ColorMask 0
        }
    }
}
