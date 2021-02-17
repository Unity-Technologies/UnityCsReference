using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    // TODO: Hack. We need this because EditorGUIUtility.systemCopyBuffer is always empty on Mac in BatchMode.
    static class BuilderEditorUtility
    {
        static string s_FakeSystemCopyBuffer = string.Empty;

        public static string systemCopyBuffer
        {
            get
            {
                if (Application.isBatchMode && Application.platform == RuntimePlatform.OSXEditor)
                    return s_FakeSystemCopyBuffer;

                return EditorGUIUtility.systemCopyBuffer;
            }

            set
            {
                if (Application.isBatchMode && Application.platform == RuntimePlatform.OSXEditor)
                    s_FakeSystemCopyBuffer = value;
                else
                    EditorGUIUtility.systemCopyBuffer = value;
            }
        }

        public static bool CopyBufferMatchesTarget(VisualElement target)
        {
            if (target == null)
                return false;

            var copyBuffer = systemCopyBuffer;

            if (string.IsNullOrEmpty(copyBuffer))
                return false;

            if (IsUxml(copyBuffer) && (target.GetFirstOfType<BuilderHierarchy>() != null || target.GetFirstOfType<BuilderViewport>() != null))
                return true;

            if (IsUss(copyBuffer) && target.GetFirstOfType<BuilderStyleSheets>() != null)
                return true;

            // Unknown string.
            return false;
        }

        public static bool IsUxml(string buffer)
        {
            if (string.IsNullOrEmpty(buffer))
                return false;

            var trimmedBuffer = buffer.Trim();
            return trimmedBuffer.StartsWith("<") && trimmedBuffer.EndsWith(">");
        }

        public static bool IsUss(string buffer)
        {
            if (string.IsNullOrEmpty(buffer))
                return false;

            var trimmedBuffer = buffer.Trim();
            return trimmedBuffer.EndsWith("}");
        }
    }
}
