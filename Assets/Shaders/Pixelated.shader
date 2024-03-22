Shader "Toon/Pixelated"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Resolution ("Resolution", Float) = 1024.0
        
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)
        _EdgeThreshold ("Edge Threshold", Float) = 0.05
        _EdgeFalloff ("Edge Falloff", Float) = 0.05
        _EdgeThickness ("Edge Thickness", Float) = 1.0
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
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

                // Sample the original, depth, and normals texture
                fixed4 col = tex2D(_MainTex, pixelUV);

                float centerDepth = tex2D(_CameraDepthTexture, pixelUV).r;
                //fixed3 centerNormal = tex2D(_CameraDepthNormalsTexture, pixelUV).rgb * 2.0 - 1.0;

                // Compute the size of one pixelated "pixel" in UV space for neighbor sampling
                float2 n = float2(0.0, 1.0/ _Resolution * aspectRatio * _EdgeThickness);
                float2 e = float2(1.0 / _Resolution * _EdgeThickness, 0.0);
                float2 s = -n;
                float2 w = -e;

                {
                    float nDepth = tex2D(_CameraDepthTexture, pixelUV + n).r;
                    float eDepth = tex2D(_CameraDepthTexture, pixelUV + e).r;
                    float sDepth = tex2D(_CameraDepthTexture, pixelUV + s).r;
                    float wDepth = tex2D(_CameraDepthTexture, pixelUV + w).r;

                    float maxDepthDifference = 0;

                    maxDepthDifference = max(maxDepthDifference, abs(nDepth - centerDepth));
                    maxDepthDifference = max(maxDepthDifference, abs(eDepth - centerDepth));
                    //maxDepthDifference = max(maxDepthDifference, (sDepth - centerDepth));
                    maxDepthDifference = max(maxDepthDifference, abs(wDepth - centerDepth));

                    if (maxDepthDifference > _EdgeThreshold * centerDepth) // Adjust this threshold based on your needs
				    {
                        float falloff = saturate(1 - centerDepth * centerDepth * _EdgeFalloff);
					    //col.rgb = col.rgb * saturate(1 - _EdgeColor.a + falloff) + _EdgeColor.rgb * saturate(_EdgeColor.a - falloff);
					    //col *= _EdgeColor;
                        col = lerp(col, _EdgeColor, falloff * _EdgeColor.a);
                    }
                }

                return col;
            }
            ENDCG
        }
    }
}

