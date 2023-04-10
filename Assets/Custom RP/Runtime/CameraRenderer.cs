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

	// static ShaderTagId[] legacyShaderTagIds = {
	// 	new ShaderTagId("Always"),
	// 	new ShaderTagId("ForwardBase"),
	// 	new ShaderTagId("PrepassBase"),
	// 	new ShaderTagId("Vertex"),
	// 	new ShaderTagId("VertexLMRGBM"),
	// 	new ShaderTagId("VertexLM")
	// };

    // static Material errorMaterial;

	Lighting lighting = new Lighting();

	public void Render (
		ScriptableRenderContext context, Camera camera,
		bool useDynamicBatching, bool useGPUInstancing,
		ShadowSettings shadowSettings
	) {
		this.context = context;
		this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();
		if (!Cull(shadowSettings.maxDistance)) {
			return;
		}

		// shadows first 
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
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth, 
            flags == CameraClearFlags.Color,
         	flags == CameraClearFlags.Color ?
			camera.backgroundColor.linear : Color.clear
        );

        buffer.BeginSample(SampleName);
        ExecuteBuffer();
		// context.SetupCameraProperties(camera);
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
			enableInstancing = useGPUInstancing
		};

		drawingSettings.SetShaderPassName(1, litShaderTagId);

		var filteringSettings = new FilteringSettings(RenderQueueRange.all);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

		context.DrawSkybox(camera);
        // Debug.Log("Hello World", camera);
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

	// void DrawUnsupportedShaders () {
    //     if (errorMaterial == null) {
	// 		errorMaterial =
	// 			new Material(Shader.Find("Hidden/InternalErrorShader"));
	// 	}
        
	// 	var drawingSettings = new DrawingSettings(
	// 		legacyShaderTagIds[0], new SortingSettings(camera)
	// 	){
	// 		overrideMaterial = errorMaterial
	// 	};

    //     for (int i = 1; i < legacyShaderTagIds.Length; i++) {
	// 		drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
	// 	}

	// 	var filteringSettings = FilteringSettings.defaultValue;
	// 	context.DrawRenderers(
	// 		cullingResults, ref drawingSettings, ref filteringSettings
	// 	);
	// }

}