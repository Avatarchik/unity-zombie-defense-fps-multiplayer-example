// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Particles/FXVille Blood World Lighting" {
Properties {
	_Columns ("Flipbook Columns", int) = 1
	_Rows ("Flipbook Rows", int) = 1
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_ChannelMask ("Channel Mask Color", Color) = (0,0,0,1)
	_MainTex ("Particle Texture", 2D) = "white" {}
	
	_EdgeMin ("SmoothStep Edge Min", float) = 0.05
	_EdgeSoft ("SmoothStep Softness", float) = 0.05
	
	_NormalMap ("Normal Map", 2D) = "bump" {}
	_NormalStr ("Normal Strength", float) = 1
	_HighlightColor ("Highlight Color", Color) = (1,1,1,0)
	_Shininess ("Shininess", float) = 64.0
	
	_Detail ("Detail Tex", 2D) = "gray" {}
	_DetailTile ("Detail Tiling", float) = 6.0
	_DetailPan ("Detail Alpha Pan", float) = 0.1
	_DetailAlphaAffect ("Detail Alpha Affect", float) =  1.0
	_DetailBrightAffect("Detail Brightness Affect", float) = 0.5
	
	_UVOff ("UV Offset Map", 2D) = "bump" {}
	_OffPow ("UV Offset Power", float) = 0.1
	_OffTile ("UV Offset Tiling", float) = 1.0
	
	_Overbright ("Overbright", float) = 0.0
	_Ambience ("Lighting Ambience", float) = 0.5
	_InvFade ("Soft Particles Factor", Range(0.01,8.0)) = 3.0
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off

	SubShader {
		Pass {
			Tags { "LightMode" = "ForwardBase" }
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_particles
			#pragma multi_compile_fog
			#pragma target 3.0
			
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			sampler2D _MainTex;
			sampler2D _Detail;
			sampler2D _NormalMap;
			sampler2D _UVOff;
			fixed4 _TintColor;
			fixed4 _ChannelMask;
			fixed4 _HighlightColor;
			fixed _EdgeMin;
			fixed _EdgeSoft;
			fixed _Overbright;
			fixed _Shininess;
			fixed _NormalStr;
			fixed _Ambience;
			fixed _DetailTile;
			fixed _DetailPan;
			fixed _OffPow;
			fixed _OffTile;
			fixed _DetailBrightAffect;
			fixed _DetailAlphaAffect;
			fixed _Columns;
			fixed _Rows;
			
			fixed4 _LightColor0;
			
			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float4 posWorld : TEXCOORD2;
				float3 normal : NORMAL;
				LIGHTING_COORDS(3, 4)

				UNITY_FOG_COORDS(1)
				#ifdef SOFTPARTICLES_ON
				float4 projPos : TEXCOORD3;
				#endif
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				
				v2f o;
				
				o.vertex = UnityObjectToClipPos(v.vertex);

				#ifdef SOFTPARTICLES_ON
				o.projPos = ComputeScreenPos (o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);
				#endif

				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				o.normal = v.normal;
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				
				return o;
			}

			sampler2D_float _CameraDepthTexture;
			float _InvFade;
			
			fixed4 frag (v2f i) : SV_Target
			{
				#ifdef SOFTPARTICLES_ON
				float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
				float partZ = i.projPos.z;
				float fade = saturate (_InvFade * (sceneZ-partZ));
				i.color.a *= fade;
				#endif

				float shadAtten = LIGHT_ATTENUATION(i);
                
				//Create a mask to reign in the effects of the UV Offset
				fixed edgeMask = 1 - distance(i.texcoord, fixed2(0.5,0.5));
				edgeMask = saturate(2 * frac(_Rows*i.texcoord.x));
				edgeMask *= saturate(2 * frac(-_Rows*i.texcoord.x));
				edgeMask *= saturate(2 * frac(_Columns*i.texcoord.y));
				edgeMask *= saturate(2 * frac(-_Columns*i.texcoord.y));
				
				//Caluculate UV Offset.
				//Detail texture can have its position randomized or be animated by changing the brigness of the green channel value.
				fixed4 UVOff = tex2D(_UVOff, (i.texcoord + fixed2(i.color.y * 154.6, i.color.y * 798.3)) * _OffTile) * 2 - 1;
				fixed2 UVOff2 = 0.1 * fixed2(UVOff.g * _OffPow, UVOff.a * _OffPow) * edgeMask;
				
				//Manually unpack normal
				fixed4 normalMap4 = (tex2D(_NormalMap, i.texcoord + UVOff2));
				fixed3 normalMap = UnpackNormal(normalMap4);
				
				//Calculate Detail Texture. 
				//Detail texture can have its position randomized or be animated by changing the brigness of the green channel value.
				fixed4 detail = tex2D(_Detail, _DetailTile * i.texcoord + fixed2(i.color.y * 1.32 + i.color.a * _DetailPan, 0) + UVOff2);
				detail.a = lerp(1, detail.a, _DetailAlphaAffect);
				detail.a *= lerp(1, detail.rgb, _DetailAlphaAffect);
				detail.rgb = lerp((1 - detail.rgb), detail.rgb, _DetailBrightAffect + 0.5);
				
				
				//Read main texture and declare col. Make sure that tex has an alpha channel. 
				//If needed, use unity's "alpha from brightness" option on the texture import settings.
				fixed4 tex = tex2D(_MainTex, i.texcoord + UVOff2);
				fixed4 col = fixed4(_TintColor.rgb, 1);
				
				tex.a = length(tex * _ChannelMask);
				col.a *= tex.a * i.color.a * detail.a;
				
				col.a = smoothstep(_EdgeMin, _EdgeMin + _EdgeSoft, col.a);
				col.rgb = col.rgb * detail.rgb * i.color.r * (_Overbright + 1);	
				
				UNITY_APPLY_FOG(i.fogCoord, col);

				float3 fragmentToLight = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
				float  distanceToLight = length(fragmentToLight);

				//Generate lighting vectors
				float3 lightDir = normalize(fragmentToLight) * _WorldSpaceLightPos0.w + normalize(_WorldSpaceLightPos0.xyz + float3(0.001,0.001,0.001)) * (1 - _WorldSpaceLightPos0.w);
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.posWorld.xyz);
				float3 halfVector = normalize(lightDir + viewDir);

				//Solve surface normals
				float3 surfaceNormal = normalize(i.normal + normalize(normalMap * half3(_NormalStr, _NormalStr, 1)));

				//Calculate lighting
				float NdotL = (dot(surfaceNormal, lightDir)) * 0.5 + 0.5;
				float NdotH = saturate(dot(surfaceNormal, halfVector));
				float NdotV = saturate(1-dot(surfaceNormal, viewDir));
				NdotV = 1-col.a;
				NdotV = normalMap.x + normalMap.y + 1 - col.a;

				//Finalize lighting. Specular lighting strength can be reduced by lowering the brightness of the blue channel in vertex/particle colors
				float3 specReflection = pow(NdotH, _Shininess) * NdotL * 4 * _LightColor0.rgb + UNITY_LIGHTMODEL_AMBIENT.rgb * NdotV;
				float3 diffuseReflection = saturate(lerp(NdotL, 1, _Ambience)) * _LightColor0.rgb + UNITY_LIGHTMODEL_AMBIENT.rgb;


				return fixed4(col.rgb * diffuseReflection + specReflection * _HighlightColor.rgb * i.color.b, col.a);
			}
			ENDCG 
		}

		//Point lights and additional lighting
		Pass {
			Tags { "LightMode" = "ForwardAdd" }
			Blend One One
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_particles
			#pragma multi_compile_fog
			#pragma target 3.0
			
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			sampler2D _MainTex;
			sampler2D _Detail;
			sampler2D _UVOff;
			fixed4 _TintColor;
			fixed4 _ChannelMask;
			fixed _EdgeMin;
			fixed _EdgeSoft;
			fixed _Overbright;
			fixed _DetailTile;
			fixed _DetailPan;
			fixed _OffPow;
			fixed _OffTile;
			fixed _DetailBrightAffect;
			fixed _DetailAlphaAffect;
			fixed _Columns;
			fixed _Rows;
			
			fixed4 _LightColor0;
			
			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float4 posWorld : TEXCOORD2;
				LIGHTING_COORDS(3, 4)

				UNITY_FOG_COORDS(1)
				#ifdef SOFTPARTICLES_ON
				float4 projPos : TEXCOORD3;
				#endif
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				
				v2f o;
				
				o.vertex = UnityObjectToClipPos(v.vertex);

				#ifdef SOFTPARTICLES_ON
				o.projPos = ComputeScreenPos (o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);
				#endif

				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				
				return o;
			}

			sampler2D_float _CameraDepthTexture;
			float _InvFade;
			
			fixed4 frag (v2f i) : SV_Target
			{
				#ifdef SOFTPARTICLES_ON
				float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
				float partZ = i.projPos.z;
				float fade = saturate (_InvFade * (sceneZ-partZ));
				i.color.a *= fade;
				#endif

				float shadAtten = LIGHT_ATTENUATION(i);

				//Calculate General Scene Illumination
                float3 lightColor = _LightColor0.rgb;
                
				//Create a mask to reign in the effects of the UV Offset
				fixed edgeMask = 1 - distance(i.texcoord, fixed2(0.5,0.5));
				edgeMask = saturate(2 * frac(_Rows*i.texcoord.x));
				edgeMask *= saturate(2 * frac(-_Rows*i.texcoord.x));
				edgeMask *= saturate(2 * frac(_Columns*i.texcoord.y));
				edgeMask *= saturate(2 * frac(-_Columns*i.texcoord.y));
				
				//Caluculate UV Offset.
				//Detail texture can have its position randomized or be animated by changing the brigness of the green channel value.
				fixed4 UVOff = tex2D(_UVOff, (i.texcoord + fixed2(i.color.y * 154.6, i.color.y * 798.3)) * _OffTile) * 2 - 1;
				fixed2 UVOff2 = 0.1 * fixed2(UVOff.g * _OffPow, UVOff.a * _OffPow) * edgeMask;
				
				//Calculate Detail Texture. 
				//Detail texture can have its position randomized or be animated by changing the brigness of the green channel value.
				fixed4 detail = tex2D(_Detail, _DetailTile * i.texcoord + fixed2(i.color.y * 1.32 + i.color.a * _DetailPan, 0) + UVOff2);
				detail.a = lerp(1, detail.a, _DetailAlphaAffect);
				detail.a *= lerp(1, detail.rgb, _DetailAlphaAffect);
				detail.rgb = lerp((1 - detail.rgb), detail.rgb, _DetailBrightAffect + 0.5);
				
				
				//Read main texture and declare col. Make sure that tex has an alpha channel. 
				//If needed, use unity's "alpha from brightness" option on the texture import settings.
				fixed4 tex = tex2D(_MainTex, i.texcoord + UVOff2);
				fixed4 col = fixed4(_TintColor.rgb, 1);
				
				tex.a = length(tex * _ChannelMask);
				col.a *= tex.a * i.color.a * detail.a;

				
				col.a = smoothstep(_EdgeMin, _EdgeMin + _EdgeSoft, col.a);
				col.rgb = col.rgb * detail.rgb * i.color.r * (_Overbright + 1);

				float3 fragmentToLight = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
				float  distanceToLight = length(fragmentToLight);
				float  atten = pow(2, -0.1 * distanceToLight * distanceToLight * distanceToLight) * _WorldSpaceLightPos0.w + 1 - _WorldSpaceLightPos0.w; // (-0.1x^2)^2 for pointlights 1 for dirlights


				float3 diffuseReflection = lightColor * shadAtten * atten;

				return fixed4(col.rgb * diffuseReflection * col.a, col.a);
			}
			ENDCG 
		}
	}	
}
Fallback "Paricles/Alpha Blended"
}