// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Particles/FXVille Blood Fast" {
Properties {
	_Columns ("Flipbook Columns", int) = 1
	_Rows ("Flipbook Rows", int) = 1
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_ChannelMask ("Channel Mask Color", Color) = (0,0,0,1)
	_MainTex ("Particle Texture", 2D) = "white" {}
	
	_EdgeMin ("SmoothStep Edge Min", float) = 0.05
	_EdgeSoft ("SmoothStep Softness", float) = 0.05

	_Detail ("Detail Tex", 2D) = "gray" {}
	_DetailTile ("Detail Tiling", float) = 6.0
	_DetailPan ("Detail Alpha Pan", float) = 0.1
	_DetailAlphaAffect ("Detail Alpha Affect", float) =  1.0
	_DetailBrightAffect("Detail Brightness Affect", float) = 0.5
	
	_Overbright ("Overbright", float) = 0.0
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
			fixed4 _TintColor;
			fixed4 _ChannelMask;
			fixed _EdgeMin;
			fixed _EdgeSoft;
			fixed _Overbright;
			fixed _DetailTile;
			fixed _DetailPan;
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
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				
				v2f o;
			
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{				
				//Calculate Detail Texture. 
				//Detail texture can have its position randomized or be animated by changing the brigness of the green channel value.
				fixed4 detail = tex2D(_Detail, _DetailTile * i.texcoord + fixed2(i.color.y * 1.32 + i.color.a * _DetailPan, 0));
				detail.a = lerp(1, detail.a, _DetailAlphaAffect);
				detail.a *= lerp(1, detail.rgb, _DetailAlphaAffect);
				detail.rgb = lerp((1 - detail.rgb), detail.rgb, _DetailBrightAffect + 0.5);
				
				
				//Read main texture and declare col. Make sure that tex has an alpha channel. 
				//If needed, use unity's "alpha from brightness" option on the texture import settings.
				fixed4 tex = tex2D(_MainTex, i.texcoord);
				fixed4 col = fixed4(_TintColor.rgb, 1);
				
				tex.a = length(tex * _ChannelMask);
				col.a *= tex.a * i.color.a * detail.a;
				
				col.a = smoothstep(_EdgeMin, _EdgeMin + _EdgeSoft, col.a);
				col.rgb = col.rgb * detail.rgb * i.color.r * (_Overbright + 1);	
				
				col = fixed4(col.rgb, col.a * _TintColor.a);
				
				UNITY_APPLY_FOG(i.fogCoord, col);
		
				return col;
			}
			ENDCG 
		}
	}	
}
Fallback "Paricles/Alpha Blended"
}