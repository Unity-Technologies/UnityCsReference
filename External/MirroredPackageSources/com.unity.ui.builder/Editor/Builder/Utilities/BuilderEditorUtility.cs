using UnityEditor;
using UnityEngine;

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
    }
}
