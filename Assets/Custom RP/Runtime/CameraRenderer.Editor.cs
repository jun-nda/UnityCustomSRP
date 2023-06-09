using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// partial class of CameraRenderer
partial class CameraRenderer {

	partial void DrawGizmos ();
	partial void DrawUnsupportedShaders ();
	partial void PrepareForSceneWindow ();
	partial void PrepareBuffer ();// 每个相机单独作用域？ 看起来像framebuffer

#if UNITY_EDITOR
	string SampleName { get; set; }

	static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

    static Material errorMaterial;


	partial void DrawGizmos () {
		if (Handles.ShouldRenderGizmos()) {
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}

	partial void DrawUnsupportedShaders () {
        if (errorMaterial == null) {
			errorMaterial =
				new Material(Shader.Find("Hidden/InternalErrorShader"));
		}
        
		var drawingSettings = new DrawingSettings(
			legacyShaderTagIds[0], new SortingSettings(camera)
		){
			overrideMaterial = errorMaterial
		};

        for (int i = 1; i < legacyShaderTagIds.Length; i++) {
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}

		var filteringSettings = FilteringSettings.defaultValue;
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}

	partial void PrepareForSceneWindow () {
		if (camera.cameraType == CameraType.SceneView) {
			ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
		}
	}

	partial void PrepareBuffer () {
		UnityEngine.Profiling.Profiler.BeginSample("Editor Only");
		buffer.name = camera.name;
		UnityEngine.Profiling.Profiler.EndSample();
	}
#else
	const string SampleName = bufferName;

#endif

}