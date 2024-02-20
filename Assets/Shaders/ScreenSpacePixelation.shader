Shader "Custom/ScreenSpacePixelation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Resolution ("Resolution", Float) = 1024.0
        _ColorRange ("Color Range", Float) = 256.0
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)
        _InnerEdgeColor ("Inner Edge Color", Color) = (0,0,0,1)
        
        _EdgeThreshold ("Edge Threshold", Float) = 0.05
        _InnerEdgeThreshold ("Inner Edge Threshold", Float) = 0.05
        
        _EdgeFalloff ("Edge Falloff", Float) = 0.05
        _InnerEdgeFalloff ("Inner Edge Falloff", Float) = 0.05

       _PaletteTex ("Palette Texture", 2D) = "white" {}
        _PaletteSize ("Palette Size", Float) = 32.0

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
            float _EdgeFalloff;
            float _InnerEdgeFalloff;
            sampler2D _PaletteTex;
            float _PaletteSize; // Number of colors in the palette

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

            float3 RGBtoHSV(float3 color)
			{
				float minV, maxV, delta;
				float3 hsv;

				minV = min(min(color.r, color.g), color.b);
				maxV = max(max(color.r, color.g), color.b);
				hsv.z = maxV; // v

				delta = maxV - minV;

				if (maxV != 0)
					hsv.y = delta / maxV; // s
				else
				{
					// r = g = b = 0
					// s = 0, v is undefined
					hsv.y = 0;
					hsv.x = -1;
					return hsv;
				}

				if (color.r == maxV)
					hsv.x = (color.g - color.b) / delta; // between yellow & magenta
				else if (color.g == maxV)
					hsv.x = 2 + (color.b - color.r) / delta; // between cyan & yellow
				else
					hsv.x = 4 + (color.r - color.g) / delta; // between magenta & cyan

				hsv.x *= 60; // degrees
				if (hsv.x < 0)
					hsv.x += 360;

				return hsv;
			}

            float3 HSVtoRGB(float3 hsv)
            {
float3 rgb;
				int i;
				float f, p, q, t;

				if (hsv.y == 0)
				{
					// achromatic (grey)
					rgb.r = rgb.g = rgb.b = hsv.z;
					return rgb;
				}

				hsv.x /= 60; // sector 0 to 5
				i = floor(hsv.x);
				f = hsv.x - i; // factorial part of h
				p = hsv.z * (1 - hsv.y);
				q = hsv.z * (1 - hsv.y * f);
				t = hsv.z * (1 - hsv.y * (1 - f));

				switch (i)
				{
					case 0:
						rgb.r = hsv.z;
						rgb.g = t;
						rgb.b = p;
						break;
					case 1:
						rgb.r = q;
						rgb.g = hsv.z;
						rgb.b = p;
						break;
					case 2:
						rgb.r = p;
						rgb.g = hsv.z;
						rgb.b = t;
						break;
					case 3:
						rgb.r = p;
						rgb.g = q;
						rgb.b = hsv.z;
						break;
					case 4:
						rgb.r = t;
						rgb.g = p;
						rgb.b = hsv.z;
						break;
					default: // case 5:
						rgb.r = hsv.z;
						rgb.g = p;
						rgb.b = q;
						break;
				}

				return rgb;

            }

            float colorDistance(float4 color1, float4 color2)
            {
                float3 hsv1 = RGBtoHSV(color1.rgb);
                float3 hsv2 = RGBtoHSV(color2.rgb);
                
                hsv1 *= float3(10, 5, 5);
                hsv2 *= float3(10, 5, 5);
                
                return distance(hsv1, hsv2);
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

                {
                    float4 originalColor = col;
                    float nearestDistance = 1e6; // Initialize with a large number
                    float4 nearestColor = originalColor;


                    for (int j = 0; j < _PaletteSize; ++j) {
                        float4 paletteColor = tex2D(_PaletteTex, float2((j+0.5) / _PaletteSize, 0.5));
                        if (paletteColor.a < 0.5) continue; // Skip transparent colors (if any))
                        float d = colorDistance(originalColor, paletteColor);
                        if (d < nearestDistance) {
                            nearestDistance = d;
                            nearestColor = paletteColor;
                        }
                    }

                   // col.rgb = nearestColor.rgb;
                }
                
                col.rgb = round(col.rgb * _ColorRange) / _ColorRange;
                {
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

                    if (maxNormalDifference > _InnerEdgeThreshold * centerDepth) // Adjust this threshold based on your needs
                    {
                        float falloff = saturate(1 - centerDepth *centerDepth * _InnerEdgeFalloff);
                        col.rgb = col.rgb * saturate(1 - _InnerEdgeColor.a + falloff) + _InnerEdgeColor.rgb * saturate(_InnerEdgeColor.a - falloff);
                        //col = _EdgeColor;
                    }
                }


                {
                    float nDepth = tex2D(_CameraDepthTexture, pixelUV + n).r;
                    float eDepth = tex2D(_CameraDepthTexture, pixelUV + e).r;
                    float sDepth = tex2D(_CameraDepthTexture, pixelUV + s).r;
                    float wDepth = tex2D(_CameraDepthTexture, pixelUV + w).r;

                    float maxDepthDifference = 0;

                    maxDepthDifference = max(maxDepthDifference, (nDepth - centerDepth));
                    maxDepthDifference = max(maxDepthDifference, (eDepth - centerDepth));
                    //maxDepthDifference = max(maxDepthDifference, (sDepth - centerDepth));
                    //maxDepthDifference = max(maxDepthDifference, (wDepth - centerDepth));

                    if (maxDepthDifference > _EdgeThreshold * centerDepth) // Adjust this threshold based on your needs
				    {
                        float falloff = saturate(1 - centerDepth * centerDepth * _EdgeFalloff);
					    //col.rgb = col.rgb * saturate(1 - _EdgeColor.a + falloff) + _EdgeColor.rgb * saturate(_EdgeColor.a - falloff);
					    col *= _EdgeColor;
				    }
                }
                
                
                // step colors

                // Squash saturation
                float3 hsv = RGBtoHSV(col.rgb);
                
                float hueSnapping = 0.125;
                float saturationSnapping = 0.25;
				float valueSnapping = 0.25;

                float valueRange = 1;
                float saturationRange = 0.125;
                
                float valueOffset = 0.125;
                float saturationOffset = 0.1;

                //hsv.x = round(hsv.x / hueSnapping) * hueSnapping;
                
                hsv.y *= saturationRange;
                hsv.y = saturate(hsv.y + saturationOffset + 0.5 - saturationRange / 2);
                //hsv.y = round(hsv.y / saturationSnapping) * saturationSnapping;
                
                hsv.z *= valueRange;
                hsv.z = saturate(hsv.z + valueOffset + 0.5 - valueRange / 2);
                hsv.z = round(hsv.z / valueSnapping) * valueSnapping;

                hsv.y = saturate(hsv.y);
                hsv.z = saturate(hsv.z);
                //col.rgb = HSVtoRGB(hsv);

                //col.rgb = tex2D(_CameraDepthNormalsTexture, i.uv).r;

                return col;
            }
            ENDCG
        }
    }
}

