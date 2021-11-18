// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class EditorUIElementsBridge : UIElementsBridge
    {
        private static string serializedPropertyCopyName = "SerializedPropertyCopyName";
        public override void SetWantsMouseJumping(int value)
        {
            EditorGUIUtility.SetWantsMouseJumping(value);
        }

        public static void RegisterSerializedPropertyBindCallback<TValueType, TField, TFieldValue>
            (BaseCompositeField<TValueType, TField, TFieldValue>  compositeField, TField field)
            where TField : TextValueField<TFieldValue>, new()
        {
            // TODO: Fix the callback since it is never received by the TField.
            field.RegisterCallback<SerializedPropertyBindEvent>(e =>
            {
                if (!(field.GetProperty(serializedPropertyCopyName) is SerializedProperty property))
                    return;
                var propertyCopy = property.Copy();

                var k = 0;
                while (k <= compositeField.propertyIndex)
                {
                    propertyCopy.Next(k == 0);
                    k++;
                }

                var f = (TField)e.target;
                f.SetProperty(serializedPropertyCopyName, propertyCopy);
                f.showMixedValue = propertyCopy.hasMultipleDifferentValues;

                compositeField.forceUpdateDisplay = true;

                compositeField.propertyIndex++;
            });
        }
    }
}
