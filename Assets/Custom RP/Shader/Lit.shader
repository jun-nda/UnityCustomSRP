Shader "Custom RP/Lit" {
	Properties {
		// 透明效果用的贴图的名字，在引擎里是写死的，就是这俩。
		// 但是我们外面想保持原有的样子，就只能把这俩隐藏，然后手动复制一下属性
		[HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
		[HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
		
		_BaseMap("Texture", 2D) = "white" {}
		[HDR]_BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
		[KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
		[NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
		[HDR] _EmissionColor("Emission", Color) = (0.0, 0.0, 0.0, 0.0)
		[Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0

		// BRDF
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5

		[Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]_DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
	}
	
	SubShader {
		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "LitInput.hlsl"
		ENDHLSL
		
		Pass {
			Tags {
				"LightMode" = "CustomLit"
			}
			
			Blend [_SrcBlend] [_DstBlend]

			HLSLPROGRAM
			// shader_feature可以认为是multi_complie的子集，
			//其与multi_complie最大的不同就是此关键字的声明变体是材质球层级的（multi_complie是全局），
			//只能通过美术在制作时调整相应材质，未被选择的变体会在打包的时候被舍弃（multi_complie不会，
			//所以其声明的变体是不能通过代码控制的（打包后会出问题）。上面的声明方式中省略了“_”，
			//这只是一种简写方式，其作用与下两行相同
			#pragma shader_feature _CLIPPING
			#pragma shader_feature _PREMULTIPLY_ALPHA
			// _ 表示2*2
			// multi_compile 引擎会自动生成所有的变体， 用于替代分支
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
			#pragma multi_compile _ LIGHTMAP_ON //
			#pragma multi_compile_instancing
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment
			#include "LitPass.hlsl"
			ENDHLSL
		}

		// shadow pass
		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0 // dont need color, only depth

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER

			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}

		// Meta Pass
		Pass {
			Tags {
				"LightMode" = "Meta"
			}

			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex MetaPassVertex
			#pragma fragment MetaPassFragment
			#include "MetaPass.hlsl"
			ENDHLSL
		}
		
	}

	CustomEditor "CustomShaderGUI"
}