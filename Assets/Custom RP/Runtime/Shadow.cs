using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{

    const string bufferName = "Shadows";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;

    CullingResults cullingResults;

    ShadowSettings settings;

    const int maxShadowedDirectionalLightCount = 4, maxCascades = 4;
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias; // Configurable Biases
        public float nearPlaneOffset;
    }

    ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount * maxCascades];

    int ShadowedDirectionalLightCount;

    static int
    dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
    dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
    cascadeCountId = Shader.PropertyToID("_CascadeCount"),
    cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
    cascadeDataId = Shader.PropertyToID("_CascadeData"),
    // shadowDistanceId = Shader.PropertyToID("_ShadowDistance");
    shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
    shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades],
        cascadeData = new Vector4[maxCascades];

    static Matrix4x4[]
            dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

    // PCF
	static string[] directionalFilterKeywords = {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7",
	};

	static string[] cascadeBlendKeywords = {
		"_CASCADE_BLEND_SOFT",
		"_CASCADE_BLEND_DITHER"
	};
  
    static string[] shadowMaskKeywords = {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
    };

    bool useShadowMask;
    
    BatchCullingProjectionType batchCullingProType;
    public void Setup(
        ScriptableRenderContext context, CullingResults cullingResults,
        ShadowSettings settings
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;

        ShadowedDirectionalLightCount = 0;
        useShadowMask = false;
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    // vector里存的是我们需要的渲染数据，不一定是一个向量，xyzw或rgba一般被叫做通道channel
    public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f 
            )
        {
            LightBakingOutput lightBaking = light.bakingOutput;
            if (
                lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
                lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask
                ) {
                useShadowMask = true; // 打开shadowMask
            }
            if (!cullingResults.GetShadowCasterBounds(
                visibleLightIndex, out Bounds b
            )) {
                return new Vector3(-light.shadowStrength, 0f, 0f);
            }
            
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
                new ShadowedDirectionalLight
                {
                    visibleLightIndex = visibleLightIndex,
                    slopeScaleBias = light.shadowBias,
                    nearPlaneOffset = light.shadowNearPlane
                };
            return new Vector3(
                light.shadowStrength,
                settings.directional.cascadeCount * ShadowedDirectionalLightCount++,
                light.shadowNormalBias
        );
        }

        return Vector3.zero;
    }
    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            // 1×1 dummy texture 
            buffer.GetTemporaryRT(
                dirShadowAtlasId, 1, 1,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
            );
        }
        
        buffer.BeginSample(bufferName);
        SetKeywords(shadowMaskKeywords, useShadowMask ? 0 : QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 -1);
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        buffer.GetTemporaryRT(
            dirShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
        );
        // 修改渲染目标到 阴影贴图
        buffer.SetRenderTarget(
            dirShadowAtlasId,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        // atlasSize 为图集每维度像素个数
        int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
        // Debug.LogFormat("{0}", ShadowedDirectionalLightCount, settings.directional.cascadeCount);
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
		buffer.SetGlobalVectorArray(
			cascadeCullingSpheresId, cascadeCullingSpheres
		);

        float f = 1f - settings.directional.cascadeFade;
        // 取倒数，shader里面就不用除法了，乘法快一点
		buffer.SetGlobalVector(
			shadowDistanceFadeId,
			new Vector4(
                1f / settings.maxDistance, 1f / settings.distanceFade,
                1f / (1f - f * f)
            )
		);

        SetKeywords(
            directionalFilterKeywords, (int)settings.directional.filter - 1            
        );
        SetKeywords(
			cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1
		);
        buffer.SetGlobalVector(
			shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize)
		);
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

	void SetKeywords (string[] keywords, int enabledIndex) {
		// int enabledIndex = (int)settings.directional.filter - 1;
        //Debug.LogFormat("{0}", (int)settings.directional.filter);
		for (int i = 0; i < keywords.Length; i++) {
			if (i == enabledIndex) {
				buffer.EnableShaderKeyword(keywords[i]);
			}
			else {
				buffer.DisableShaderKeyword(keywords[i]);
			}
		}
	}

    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(
            offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
        ));
        return offset;
    }
    // why do this matrix?  atlas offset
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }

	void SetCascadeData (int index, Vector4 cullingSphere, float tileSize) {
        // normal bias
        // cullingSphere.w 为球体半径
        // tileSize为像素个数,直径长度/像素个数=每个像素的长度
        float texelSize = 2f * cullingSphere.w / tileSize;
        // for PCF
        float filterSize = texelSize * ((float)settings.directional.filter + 1f);
		cascadeData[index] = new Vector4(
			1f / cullingSphere.w,
			filterSize * 1.4142136f // 最坏情况下是斜着，texel为方形，所以要乘个√2
		);

        cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;
		cascadeCullingSpheres[index] = cullingSphere;
	}

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        // 注意这里的light是我们自己定义得灯光数据类
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
       
        var shadowSettings =
            new ShadowDrawingSettings(cullingResults, light.visibleLightIndex, batchCullingProType);

        int cascadeCount = settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.directional.CascadeRatios;
		float cullingFactor =
			Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);
        for (int i = 0; i < cascadeCount; i++)
        {
            // unity native method for light space data
            // 这里返回的应该就是正交矩阵了
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
            );

			// all light use the same, only compute once
            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }

            splitData.shadowCascadeBlendCullingFactor = cullingFactor; // culling bias
            shadowSettings.splitData = splitData;
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize), split
            );

            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
			buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
            buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
			ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }

    }

    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

}