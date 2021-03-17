// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

using System;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Utility/SerializationDebug.bindings.h")]
    internal sealed partial class SerializationDebug
    {
        internal static extern string ToYAMLString(Object obj);

        internal static void Log(Object obj)
        {
            Debug.Log(ToYAMLString(obj));
        }

        internal static void Log(SerializedObject serializerObject)
        {
            Debug.Log(GetLogString(serializerObject));
        }

        // GetLogString() is useful when a quick Object/SerializedObject dump is needed
        // It can be used directly from the debugger compared to Log which will print the object in UnityEditor console with some delay
        internal static string GetLogString(Object obj)
        {
            return ToYAMLString(obj);
        }

        private static string GetLogString(SerializedObject serializerObject)
        {
            string serializedObjectStr = "";

            SerializedProperty property = serializerObject.GetIterator();
            while (property.Next(property.hasVisibleChildren))
            {
                string propertyValue = "";
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Generic:
                        propertyValue = "Generic type value";
                        break;

                    case SerializedPropertyType.Integer:
                        propertyValue = $"{property.intValue}";
                        break;

                    case SerializedPropertyType.Boolean:
                        propertyValue = $"{property.boolValue}";
                        break;

                    case SerializedPropertyType.Float:
                        propertyValue = $"{property.floatValue}";
                        break;

                    case SerializedPropertyType.String:
                        propertyValue = property.stringValue;
                        break;

                    case SerializedPropertyType.Color:
                        propertyValue = $"{property.colorValue}";
                        break;

                    case SerializedPropertyType.ObjectReference:
                        propertyValue = $"{{instanceID: {property.objectReferenceInstanceIDValue} ({property.objectReferenceStringValue})}}";
                        break;

                    case SerializedPropertyType.LayerMask:
                        propertyValue = $"{property.layerMaskBits}";
                        break;

                    case SerializedPropertyType.Enum:
                        propertyValue = $"{property.enumValueIndex}";
                        break;

                    case SerializedPropertyType.Vector2:
                        propertyValue = $"{property.vector2Value}";
                        break;

                    case SerializedPropertyType.Vector3:
                        propertyValue = $"{property.vector3Value}";
                        break;

                    case SerializedPropertyType.Vector4:
                        propertyValue = $"{property.vector4Value}";
                        break;

                    case SerializedPropertyType.Rect:
                        propertyValue = $"{property.rectValue}";
                        break;

                    case SerializedPropertyType.ArraySize:
                        propertyValue = $"{property.intValue}";
                        break;

                    case SerializedPropertyType.Character:
                        propertyValue = $"{(char)property.intValue}";
                        break;

                    case SerializedPropertyType.AnimationCurve:
                        propertyValue = $"{property.animationCurveValue}";
                        break;

                    case SerializedPropertyType.Bounds:
                        propertyValue = $"{property.boundsValue}";
                        break;

                    case SerializedPropertyType.Gradient:
                        propertyValue = $"{property.gradientValue}";
                        break;

                    case SerializedPropertyType.Quaternion:
                        propertyValue = $"{property.quaternionValue}";
                        break;

                    case SerializedPropertyType.ExposedReference:
                        propertyValue = $"{{instanceID: {property.objectReferenceInstanceIDValue} ({property.objectReferenceStringValue})}}";
                        break;

                    case SerializedPropertyType.FixedBufferSize:
                        propertyValue = $"{property.fixedBufferSize}";
                        break;

                    case SerializedPropertyType.Vector2Int:
                        propertyValue = $"{property.vector2IntValue}";
                        break;

                    case SerializedPropertyType.Vector3Int:
                        propertyValue = $"{property.vector3IntValue}";
                        break;

                    case SerializedPropertyType.RectInt:
                        propertyValue = $"{property.rectIntValue}";
                        break;

                    case SerializedPropertyType.BoundsInt:
                        propertyValue = $"{property.boundsIntValue}";
                        break;

                    case SerializedPropertyType.ManagedReference:
                        propertyValue = $"{{instanceID: {property.objectReferenceInstanceIDValue} ({property.objectReferenceStringValue})}}";
                        break;

                    case SerializedPropertyType.Hash128:
                        propertyValue = $"{property.hash128Value}";
                        break;
                }

                if (property.prefabOverride)
                {
                    propertyValue += "(Prefab Override)";
                }

                serializedObjectStr += $"{property.propertyPath}: {propertyValue}\n";
            }

            return serializedObjectStr;
        }
    }
}
