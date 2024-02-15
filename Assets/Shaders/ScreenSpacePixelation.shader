Shader "Custom/ScreenSpacePixelation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Resolution ("Resolution", Float) = 1024.0
        _ColorRange ("Color Range", Float) = 256.0
        _EdgeThreshold ("Edge Threshold", Float) = 0.05
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)

        _InnerEdgeThreshold ("Inner Edge Threshold", Float) = 0.05
_InnerEdgeColor ("Inner Edge Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        // Render setup goes here...

        // Rendertype opaque

        Tags { "RenderType"="Opaque" }
        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler2D _CameraDepthNormalsTexture;
            float _Resolution;
            float _ColorRange;
            float _EdgeThreshold;
            fixed4 _EdgeColor;
            float _InnerEdgeThreshold;
            fixed4 _InnerEdgeColor;


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
                fixed3 centerNormal = tex2D(_CameraDepthNormalsTexture, pixelUV).rgb * 2.0 - 1.0;

                // Compute the size of one pixelated "pixel" in UV space for neighbor sampling
                float2 n = float2(0.0, 1.0/ _Resolution * aspectRatio);
                float2 e = float2(1.0 / _Resolution, 0.0);
                float2 s = -n;
                float2 w = -e;

                float nDepth = tex2D(_CameraDepthTexture, pixelUV + n).r;
                float eDepth = tex2D(_CameraDepthTexture, pixelUV + e).r;
                float sDepth = tex2D(_CameraDepthTexture, pixelUV + s).r;
                float wDepth = tex2D(_CameraDepthTexture, pixelUV + w).r;

                float maxDepthDifference = 0;

                maxDepthDifference = max(maxDepthDifference, abs(centerDepth - nDepth));
                maxDepthDifference = max(maxDepthDifference, abs(centerDepth - eDepth));
                //maxDepthDifference = max(maxDepthDifference, abs(centerDepth - sDepth));
                //maxDepthDifference = max(maxDepthDifference, abs(centerDepth - wDepth));

                if (maxDepthDifference > _EdgeThreshold) // Adjust this threshold based on your needs
				{
					col.rgb = col.rgb * (1 - _EdgeColor.a) + _EdgeColor.rgb * _EdgeColor.a;
					//col = _EdgeColor;
				}

                // Sample neighbor normal and unpack it
                fixed3 nNormal = tex2D(_CameraDepthNormalsTexture, pixelUV + n).rgb * 2.0 - 1.0;
                fixed3 eNormal = tex2D(_CameraDepthNormalsTexture, pixelUV + e).rgb * 2.0 - 1.0;
                fixed3 sNormal = tex2D(_CameraDepthNormalsTexture, pixelUV + s).rgb * 2.0 - 1.0;
                fixed3 wNormal = tex2D(_CameraDepthNormalsTexture, pixelUV + w).rgb * 2.0 - 1.0;

                float maxNormalDifference = 0;
                maxNormalDifference = max(maxNormalDifference, 1.0 - dot(normalize(centerNormal), normalize(nNormal)));
                maxNormalDifference = max(maxNormalDifference, 1.0 - dot(normalize(centerNormal), normalize(eNormal)));
                //maxNormalDifference = max(maxNormalDifference, 1.0 - dot(normalize(centerNormal), normalize(sNormal)));
                //maxNormalDifference = max(maxNormalDifference, 1.0 - dot(normalize(centerNormal), normalize(wNormal)));

                if (maxNormalDifference > _InnerEdgeThreshold) // Adjust this threshold based on your needs
                {
                    col.rgb = col.rgb * (1 - _InnerEdgeColor.a) + _InnerEdgeColor.rgb * _InnerEdgeColor.a;
                    //col = _EdgeColor;
                }

                // step colors
                col.rgb = round(col.rgb * _ColorRange) / _ColorRange;

                return col;
            }
            ENDCG
        }
    }
}
