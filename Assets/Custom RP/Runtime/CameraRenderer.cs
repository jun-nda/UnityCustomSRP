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

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

	// static ShaderTagId[] legacyShaderTagIds = {
	// 	new ShaderTagId("Always"),
	// 	new ShaderTagId("ForwardBase"),
	// 	new ShaderTagId("PrepassBase"),
	// 	new ShaderTagId("Vertex"),
	// 	new ShaderTagId("VertexLMRGBM"),
	// 	new ShaderTagId("VertexLM")
	// };

    // static Material errorMaterial;

	public void Render (ScriptableRenderContext context, Camera camera) {
		this.context = context;
		this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();
		if (!Cull()) {
			return;
		}

        Setup();
        DrawVisibleGeometry();
#if UNITY_EDITOR
        DrawUnsupportedShaders();
        DrawGizmos();
#endif
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
		context.Submit();
	}

    void DrawVisibleGeometry () {
        var sortingSettings = new SortingSettings(camera){
            criteria = SortingCriteria.CommonOpaque
        };
        // var sortingSettings = new SortingSettings(camera);
        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
        );
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

	bool Cull () {
		// ScriptableCullingParameters p;
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
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