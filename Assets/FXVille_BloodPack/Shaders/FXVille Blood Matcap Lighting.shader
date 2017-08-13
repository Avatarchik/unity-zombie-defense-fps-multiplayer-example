// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Particles/FXVille Blood Matcap Lighting" {
Properties {
	_Columns ("Flipbook Columns", int) = 1
	_Rows ("Flipbook Rows", int) = 1
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_ChannelMask ("Channel Mask Color", Color) = (0,0,0,1)
	_MainTex ("Particle Texture", 2D) = "white" {}
	
	_EdgeMin ("SmoothStep Edge Min", float) = 0.05
	_EdgeSoft ("SmoothStep Softness", float) = 0.05
	
	_NormalMap ("Normal Map", 2D) = "bump" {}
	_HighlightColor ("Highlight Color", Color) = (1,1,1,0)
	_Reflect ("Reflection Matcap", 2D) = "black" {}
	
	_Detail ("Detail Tex", 2D) = "gray" {}
	_DetailTile ("Detail Tiling", float) = 6.0
	_DetailPan ("Detail Alpha Pan", float) = 0.1
	_DetailAlphaAffect ("Detail Alpha Affect", float) =  1.0
	_DetailBrightAffect("Detail Brightness Affect", float) = 0.5
	
	_UVOff ("UV Offset Map", 2D) = "bump" {}
	_OffPow ("UV Offset Power", float) = 0.1
	_OffTile ("UV Offset Tiling", float) = 1.0
	
	_Overbright ("Overbright", float) = 0.0
	_InvFade ("Soft Particles Factor", Range(0.01,8.0)) = 3.0
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off

	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_particles
			#pragma multi_compile_fog
			#pragma target 3.0
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _Detail;
			sampler2D _NormalMap;
			sampler2D _UVOff;
			sampler2D _Reflect;
			fixed4 _TintColor;
			fixed4 _ChannelMask;
			fixed4 _HighlightColor;
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
				UNITY_FOG_COORDS(1)
				#ifdef SOFTPARTICLES_ON
				float4 projPos : TEXCOORD2;
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
				
				//Calculate General Scene Illumination
                float3 lightColor = _LightColor0.rgb + UNITY_LIGHTMODEL_AMBIENT.rgb;
                
				//Create a mask to reign in the effects of the UV Offset
				fixed edgeMask = 1 - distance(i.texcoord, fixed2(0.5,0.5));
				edgeMask = saturate(2 * frac(_Rows*i.texcoord.x));
				edgeMask *= saturate(2 * frac(-_Rows*i.texcoord.x));
				edgeMask *= saturate(2 * frac(_Columns*i.texcoord.y));
				edgeMask *= saturate(2 * frac(-_Columns*i.texcoord.y));
				
				//Caluculate UV Offset.
				//Detail texture can have its position randomized or be animated by changing the brightness of the green channel value.
				fixed4 UVOff = tex2D(_UVOff, (i.texcoord + fixed2(i.color.y * 154.6, i.color.y * 798.3)) * _OffTile) * 2 - 1;
				fixed2 UVOff2 = 0.1 * fixed2(UVOff.g * _OffPow, UVOff.a * _OffPow) * edgeMask;
				
				//Manually unpack normal
				fixed4 normalMap4 = (tex2D(_NormalMap, i.texcoord + UVOff2));
				fixed3 normalMap = UnpackNormal(normalMap4) * 0.5 + 0.5;
				
				//Calculate Detail Texture. 
				//Detail texture can have its position randomized or be animated by changing the brightness of the green channel value.
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
				
				//Read Matcap. Lighting brigtness can be set with the brightness of the blue channel value.
				fixed3 matcap = tex2D(_Reflect, lerp(normalMap.xy, normalMap.xy * 1.5, detail.a));
				fixed3 highlight = i.color.b * lightColor * matcap * _HighlightColor.rgb * 2;
				
				col.a = smoothstep(_EdgeMin, _EdgeMin + _EdgeSoft, col.a);
				col.rgb = col.rgb * detail.rgb * i.color.r * (_Overbright + 1);	
				
				col = fixed4(highlight + col.rgb * lightColor, col.a * _TintColor.a + length(highlight * col.a)*0.5);
				
				UNITY_APPLY_FOG(i.fogCoord, col);
		
				return col;
			}
			ENDCG 
		}
	}	
}
Fallback "Paricles/Alpha Blended"
}