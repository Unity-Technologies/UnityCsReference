// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Search
{
    static class PropertySelectors
    {
        [SearchSelector(@"#(?<propertyPath>[\w\d\.]+)", 9999, printable: false)]
        [SearchSelector("SerializedProperty/(?<propertyPath>.+)", 9999, printable: false)]
        public static object GetSerializedPropertyValue(SearchSelectorArgs args)
        {
            if (!(args["propertyPath"] is string propertyPath))
                return null;
            return GetSerializedPropertyValue(args.current, propertyPath);
        }

        internal static object GetSerializedPropertyValue(SearchItem item, string propertyPath)
        {
            object value = null;
            var property = GetSerializedProperty(item, propertyPath, out var so);
            if (property != null)
                value = GetSerializedPropertyValue(property);

            property?.Dispose();
            so?.Dispose();

            return value;
        }

        internal static SerializedProperty GetSerializedProperty(SearchItem item, SearchColumn column, out SerializedObject so)
        {
            foreach (var m in SelectorManager.Match(column.selector, item.provider?.type))
            {
                var selectorArgs = new SearchSelectorArgs(m, item);
                if (selectorArgs.name == null)
                    continue;
                var property = GetSerializedProperty(item, selectorArgs.name, out so);
                if (property == null)
                {
                    so?.Dispose();
                    continue;
                }

                so.UpdateIfRequiredOrScript();
                return property;
            }

            so = null;
            return null;
        }

        internal static SerializedProperty GetSerializedProperty(SearchItem item, string propertyPath, out SerializedObject so)
        {
            var go = item.ToObject<GameObject>();
            if (go)
            {
                foreach (var c in go.GetComponents<Component>())
                {
                    var v = FindProperty(c, propertyPath, out so);
                    if (v != null)
                        return v;
                }
            }

            return FindProperty(item.ToObject<AssetImporter>(), propertyPath, out so) ??
                FindProperty(item.ToObject(), propertyPath, out so);
        }

        public static SerializedProperty FindProperty(UnityEngine.Object obj, string propertyPath, out SerializedObject so)
        {
            if (!obj)
            {
                so = null;
                return null;
            }

            so = new SerializedObject(obj);
            var property = so.FindProperty(propertyPath);
            if (property != null)
                return property;

            {

                property = so.FindProperty($"m_{propertyPath}");
                if (property != null)
                {
                    return property;
                }

                property = so.GetIterator();
                var next = property.NextVisible(true);
                while (next)
                {
                    if (property.name.EndsWith(propertyPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return property;
                    }
                    next = property.NextVisible(property.hasChildren);
                }

                so?.Dispose();
                so = null;
                return null;
            }
        }

        internal static object GetSerializedPropertyValue(SerializedProperty p)
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
                case SerializedPropertyType.FixedBufferSize: return p.fixedBufferSize;
                //case SerializedPropertyType.ArraySize: return p.arraySize;

                case SerializedPropertyType.Rect: return p.rectValue.ToString();
                case SerializedPropertyType.RectInt: return p.rectIntValue.ToString();

                case SerializedPropertyType.Vector2: return p.vector2Value.ToString();
                case SerializedPropertyType.Vector3: return p.vector3Value.ToString();
                case SerializedPropertyType.Vector4: return p.vector4Value.ToString();
                case SerializedPropertyType.AnimationCurve: return p.animationCurveValue.ToString();
                case SerializedPropertyType.Quaternion: return p.quaternionValue.eulerAngles.ToString();
                case SerializedPropertyType.Vector2Int: return p.vector2IntValue.ToString();
                case SerializedPropertyType.Vector3Int: return p.vector3IntValue.ToString();

                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue;
                case SerializedPropertyType.ExposedReference: return p.exposedReferenceValue;

                case SerializedPropertyType.Gradient: return p.gradientValue.ToString();
                case SerializedPropertyType.LayerMask: return p.layerMaskBits;
                case SerializedPropertyType.Hash128: return p.hash128Value.ToString();

                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.Generic:
                    break;
            }

            if (p.isArray)
                return p.arraySize;

            return null;
        }

        public static IEnumerable<SearchColumn> Enumerate(IEnumerable<SearchItem> items)
        {
            var descriptors = new List<SearchColumn>();
            var templates = GetTemplates(items.Where(e => e != null).Select(e => e.ToObject()).Where(e => e));

            foreach (var obj in templates)
            {
                var objType = obj.GetType();
                var columns = new List<SearchColumn>();
                FillSerializedPropertyColumns(obj, objType, columns);
                if (columns.Count <= 0)
                    continue;

                foreach (var c in columns)
                    c.content.image = Utils.FindTextureForType(objType);
                descriptors.AddRange(columns);
            }

            return descriptors;
        }

        static void FillSerializedPropertyColumns(UnityEngine.Object obj, Type objType, List<SearchColumn> columns)
        {
            var iconType = Utils.FindTextureForType(objType);
            using (var so = new SerializedObject(obj))
            {
                var p = so.GetIterator();
                var next = p.NextVisible(true);
                while (next)
                {
                    var supported = IsPropertyTypeSupported(p);
                    if (supported)
                    {
                        var column = new SearchColumn(
                            path: $"{objType.Name}/{p.propertyPath.Replace(".", "/")}",
                            selector: "#" + p.propertyPath,
                            provider: p.propertyType.ToString(),
                            content: new GUIContent(Utils.TrimText(p.displayName, 31), iconType, p.tooltip));
                        Styles.itemLabel.CalcMinMaxWidth(column.content, out column.width, out _);
                        if (p.hasVisibleChildren)
                            column.width = Mathf.Min(220, column.width);
                        columns.Add(column);
                    }

                    var isVector = p.propertyType == SerializedPropertyType.Vector3 ||
                        p.propertyType == SerializedPropertyType.Vector4 ||
                        p.propertyType == SerializedPropertyType.Quaternion ||
                        p.propertyType == SerializedPropertyType.Vector2;

                    next = p.NextVisible(supported && !p.isArray && !p.isFixedBuffer && !isVector);
                }
            }
        }

        static bool IsPropertyTypeSupported(SerializedProperty p)
        {
            if (p.propertyType == SerializedPropertyType.Generic)
            {
                if (string.Equals(p.type, "map", StringComparison.Ordinal))
                    return false;
                if (string.Equals(p.type, "Matrix4x4f", StringComparison.Ordinal))
                    return false;
            }

            return p.propertyType != SerializedPropertyType.LayerMask &&
                p.propertyType != SerializedPropertyType.Character &&
                p.propertyType != SerializedPropertyType.ArraySize &&
                !p.isArray && !p.isFixedBuffer && p.propertyPath.LastIndexOf('[') == -1;
        }

        static IEnumerable<UnityEngine.Object> GetTemplates(IEnumerable<UnityEngine.Object> objects)
        {
            var seenTypes = new HashSet<Type>();
            foreach (var obj in objects)
            {
                var ct = obj.GetType();
                if (!seenTypes.Contains(ct))
                {
                    seenTypes.Add(ct);
                    yield return obj;
                }

                if (obj is GameObject go)
                {
                    foreach (var comp in go.GetComponents<Component>())
                    {
                        if (!comp)
                            continue;
                        ct = comp.GetType();
                        if (!seenTypes.Contains(ct))
                        {
                            seenTypes.Add(ct);
                            yield return comp;
                        }
                    }
                }

                var path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    var importer = AssetImporter.GetAtPath(path);
                    if (importer)
                    {
                        var it = importer.GetType();
                        if (it != typeof(AssetImporter) && !seenTypes.Contains(it))
                        {
                            seenTypes.Add(it);
                            yield return importer;
                        }
                    }
                }
            }
        }

        [SearchColumnProvider("Color")]
        public static void InitializeColorColumn(SearchColumn column)
        {
            column.drawer = args =>
            {
                if (args.value is Color c)
                    return EditorGUI.ColorField(args.rect, GUIContent.none, c, showEyedropper: false, showAlpha: true, hdr: false);
                return args.value;
            };
        }

        [SearchColumnProvider("Texture2D")]
        public static void InitializeTextureColumn(SearchColumn column)
        {
            column.drawer = args =>
            {
                if (args.value is Texture t)
                    GUI.DrawTexture(args.rect, t, ScaleMode.ScaleToFit);
                return args.value;
            };
        }

        static void DrawObjectField(Rect rect, UnityEngine.Object obj)
        {
            var mouseInRect = rect.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
            {
                var temp = EditorGUIUtility.ObjectContent(obj, obj.GetType());
                Styles.readOnlyObjectField.Draw(rect, temp, -1, false, mouseInRect);
            }
            else if (Event.current.type == EventType.MouseDown && mouseInRect)
            {
                Utils.SelectObject(obj, ping: true);
                Event.current.Use();
            }
        }

        internal static object DrawObjectReference(Rect rect, object value)
        {
            if (value is UnityEngine.Object obj)
            {
                DrawObjectField(rect, obj);
            }
            else if (value is string s && GlobalObjectId.TryParse(s, out var gid))
            {
                obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                if (obj)
                    DrawObjectField(rect, obj);
            }

            return value?.ToString();
        }

        [SearchColumnProvider("ObjectReference")]
        public static void InitializeObjectReferenceColumn(SearchColumn column)
        {
            column.drawer = args => DrawObjectReference(args.rect, args.value);
        }

        [SearchColumnProvider("ObjectPath")]
        public static void InitializeObjectPathColumn(SearchColumn column)
        {
            column.getter = GetObjectPath;
        }

        private static object GetObjectPath(SearchColumnEventArgs args)
        {
            var value = args.column.SelectValue(args.item, args.context);
            if (value is UnityEngine.Object obj)
            {
                var objPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(objPath))
                    return objPath;
                if (obj is GameObject go)
                    return SearchUtils.GetTransformPath(go.transform);
            }
            return value;
        }

        static class SerializedPropertyColumnProvider
        {
            [SearchColumnProvider("Experimental/SerializedProperty")]
            public static void InitializeSerializedPropertyColumn(SearchColumn column)
            {
                column.getter = args => Getter(args.item, args.column);
                column.setter = args => Setter(args.item, args.column, args.value, args.multiple);
                column.drawer = args => Drawer(args.rect, args.value);
                column.comparer = args => Comparer(args.lhs.value, args.rhs.value, args.sortAscending);
            }

            static class DefaultDelegates
            {
                public static int CompareColorHandler(SerializedProperty lhsObjCast, SerializedProperty rhsObjCast)
                {
                    Color.RGBToHSV(lhsObjCast.colorValue, out float lh, out _, out _);
                    Color.RGBToHSV(rhsObjCast.colorValue, out float rh, out _, out _);

                    return lh.CompareTo(rh);
                }

                public static int CompareFloatHandler(SerializedProperty lhsObjCast, SerializedProperty rhsObjCast)
                {
                    return lhsObjCast.floatValue.CompareTo(rhsObjCast.floatValue);
                }

                public static int CompareCheckboxHandler(SerializedProperty lhsObjCast, SerializedProperty rhsObjCast)
                {
                    return lhsObjCast.boolValue.CompareTo(rhsObjCast.boolValue);
                }

                public static int CompareIntHandler(SerializedProperty lhs, SerializedProperty rhs)
                {
                    return lhs.intValue.CompareTo(rhs.intValue);
                }

                public static int CompareEnumHandler(SerializedProperty lhs, SerializedProperty rhs)
                {
                    return lhs.enumValueIndex.CompareTo(rhs.enumValueIndex);
                }

                public static int CompareVector2Handler(SerializedProperty lhsObjCast, SerializedProperty rhsObjCast)
                {
                    return lhsObjCast.vector2Value.magnitude.CompareTo(rhsObjCast.vector2Value.magnitude);
                }

                public static int CompareVector2IntHandler(SerializedProperty lhsObjCast, SerializedProperty rhsObjCast)
                {
                    return lhsObjCast.vector2IntValue.magnitude.CompareTo(rhsObjCast.vector2IntValue.magnitude);
                }

                public static int CompareVector3Handler(SerializedProperty lhsObjCast, SerializedProperty rhsObjCast)
                {
                    return lhsObjCast.vector3Value.magnitude.CompareTo(rhsObjCast.vector3Value.magnitude);
                }

                public static int CompareVector3IntHandler(SerializedProperty lhsObjCast, SerializedProperty rhsObjCast)
                {
                    return lhsObjCast.vector3IntValue.magnitude.CompareTo(rhsObjCast.vector3IntValue.magnitude);
                }

                public static int CompareVector4Handler(SerializedProperty lhsObjCast, SerializedProperty rhsObjCast)
                {
                    return lhsObjCast.vector4Value.magnitude.CompareTo(rhsObjCast.vector4Value.magnitude);
                }

                internal static int CompareReferencesHandler(SerializedProperty lhs, SerializedProperty rhs)
                {
                    return string.CompareOrdinal(lhs.objectReferenceValue?.name, rhs.objectReferenceValue?.name);
                }

                public static object DrawCheckboxHandler(Rect r, SerializedProperty prop)
                {
                    var off = Math.Max(0.0f, (r.width / 2) - 8);
                    r.x += off;
                    r.width -= off;

                    EditorGUI.PropertyField(r, prop, GUIContent.none);

                    return prop;
                }

                public static object DrawQuaternionHandler(Rect r, SerializedProperty prop)
                {
                    var eulerAngles = new Vector3();

                    EditorGUI.BeginChangeCheck();

                    for (int i = 0; i < 3; i++)
                        eulerAngles[i] = float.Parse(prop.quaternionValue.eulerAngles[i].ToString("F7"));

                    var newValue = EditorGUI.Vector3Field(r, "", eulerAngles);

                    if (EditorGUI.EndChangeCheck())
                        prop.quaternionValue = Quaternion.Euler(newValue);

                    return prop;
                }

                // TODO
                public static object DrawVector4Handler(Rect r, SerializedProperty prop)
                {
                    return null;
                }

                public static object DrawDefaultHandler(Rect r, SerializedProperty prop)
                {
                    var fieldContent = Utils.GUIContentTemp(string.Empty);
                    using (new PropertyFieldSettingsScope(prop.propertyType, fieldContent))
                        EditorGUI.PropertyField(r, prop, fieldContent);

                    return prop;
                }

                readonly struct PropertyFieldSettingsScope : IDisposable
                {
                    readonly bool prevWideMode;
                    readonly float prevLabelWidth;

                    public PropertyFieldSettingsScope(SerializedPropertyType type, GUIContent fieldContent)
                    {
                        prevWideMode = EditorGUIUtility.wideMode;
                        prevLabelWidth = EditorGUIUtility.labelWidth;

                        if (type == SerializedPropertyType.Float)
                        {
                            fieldContent.text = "\u2022";
                            EditorGUIUtility.wideMode = false;
                            EditorGUIUtility.labelWidth = 10f;
                        }
                    }

                    public void Dispose()
                    {
                        EditorGUIUtility.wideMode = prevWideMode;
                        EditorGUIUtility.labelWidth = prevLabelWidth;
                    }
                }
            }

            public static object Getter(SearchItem item, SearchColumn column)
            {
                var p = GetSerializedProperty(item, column, out var _);
                return p;
            }

            public static void Setter(SearchItem item, SearchColumn column, object newValue, bool multiple)
            {
                if (!(newValue is SerializedProperty newValueProperty))
                    return;

                if (!multiple)
                {
                    newValueProperty.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    var property = GetSerializedProperty(item, column, out var so);
                    if (property != null && so != null)
                    {
                        so.CopyFromSerializedProperty(newValueProperty);
                        so.ApplyModifiedProperties();

                        property.Dispose();
                        so.Dispose();
                    }
                }
            }

            public static object Drawer(Rect r, object prop)
            {
                if (!(prop is SerializedProperty sp))
                    return null;

                switch (sp.propertyType)
                {
                    case SerializedPropertyType.Boolean: return DefaultDelegates.DrawCheckboxHandler(r, sp);
                    case SerializedPropertyType.Quaternion: return DefaultDelegates.DrawQuaternionHandler(r, sp);
                    case SerializedPropertyType.Vector4: return DefaultDelegates.DrawVector4Handler(r, sp);
                    default: return DefaultDelegates.DrawDefaultHandler(r, sp);
                }
            }

            public static int Comparer(object lhsObj, object rhsObj, bool sortAscending)
            {
                if (lhsObj == null && rhsObj == null)
                    return 0;

                if (lhsObj == null && rhsObj != null)
                    return sortAscending ? 1 : -1;

                if (lhsObj != null && rhsObj == null)
                    return sortAscending ? -1 : 1;

                if (!(lhsObj is SerializedProperty lhsProp) ||
                    !(rhsObj is SerializedProperty rhsProp) ||
                    lhsProp.propertyType != rhsProp.propertyType)
                    return 0;

                switch (lhsProp.propertyType)
                {
                    case SerializedPropertyType.Enum: return DefaultDelegates.CompareEnumHandler(lhsProp, rhsProp);
                    case SerializedPropertyType.Float: return DefaultDelegates.CompareFloatHandler(lhsProp, rhsProp);
                    case SerializedPropertyType.Integer: return DefaultDelegates.CompareIntHandler(lhsProp, rhsProp);
                    case SerializedPropertyType.Color: return DefaultDelegates.CompareColorHandler(lhsProp, rhsProp);
                    case SerializedPropertyType.Boolean: return DefaultDelegates.CompareCheckboxHandler(lhsProp, rhsProp);
                    case SerializedPropertyType.Vector2: return DefaultDelegates.CompareVector2Handler(lhsProp, rhsProp);
                    case SerializedPropertyType.Vector2Int: return DefaultDelegates.CompareVector2IntHandler(lhsProp, rhsProp);
                    case SerializedPropertyType.Vector3: return DefaultDelegates.CompareVector3Handler(lhsProp, rhsProp);
                    case SerializedPropertyType.Vector3Int: return DefaultDelegates.CompareVector3IntHandler(lhsProp, rhsProp);
                    case SerializedPropertyType.Vector4: return DefaultDelegates.CompareVector4Handler(lhsProp, rhsProp);
                    case SerializedPropertyType.ObjectReference: return DefaultDelegates.CompareReferencesHandler(lhsProp, rhsProp);
                }

                return lhsProp.GetHashCode().CompareTo(rhsProp.GetHashCode());
            }
        }
    }
}
