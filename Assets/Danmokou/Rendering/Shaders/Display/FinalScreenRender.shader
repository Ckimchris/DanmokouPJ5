﻿Shader "DMKCamera/FinalRender" {
	//See LetteredboxedInput for the code that handles dealing with mouse positions.
	Properties {
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _MonitorAspect("Monitor Aspect Ratio", float) = 1
		_LetterboxTex("Letterbox Texture", 2D) = "white" {}
		_LetterboxTint("Letterbox Tint", Color) = (.8314, 0,.32157,1)
	}
	SubShader {
		Tags {
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
			"Queue" = "Transparent"
		}
		Cull Off
		Lighting Off
		ZWrite Off
		//As the source tex is a render tex accumulating
		// premulted colors, we use the merge (1 1-SrcA),
		// but since the target tex is always blank, we can optimize with Blend Off.
		Blend Off

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct vertex {
				float4 loc  : POSITION;
				float2 uv	: TEXCOORD0;
			};

			struct fragment {
				float4 loc  : SV_POSITION;
				float2 uv	: TEXCOORD0;
			};
			
			fragment vert(vertex v) {
				fragment f;
				f.loc = UnityObjectToClipPos(v.loc);
				f.uv = v.uv - 0.5;
				return f;
			}
			
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D _LetterboxTex;
			float4 _LetterboxTex_TexelSize;
			float4 _LetterboxTint;

			float _MonitorAspect;
			
			float4 frag(fragment f) : SV_Target {
				float texAspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
				float ltexAspect = _LetterboxTex_TexelSize.z / _LetterboxTex_TexelSize.w;
				float monitorIsWider = step(texAspect, _MonitorAspect);
				float2 scaledUv = monitorIsWider * float2(f.uv.x * _MonitorAspect / texAspect, f.uv.y) +
					(1 - monitorIsWider) * float2(f.uv.x, f.uv.y * texAspect / _MonitorAspect);
				//Scale-to-fit: draw black bars on the sides in empty area
				float4 c = lerp(tex2D(_LetterboxTex, fmod(scaledUv + monitorIsWider * 0.5, 1) * float2(texAspect / ltexAspect, 1)) * _LetterboxTint,
					tex2D(_MainTex, scaledUv + 0.5),
					step(max(abs(scaledUv.x), abs(scaledUv.y)), 0.5));
				//The source is a render texture using premult colors and the output is consumed
				// as screen data. Screen data is expected to be premult, so we do not need to deconvert.
				return c;
			}
			ENDCG
		}
	}
}