// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor
{
    [CustomEditor(typeof(MovieImporter))]
    internal class MovieImporterInspector : AssetImporterEditor
    {
        private float m_quality;
        private float m_duration;
        private bool m_linearTexture;

        public static GUIContent linearTextureContent = EditorGUIUtility.TextContent("Bypass sRGB Sampling|Texture will not be converted from gamma space to linear when sampled. Enable for IMGUI textures and non-color textures.");

        // Don't show the imported movie as a separate editor
        public override bool showImportedObject { get { return false; } }

        public override bool HasModified()
        {
            MovieImporter importer = target as MovieImporter;
            return (importer.quality != m_quality || importer.linearTexture != m_linearTexture);
        }

        protected override void ResetValues()
        {
            MovieImporter importer = target as MovieImporter;

            m_quality = importer.quality;
            m_linearTexture = importer.linearTexture;
            // only read out this once (its pretty slow)
            m_duration = importer.duration;
        }

        protected override void Apply()
        {
            MovieImporter importer = target as MovieImporter;
            importer.quality = m_quality;
            importer.linearTexture = m_linearTexture;
        }

        public override void OnInspectorGUI()
        {
            MovieImporter importer = target as MovieImporter;

            if (importer != null)
            {
                GUILayout.BeginVertical();

                m_linearTexture = EditorGUILayout.Toggle(linearTextureContent, m_linearTexture);

                int bitrate = (int)(GetVideoBitrateForQuality(m_quality) + GetAudioBitrateForQuality(m_quality));
                float size = (bitrate / 8 *  m_duration);
                float kbsize = 1024.0f * 1024.0f;

                m_quality = EditorGUILayout.Slider("Quality", m_quality, 0.0f, 1.0f);
                GUILayout.Label(
                    string.Format("Approx. {0:0.00} " + (size < kbsize ? "kB" : "MB") + ", {1} kbps",
                        size / (size < kbsize ? 1024.0f : kbsize), bitrate / 1000), EditorStyles.helpBox);
                GUILayout.EndVertical();
            }

            ApplyRevertGUI();

            MovieTexture movie = assetEditor.target as MovieTexture;
            if (movie && movie.loop)
            {
                EditorGUILayout.Space();
                movie.loop = EditorGUILayout.Toggle("Loop", movie.loop);
                GUILayout.Label("The Loop setting in the Inspector is obsolete. Use the Scripting API to control looping instead.\n\nThe loop setting will be disabled on next re-import or by disabling it above.", EditorStyles.helpBox);
            }
        }

        double GetAudioBitrateForQuality(double f) { return (56000 + 200000 * (f)); }
        double GetVideoBitrateForQuality(double f) { return (100000 + 8000000 * (f)); }
        double GetAudioQualityForBitrate(double f) { return (f - 56000) / 200000; }
        double GetVideoQualityForBitrate(double f) { return (f - 100000) / 8000000; }
    }
}
