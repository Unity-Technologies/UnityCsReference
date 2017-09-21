// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    [CustomEditor(typeof(LightmapParameters))]
    [CanEditMultipleObjects]
    class LightmapParametersEditor : Editor
    {
        SerializedProperty  m_Resolution;
        SerializedProperty  m_ClusterResolution;
        SerializedProperty  m_IrradianceBudget;
        SerializedProperty  m_IrradianceQuality;
        SerializedProperty  m_BackFaceTolerance;
        SerializedProperty  m_ModellingTolerance;
        SerializedProperty  m_EdgeStitching;
        SerializedProperty  m_SystemTag;
        SerializedProperty  m_IsTransparent;

        SerializedProperty  m_AOQuality;
        SerializedProperty  m_AOAntiAliasingSamples;
        SerializedProperty  m_BlurRadius;
        SerializedProperty  m_BakedLightmapTag;
        SerializedProperty  m_Pushoff;

        SerializedProperty  m_AntiAliasingSamples;
        SerializedProperty  m_DirectLightQuality;

        public void OnEnable()
        {
            m_Resolution                = serializedObject.FindProperty("resolution");
            m_ClusterResolution         = serializedObject.FindProperty("clusterResolution");
            m_IrradianceBudget          = serializedObject.FindProperty("irradianceBudget");
            m_IrradianceQuality         = serializedObject.FindProperty("irradianceQuality");
            m_BackFaceTolerance         = serializedObject.FindProperty("backFaceTolerance");
            m_ModellingTolerance        = serializedObject.FindProperty("modellingTolerance");
            m_EdgeStitching             = serializedObject.FindProperty("edgeStitching");
            m_IsTransparent             = serializedObject.FindProperty("isTransparent");
            m_SystemTag                 = serializedObject.FindProperty("systemTag");
            m_AOQuality                 = serializedObject.FindProperty("AOQuality");
            m_AOAntiAliasingSamples     = serializedObject.FindProperty("AOAntiAliasingSamples");
            m_BlurRadius                = serializedObject.FindProperty("blurRadius");
            m_AntiAliasingSamples       = serializedObject.FindProperty("antiAliasingSamples");
            m_DirectLightQuality        = serializedObject.FindProperty("directLightQuality");
            m_BakedLightmapTag          = serializedObject.FindProperty("bakedLightmapTag");
            m_Pushoff                   = serializedObject.FindProperty("pushoff");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Label(Styles.precomputedRealtimeGIContent, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Resolution, Styles.resolutionContent);
            EditorGUILayout.Slider(m_ClusterResolution, 0.1F, 1.0F, Styles.clusterResolutionContent);
            EditorGUILayout.IntSlider(m_IrradianceBudget, 32, 2048, Styles.irradianceBudgetContent);
            EditorGUILayout.IntSlider(m_IrradianceQuality, 512, 131072, Styles.irradianceQualityContent);
            EditorGUILayout.Slider(m_ModellingTolerance, 0.0f, 1.0f, Styles.modellingToleranceContent);
            EditorGUILayout.PropertyField(m_EdgeStitching, Styles.edgeStitchingContent);
            EditorGUILayout.PropertyField(m_IsTransparent, Styles.isTransparent);
            EditorGUILayout.PropertyField(m_SystemTag, Styles.systemTagContent);
            EditorGUILayout.Space();

            bool usesPathTracerBakeBackend = LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveCPU;

            GUILayout.Label(Styles.bakedGIContent, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(usesPathTracerBakeBackend))
            {
                EditorGUILayout.PropertyField(m_BlurRadius, Styles.blurRadiusContent);
            }
            EditorGUILayout.PropertyField(m_AntiAliasingSamples, Styles.antiAliasingSamplesContent);
            using (new EditorGUI.DisabledScope(usesPathTracerBakeBackend))
            {
                EditorGUILayout.PropertyField(m_DirectLightQuality, Styles.directLightQualityContent);
            }
            EditorGUILayout.PropertyField(m_BakedLightmapTag, Styles.bakedLightmapTagContent);
            EditorGUILayout.Slider(m_Pushoff, 0.0f, 1.0f, Styles.pushoffContent);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(usesPathTracerBakeBackend))
            {
                GUILayout.Label(Styles.bakedAOContent, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_AOQuality, Styles.aoQualityContent);
                EditorGUILayout.PropertyField(m_AOAntiAliasingSamples, Styles.aoAntiAliasingSamplesContent);
            }

            GUILayout.Label(Styles.generalGIContent, EditorStyles.boldLabel);
            EditorGUILayout.Slider(m_BackFaceTolerance, 0.0f, 1.0f, Styles.backFaceToleranceContent);

            serializedObject.ApplyModifiedProperties();
        }

        internal override void OnHeaderControlsGUI()
        {
            GUILayoutUtility.GetRect(10, 10, 16, 16, EditorStyles.layerMaskField);
            GUILayout.FlexibleSpace();
        }

        private class Styles
        {
            public static readonly GUIContent generalGIContent = EditorGUIUtility.TextContent("General GI|Settings used in both Precomputed Realtime Global Illumination and Baked Global Illumination.");
            public static readonly GUIContent precomputedRealtimeGIContent = EditorGUIUtility.TextContent("Precomputed Realtime GI|Settings used in Precomputed Realtime Global Illumination where it is precomputed how indirect light can bounce between static objects, but the final lighting is done at runtime. Lights, ambient lighting in addition to the materials and emission of static objects can still be changed at runtime. Only static objects can affect GI by blocking and bouncing light, but non-static objects can receive bounced light via light probes.");  // Reuse the label from the Lighting window
            public static readonly GUIContent resolutionContent = EditorGUIUtility.TextContent("Resolution|Realtime lightmap resolution in texels per world unit. This value is multiplied by the realtime resolution in the Lighting window to give the output lightmap resolution. This should generally be an order of magnitude less than what is common for baked lightmaps to keep the precompute time manageable and the performance at runtime acceptable. Note that if this is made more fine-grained, then the Irradiance Budget will often need to be increased too, to fully take advantage of this increased detail.");
            public static readonly GUIContent clusterResolutionContent = EditorGUIUtility.TextContent("Cluster Resolution|The ratio between the resolution of the clusters with which light bounce is calculated and the resolution of the output lightmaps that sample from these.");
            public static readonly GUIContent irradianceBudgetContent = EditorGUIUtility.TextContent("Irradiance Budget|The amount of data used by each texel in the output lightmap. Specifies how fine-grained a view of the scene an output texel has. Small values mean more averaged out lighting, since the light contributions from more clusters are treated as one. Affects runtime memory usage and to a lesser degree runtime CPU usage.");
            public static readonly GUIContent irradianceQualityContent = EditorGUIUtility.TextContent("Irradiance Quality|The number of rays to cast to compute which clusters affect a given output lightmap texel - the granularity of how this is saved is defined by the Irradiance Budget. Affects the speed of the precomputation but has no influence on runtime performance.");
            public static readonly GUIContent backFaceToleranceContent = EditorGUIUtility.TextContent("Backface Tolerance|The percentage of rays shot from an output texel that must hit front faces to be considered usable. Allows a texel to be invalidated if too many of the rays cast from it hit back faces (the texel is inside some geometry). In that case artefacts are avoided by cloning valid values from surrounding texels. For example, if backface tolerance is 0.0, the texel is rejected only if it sees nothing but backfaces. If it is 1.0, the ray origin is rejected if it has even one ray that hits a backface.");
            public static readonly GUIContent modellingToleranceContent = EditorGUIUtility.TextContent("Modelling Tolerance|Maximum size of gaps that can be ignored for GI.");
            public static readonly GUIContent edgeStitchingContent = EditorGUIUtility.TextContent("Edge Stitching|If enabled, ensures that UV charts (aka UV islands) in the generated lightmaps blend together where they meet so there is no visible seam between them.");
            public static readonly GUIContent systemTagContent = EditorGUIUtility.TextContent("System Tag|Systems are groups of objects whose lightmaps are in the same atlas. It is also the granularity at which dependencies are calculated. Multiple systems are created automatically if the scene is big enough, but it can be helpful to be able to split them up manually for e.g. streaming in sections of a level. The system tag lets you force an object into a different realtime system even though all the other parameters are the same.");
            public static readonly GUIContent bakedGIContent = EditorGUIUtility.TextContent("Baked GI|Settings used in Baked Global Illumination where direct and indirect lighting for static objects is precalculated and saved (baked) into lightmaps for use at runtime. This is useful when lights are known to be static, for mobile, for low end devices and other situations where there is not enough processing power to use Precomputed Realtime GI. You can toggle on each light whether it should be included in the bake.");  // Reuse the label from the Lighting window
            public static readonly GUIContent blurRadiusContent = EditorGUIUtility.TextContent("Blur Radius|The radius (in texels) of the post-processing filter that blurs baked direct lighting. This reduces aliasing artefacts and produces softer shadows.");
            public static readonly GUIContent antiAliasingSamplesContent = EditorGUIUtility.TextContent("Anti-aliasing Samples|The maximum number of times to supersample a texel to reduce aliasing. Progressive lightmapper supersamples the positions and normals buffers (part of the G-buffer) and hence the sample count is a multiplier on the amount of memory used for those buffers. Progressive lightmapper clamps the value to the [1;16] range.");
            public static readonly GUIContent directLightQualityContent = EditorGUIUtility.TextContent("Direct Light Quality|The number of rays used for lights with an area. Allows for accurate soft shadowing.");
            public static readonly GUIContent bakedAOContent = EditorGUIUtility.TextContent("Baked AO|Settings used in Baked Ambient Occlusion, where the information on dark corners and crevices in static geometry is baked. It is multiplied by indirect lighting when compositing the baked lightmap.");
            public static readonly GUIContent aoQualityContent = EditorGUIUtility.TextContent("Quality|The number of rays to cast for computing ambient occlusion.");
            public static readonly GUIContent aoAntiAliasingSamplesContent = EditorGUIUtility.TextContent("Anti-aliasing Samples|The maximum number of times to supersample a texel to reduce aliasing in ambient occlusion.");
            public static readonly GUIContent isTransparent = EditorGUIUtility.TextContent("Is Transparent|If enabled, the object appears transparent during GlobalIllumination lighting calculations. Backfaces are not contributing to and light travels through the surface. This is useful for emissive invisible surfaces.");
            public static readonly GUIContent bakedLightmapTagContent = EditorGUIUtility.TextContent("Baked Tag|An integer that lets you force an object into a different baked lightmap even though all the other parameters are the same. This can be useful e.g. when streaming in sections of a level.");
            public static readonly GUIContent pushoffContent = EditorGUIUtility.TextContent("Pushoff|The amount to push off geometry for ray tracing, in modelling units. It is applied to all baked light maps, so it will affect direct light, indirect light and AO. Useful for getting rid of unwanted AO or shadowing.");
        };
    }
}
