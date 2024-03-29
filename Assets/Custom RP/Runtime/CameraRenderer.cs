using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer {
	ScriptableRenderContext context;

	Camera camera;

	const string bufferName = "Render Camera";

	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

    CullingResults cullingResults;

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
	litShaderTagId = new ShaderTagId("CustomLit");

	Lighting lighting = new Lighting();

	public void Render (
		ScriptableRenderContext context, Camera camera,
		bool useDynamicBatching, bool useGPUInstancing,
		ShadowSettings shadowSettings
	) {
		this.context = context;
		this.camera = camera;

		//Debug.LogFormat("lalala: {0}", LightProbeProxyVolume.isFeatureSupported); 
        PrepareBuffer();
        PrepareForSceneWindow();
		if (!Cull(shadowSettings.maxDistance)) {
			return;
		}

		// shadows first, because objectrender need the shadow data
		buffer.BeginSample(SampleName);
		ExecuteBuffer();
		lighting.Setup(context, cullingResults,shadowSettings);
        buffer.EndSample(SampleName);
		
		Setup();
        
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
#if UNITY_EDITOR
        DrawUnsupportedShaders();
        DrawGizmos();
#endif
		lighting.Cleanup(); // before submit
        Submit();
	}

    void Setup () {
		context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
		// clear RenderTarget
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth, 
            flags == CameraClearFlags.Color,
         	flags == CameraClearFlags.Color ?
			camera.backgroundColor.linear : Color.clear
        );

        buffer.BeginSample(SampleName);
        ExecuteBuffer(); // nothing to execute, only avoid unexpected error 
	}

    void Submit () {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
		context.Submit(); // core submit
	}

    void DrawVisibleGeometry (bool useDynamicBatching, bool useGPUInstancing) {
        var sortingSettings = new SortingSettings(camera){
            criteria = SortingCriteria.CommonOpaque
        };
        // var sortingSettings = new SortingSettings(camera);
        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
        ){
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing,
			perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe
			                                        | PerObjectData.LightProbeProxyVolume
			                                        | PerObjectData.ShadowMask
			                                        | PerObjectData.OcclusionProbe
			                                        | PerObjectData.OcclusionProbeProxyVolume
			                                        | PerObjectData.ReflectionProbes
		};

		drawingSettings.SetShaderPassName(1, litShaderTagId);

		var filteringSettings = new FilteringSettings(RenderQueueRange.all);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

		context.DrawSkybox(camera);
	}

    void ExecuteBuffer () {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	bool Cull (float maxShadowDistance) {
		// ScriptableCullingParameters p;
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
			/*
			* It doesn't make sense to render shadows that are further away than the camera can see,
			* so take the minimum of the max shadow distance and the camera's far clip plane.
			*/
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}
}