// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Audio;

namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(IAudioGenerator.Serializable))]
    internal class AudioGeneratorSerializableInterfaceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty serializableStruct, GUIContent label)
        {
            var innerField = serializableStruct.FindPropertyRelative(nameof(IAudioGenerator.Serializable.Reference));
            // TODO: There's a bug where "allowSceneObjects" isn't recovered correctly from the relative property,
            // so while you can drag and drop scene references, you can't object pick them (yet).
            EditorGUI.ObjectField(position, innerField, typeof(IAudioGenerator), label);
        }
    }
}
