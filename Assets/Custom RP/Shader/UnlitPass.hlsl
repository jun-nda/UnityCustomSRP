#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

// CBUFFER_START(UnityPerMaterial)
// float4 _BaseColor;
// CBUFFER_END

// TEXTURE2D(_BaseMap);
// SAMPLER(sampler_BaseMap);

// UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
// UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
// UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
// UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
// UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

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

Varyings UnlitPassVertex (Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

	output.baseUV = TransformBaseUV(input.baseUV);

	return output;
}

float4 UnlitPassFragment (Varyings input) : SV_TARGET{
    UNITY_SETUP_INSTANCE_ID(input);

	float4 base = GetBase(input.baseUV);
#if defined(_CLIPPING)
	clip(base.a - GetCutoff(input.baseUV));
#endif
	return base;
}
#endif