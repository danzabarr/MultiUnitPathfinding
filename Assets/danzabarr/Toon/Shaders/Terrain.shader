Shader "Toon/Terrain" 
{
	Properties 
	{
		_MainColor ("Flat Colour", Color) = (1,1,1,1)
        _SlopeColor ("Slope Colour", Color) = (0,0,0,1)
        _SlopeMin ("Slope Threshold", Range(0, 1)) = 0.5
		_AmbientColor ("Shadow Opacity", Range(0, 1)) = 0.5
		_ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
	}
    SubShader 
	{
        Pass 
		{
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#pragma multi_compile_fwdbase 
			#pragma multi_compile_instancing
			#pragma multi_compile_fog
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 normal : NORMAL;
				SHADOW_COORDS(1)
				UNITY_FOG_COORDS(2)
				UNITY_VERTEX_INPUT_INSTANCE_ID 
			};

			fixed4 _MainColor;
            fixed4 _SlopeColor;
            float _SlopeMin;
			float _AmbientColor;
			float _ShadowThreshold;

            v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.normal = UnityObjectToWorldNormal(v.normal);
				TRANSFER_SHADOW(o);
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
				float lighting = max(0, dot(normalize(i.normal), lightDir)) * SHADOW_ATTENUATION(i);
				
				fixed4 col = dot(i.normal, float3(0,1,0)) < _SlopeMin ? _SlopeColor : _MainColor;

				col = lighting > _ShadowThreshold ? col : lerp(col, UNITY_LIGHTMODEL_AMBIENT, _AmbientColor);

                
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}

            ENDCG
        }

		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"


    }
}
