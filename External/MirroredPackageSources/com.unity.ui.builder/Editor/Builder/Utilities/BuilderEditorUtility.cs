using System;
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

        static int SearchCharSeq(string numAsText, char c, int minSeqCount)
        {
            // check if there is a decimal number separator
            if (numAsText.Contains("."))
            {
                int lastIndex = numAsText.Length - 2; // ignore the last character as it is not relevant. E.g 0.1999992
                int indexOffset = 0;

                // search for the left most index of the sequence of characters
                while (numAsText[lastIndex - indexOffset] == c)
                {
                    indexOffset++;
                }

                // If the number of characters in the sequence is greater than the expected minimum then 
                // we assume a round off error.
                bool hasRoundOffError = (indexOffset >= minSeqCount);

                if (hasRoundOffError)
                {
                    return lastIndex - indexOffset;
                }
            }

            return -1;
        }

        public static float FixRoundOff(float value)
        {
            const int seqCount = 3;
            string str = value.ToString();
            // search a sequence of 9s at the end of the value
            int seqIndex = SearchCharSeq(str, '9', seqCount);

            // if there is no sequence of 9s then search sequence of 0s
            if (seqIndex == -1)
                seqIndex = SearchCharSeq(str, '0', seqCount);

            if (seqIndex != -1)
            {
                return (float)Math.Round(value, seqIndex + 1);
            }

            return value;
        }
    }
}
