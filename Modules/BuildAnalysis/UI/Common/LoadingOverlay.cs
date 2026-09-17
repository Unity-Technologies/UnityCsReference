// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal sealed class LoadingOverlay : VisualElement
    {
        private static readonly string ussClassName = "loading-overlay";
        private static readonly string spinnerUssClassName = ussClassName + "__spinner";
        private static readonly string labelUssClassName = ussClassName + "__label";

        private const string k_UssPath = "BuildAnalysis/StyleSheets/LoadingOverlay.uss";
        private const int k_FrameCount = 12;
        private const long k_FrameIntervalMs = 80; // 12 frames ≈ one rotation per second

        [NoAutoStaticsCleanup] // lazy cache of built-in editor spinner icons loaded by fixed name; the assets survive code reload and the field re-initialises on first access
        private static Texture[] s_Frames;

        private readonly Image m_Spinner;
        private readonly Label m_Label;
        private IVisualElementScheduledItem m_Spin;
        private int m_Frame;

        public LoadingOverlay()
        {
            var styleSheet = EditorGUIUtility.LoadRequired(k_UssPath) as StyleSheet;
            styleSheets.Add(styleSheet);
            AddToClassList(ussClassName);

            pickingMode = PickingMode.Position; // block clicks to the content beneath while shown
            style.display = DisplayStyle.None;

            s_Frames ??= LoadFrames();

            m_Spinner = new Image { image = s_Frames[0] };
            m_Spinner.AddToClassList(spinnerUssClassName);
            Add(m_Spinner);

            m_Label = new Label();
            m_Label.AddToClassList(labelUssClassName);
            Add(m_Label);
        }

        public void Show(string message)
        {
            m_Label.text = message ?? string.Empty;
            style.display = DisplayStyle.Flex;
            m_Spin ??= schedule.Execute(Advance).Every(k_FrameIntervalMs);
            m_Spin.Resume();
        }

        public void Hide()
        {
            style.display = DisplayStyle.None;
            m_Spin?.Pause();
        }

        private void Advance()
        {
            m_Frame = (m_Frame + 1) % s_Frames.Length;
            m_Spinner.image = s_Frames[m_Frame];
        }

        private static Texture[] LoadFrames()
        {
            var frames = new Texture[k_FrameCount];
            for (var i = 0; i < frames.Length; i++)
                frames[i] = EditorGUIUtility.IconContent("WaitSpin" + i.ToString("00")).image;
            return frames;
        }
    }
}
