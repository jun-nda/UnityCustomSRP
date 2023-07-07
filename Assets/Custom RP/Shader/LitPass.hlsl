#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

// 注意include顺序
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
// CBUFFER_START(UnityPerMaterial)
// float4 _BaseColor;
// CBUFFER_END

#if defined(LIGHTMAP_ON)
	#define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
	#define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
	#define TRANSFER_GI_DATA(input, output) \
        output.lightMapUV = input.lightMapUV * \
		unity_LightmapST.xy + unity_LightmapST.zw;

	#define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
    #define GI_ATTRIBUTE_DATA
    #define GI_VARYINGS_DATA
    #define TRANSFER_GI_DATA(input, output)
    #define GI_FRAGMENT_DATA(input) 0.0
#endif


struct Attributes {
	float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;

    GI_ATTRIBUTE_DATA // GI传递数据
    UNITY_VERTEX_INPUT_INSTANCE_ID // GPU instance
};

struct Varyings {
	float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL;
    float2 baseUV : VAR_BASE_UV;

    GI_VARYINGS_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex (Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);

    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip( output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	
	output.baseUV = TransformBaseUV(input.baseUV);

	return output;
}

float4 LitPassFragment (Varyings input) : SV_TARGET{
    UNITY_SETUP_INSTANCE_ID(input);
    
	float4 base = GetBase(input.baseUV);
#if defined(_CLIPPING)
	clip(base.a - GetCutoff(input.baseUV));
#endif
    // base.rgb = normalize(input.normalWS);

    // surface
	Surface surface;
    surface.position = input.positionWS;
	surface.normal = normalize(input.normalWS);
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.depth = -TransformWorldToView(input.positionWS).z;

    surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = GetMetallic(input.baseUV);
	surface.smoothness = GetSmoothness(input.baseUV);

    surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

    #if defined(_PREMULTIPLY_ALPHA)
        BRDF brdf = GetBRDF(surface, true);
    #else
        BRDF brdf = GetBRDF(surface);
    #endif
    
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface);// init GI
    float3 color = GetLighting(surface,brdf, gi);
	color += GetEmission(input.baseUV);
	
    // base.rgb = abs(length(input.normalWS) - 1.0) * 100.0;
	return float4(color, surface.alpha);
}
#endif