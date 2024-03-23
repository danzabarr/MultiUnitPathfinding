Shader "Toon/Terrain" 
{
	Properties 
	{
		_MainColor ("Flat Color", Color) = (1,1,1,1)
        _SlopeColor ("Slope Color", Color) = (0,0,0,1)
        _SlopeMin ("Slope Min", Range(0, 1)) = 0.5
		_AmbientColor ("Ambient Color", Range(0, 1)) = 0.5
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
			#pragma multi_compile_fwdbase multi_compile_instancing
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
				TRANSFER_SHADOW(o)

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
				float lighting = max(0, dot(normalize(i.normal), lightDir)) * SHADOW_ATTENUATION(i);
				
				fixed4 finalColor = dot(i.normal, float3(0,1,0)) < _SlopeMin ? _SlopeColor : _MainColor;
				if (lighting <= _ShadowThreshold) 
                    finalColor = lerp(finalColor, UNITY_LIGHTMODEL_AMBIENT, _AmbientColor);
                
				return finalColor;
			}

            ENDCG
        }

		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"


    }
}
