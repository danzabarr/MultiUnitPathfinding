Shader "Toon/Pixelated"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Resolution ("Resolution", Float) = 1024.0
        
        _EdgeColor ("Edge Colour", Color) = (0,0,0,1)
        _EdgeThreshold ("Edge Threshold", Float) = 0.05
        _EdgeFalloff ("Edge Falloff", Float) = 0.05
        _EdgeThickness ("Edge Thickness", Float) = 1.0
        _EdgeMinimumAltitude ("Edge Minimum Altitude", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            //sampler2D _CameraDepthNormalsTexture;
            float _Resolution;

            float _EdgeThreshold;
            fixed4 _EdgeColor;
            float _EdgeFalloff;
            float _EdgeThickness;
            float _EdgeMinimumAltitude;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 camRelativeWorldPos : TEXCOORD1;

            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.camRelativeWorldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz - _WorldSpaceCameraPos;
                return o;
            }

            float4x4 _CameraInvProjection;
            float4x4 _CameraInvView;

            float3 ComputeWorldPos(float2 screenPos, float depth)
            {
                float4 clipPos = float4(screenPos * 2 - 1, depth, 1);
                float4 viewPos = mul(_CameraInvProjection, clipPos);
                viewPos /= viewPos.w;
                float3 worldPos = mul(_CameraInvView, viewPos).xyz;
                return worldPos;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Aspect ratio correction
                float aspectRatio = _ScreenParams.y / _ScreenParams.x;
    
                // Adjust UVs for square pixels
                float2 pixelUV = i.uv;
                pixelUV.x /= aspectRatio;
                pixelUV *= _Resolution;
                pixelUV = floor(pixelUV) / _Resolution;
                pixelUV.x *= aspectRatio;

                fixed4 col = tex2D(_MainTex, pixelUV);

                // Edge detection

                // Compute the size of one pixelated "pixel" in UV space for neighbor sampling
                float2 n = float2(0.0, 1.0/ _Resolution * aspectRatio * _EdgeThickness);
                float2 e = float2(1.0 / _Resolution * _EdgeThickness, 0.0);
                float2 s = -n;
                float2 w = -e;
                
                float depth = tex2D(_CameraDepthTexture, pixelUV).r;
                float sceneZ = LinearEyeDepth(depth);
                float3 rayNorm = normalize(i.camRelativeWorldPos.xyz);
                float3 rayUnitDepth = rayNorm / dot(rayNorm, unity_WorldToCamera._m20_m21_m22);
                float3 worldPos = rayUnitDepth * sceneZ + _WorldSpaceCameraPos;
            
                //col.rgb = worldPos.xyz;
            
                
                
                if (worldPos.y > _EdgeMinimumAltitude)
                {
                    float nDepth = tex2D(_CameraDepthTexture, pixelUV + n).r;
                    float eDepth = tex2D(_CameraDepthTexture, pixelUV + e).r;
                    float sDepth = tex2D(_CameraDepthTexture, pixelUV + s).r;
                    float wDepth = tex2D(_CameraDepthTexture, pixelUV + w).r;
                    
                    // convert to linear depth for more accurate comparisons
                    depth = Linear01Depth(depth);
                    nDepth = Linear01Depth(nDepth);
                    eDepth = Linear01Depth(eDepth);
                    sDepth = Linear01Depth(sDepth);
                    wDepth = Linear01Depth(wDepth);

                    float maxDepthDifference = 0;

                    // Compare the depth of the current pixel to its north, east, and west neighbors
                    maxDepthDifference = max(maxDepthDifference, abs(nDepth - depth));
                    maxDepthDifference = max(maxDepthDifference, abs(eDepth - depth));
                    //maxDepthDifference = max(maxDepthDifference, (sDepth - centerDepth));
                    maxDepthDifference = max(maxDepthDifference, abs(wDepth - depth));

                    // If the depth difference is greater than the threshold, draw an edge
                    if (maxDepthDifference > _EdgeThreshold * depth) 
				    {
                        float falloff = saturate(1 - depth * depth * _EdgeFalloff);
                        col = lerp(col, _EdgeColor, falloff * _EdgeColor.a);
                    }
                }

                return col;
            }
            ENDCG
        }
    }
}

