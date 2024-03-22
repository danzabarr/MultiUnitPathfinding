Shader "Toon/Solid" 
{
	Properties 
	{
		_MainColor ("Main Color", Color) = (1,1,1,1)
		_AmbientColor ("Ambient Color", Range(0, 1)) = 0.4
		_ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.3
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
				// Calculate diffuse lighting
				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
				float diffuse = max(0, dot(normalize(i.normal), lightDir));

				float shadow = SHADOW_ATTENUATION(i);

				diffuse *= shadow;

				// Apply threshold for toon shading
				fixed4 finalColor;
				if (diffuse > _ShadowThreshold) {
					
					// specular lighting
					//float3 viewDir = normalize(_WorldSpaceCameraPos - i.pos);
					//float3 reflectDir = reflect(-lightDir, i.normal);
					//float spec = pow(saturate(dot(viewDir, reflectDir)), 16);

					finalColor = _MainColor;

				} else {
					// Use shaded color
					finalColor = lerp(_MainColor, UNITY_LIGHTMODEL_AMBIENT, _AmbientColor);
				}
				return finalColor;
			}

            ENDCG
        }

		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"


    }
}
