// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Text;
using static UnityEditor.Search.Providers.AssetProvider;

namespace UnityEditor.Search.Providers
{
    class AssetProviderSelectors
    {
        [SearchExpressionSelector("^filename$", provider: type)]
        static object GetAssetFilename(SearchItem item)
        {
            return Path.GetFileName(GetAssetPath(item));
        }

        [SearchExpressionSelector("^type$", provider: type)]
        static string GetAssetType(SearchItem item)
        {
            if (GetAssetPath(item) is string assetPath)
                return AssetDatabase.GetMainAssetTypeAtPath(assetPath)?.Name;
            return null;
        }

        [SearchExpressionSelector("^extension$", provider: type)]
        static string GetAssetExtension(SearchItem item)
        {
            if (GetAssetPath(item) is string assetPath)
                return Path.GetExtension(assetPath).Substring(1);
            return null;
        }

        [SearchExpressionSelector(@"#(?<propertyPath>[\w\d\.]+)", 9999)]
        [SearchExpressionSelector("SerializedProperty/(?<propertyPath>.+)", 9999)]
        static object GetSerializedPropertyValue(SearchExpressionSelectorArgs args)
        {
            if (!(args["propertyPath"] is string propertyPath))
                return null;

            return FindItemProperty(args.current, propertyPath);
        }

        private static string ListAllProperties(SerializedObject so)
        {
            var sb = new StringBuilder();
            using (var property = so.GetIterator())
            {
                var next = property.NextVisible(true);
                while (next)
                {
                    sb.Append(property.name + ", ");
                    next = property.NextVisible(false);
                }
            }
            return sb.ToString();
        }

        static object FindItemProperty(SearchItem item, string propertyName)
        {
            var obj = item.ToObject();
            if (!obj)
                return null;

            using (var so = new SerializedObject(obj))
            {
                if (string.Equals(propertyName, "*", System.StringComparison.Ordinal))
                    return ListAllProperties(so);

                using (var property = so.GetIterator())
                {
                    var next = property.NextVisible(true);
                    while (next)
                    {
                        if (property.name.EndsWith(propertyName, System.StringComparison.OrdinalIgnoreCase))
                            return GetSerializedPropertyValue(property);
                        next = property.NextVisible(property.hasChildren);
                    }
                }
            }
            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Correctness", "UNT0008:Null propagation on Unity objects", Justification = "<Pending>")]
        static object GetSerializedPropertyValue(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: return p.intValue;
                case SerializedPropertyType.Boolean: return p.boolValue;
                case SerializedPropertyType.Float: return p.floatValue;
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Enum: return p.enumNames[p.enumValueIndex];
                case SerializedPropertyType.Bounds: return p.boundsValue.size.magnitude;
                case SerializedPropertyType.BoundsInt: return p.boundsIntValue.size.magnitude;
                case SerializedPropertyType.Color: return p.colorValue;
                case SerializedPropertyType.LayerMask: return p.layerMaskBits;
                case SerializedPropertyType.FixedBufferSize: return p.fixedBufferSize;
                case SerializedPropertyType.ArraySize: return p.arraySize;

                case SerializedPropertyType.Rect: return p.rectValue.ToString();
                case SerializedPropertyType.RectInt: return p.rectIntValue.ToString();

                case SerializedPropertyType.Vector2: return p.vector2Value.ToString();
                case SerializedPropertyType.Vector3: return p.vector3Value.ToString();
                case SerializedPropertyType.Vector4: return p.vector4Value.ToString();
                case SerializedPropertyType.AnimationCurve: return p.animationCurveValue.ToString();
                case SerializedPropertyType.Gradient: return p.gradientValue.ToString();
                case SerializedPropertyType.Quaternion: return p.quaternionValue.eulerAngles.ToString();
                case SerializedPropertyType.Vector2Int: return p.vector2IntValue.ToString();
                case SerializedPropertyType.Vector3Int: return p.vector3IntValue.ToString();
                case SerializedPropertyType.Hash128: return p.hash128Value.ToString();

                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue?.name;
                case SerializedPropertyType.ExposedReference: return p.exposedReferenceValue?.name;

                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.Generic:
                    break;
            }

            return null;
        }
    }
}
