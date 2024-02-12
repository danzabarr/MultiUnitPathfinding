Shader "Custom/Terrain" {
    Properties {
        _Color ("Base Color", Color) = (1,1,1,1)
        _UpColor ("Up Normals Color", Color) = (0,1,0,1)
        _Threshold ("Threshold", Range(0,1)) = 0.9
        _Blend ("Blend", Range(0,1)) = 0.5
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input {
            float3 worldNormal;
        };

        half4 _Color;
        half4 _UpColor;
        half _Threshold;
half _Blend;

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Check if the normal is facing up
            
            half upness = dot(IN.worldNormal, float3(0,1,0));

            if (upness > _Threshold) {
                // Color for normals facing upwards
                o.Albedo = _UpColor.rgb;
            } else if (upness + _Blend > _Threshold) {
// Blend between the two colors
				o.Albedo = lerp(_Color.rgb, _UpColor.rgb, (upness + _Blend - _Threshold) / _Blend);
			} else {
				// Base color
				o.Albedo = _Color.rgb;
            } 
        }
        ENDCG
    }
    FallBack "Diffuse"
}
