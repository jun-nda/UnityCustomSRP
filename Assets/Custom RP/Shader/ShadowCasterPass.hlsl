#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

struct Attributes {
	float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID // GPU instance
};

struct Varyings {
	float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ShadowCasterPassVertex  (Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

    // output.positionCS.w only for correct w sign +-
    // 不太理解 乘W
    // Shadow Pancaking相关
    // clamp, 也就是超出近平面的物体，直接把它的z弄成最大的，这个w的值不确定，暂时先这么理解
    // #define UNITY_REVERSED_Z 0

    // unity 现在好像默认reversez了，不知道怎么改....
	#if UNITY_REVERSED_Z
		output.positionCS.z =
			min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
            // 这里的w是真的不太理解
			// min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);

	#else
        // 因为本身就在近平面和光源中间，目的是让物体贴在近平面，所以要把物体往远推，dx11的z的范围是[0-1]由近到远
        // 所以取z和w的最大值
		output.positionCS.z =
			max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
	#endif
	
	output.baseUV = TransformBaseUV(input.baseUV);

	return output;
}

void ShadowCasterPassFragment (Varyings input){
    UNITY_SETUP_INSTANCE_ID(input);
	float4 base = GetBase(input.baseUV);
	#if defined(_SHADOWS_CLIP)
	clip(base.a - GetCutoff(input.baseUV));
	#elif defined(_SHADOWS_DITHER)
	float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	clip(base.a - dither);
	#endif
}
#endif