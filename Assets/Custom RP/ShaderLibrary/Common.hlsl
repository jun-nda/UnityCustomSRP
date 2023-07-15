#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED

// 如果不定义，shadowMask数据不会被实例化
#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
	#define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_PREV_MATRIX_M unity_PreMatrixM
#define UNITY_PREV_MATRIX_I_M unity_PreMatrixIM
#define UNITY_MATRIX_I_V   unity_MatrixInvV

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl" // must be here
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

float DistanceSquared(float3 pA, float3 pB) {
	return dot(pA - pB, pA - pB);
}


// float3 TransformObjectToWorld (float3 positionOS) {
// 	return mul(unity_ObjectToWorld, float4(positionOS, 1.0)).xyz;
// }

// float4 TransformWorldToHClip (float3 positionWS) {
// 	return mul(unity_MatrixVP, float4(positionWS, 1.0));
// }

void ClipLOD (float2 positionCS, float fade) {
	#if defined(LOD_FADE_CROSSFADE)
	float dither = InterleavedGradientNoise(positionCS.xy, 0);
	clip(fade + (fade < 0.0 ? dither : -dither));
	#endif
}

#endif