// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    static class PropertySelectors
    {
        [SearchSelector(@"#(?<propertyPath>[\w\d\.\[\]]+)", 9999, printable: false)]
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
            var property = so.FindPropertyIgnoreCase(propertyPath);
            if (property != null)
                return property;

            using (var view = SearchMonitor.GetView())
            {
                var documentKey = SearchUtils.GetDocumentKey(obj);
                var recordKey = PropertyDatabase.CreateRecordKey(documentKey, PropertyDatabase.CreatePropertyHash($"{obj.GetType().Name}.{propertyPath}"));
                if (view.TryLoadAlias(recordKey, out var resolvedPropertyPath))
                {
                    if (string.IsNullOrEmpty(resolvedPropertyPath))
                    {
                        so?.Dispose();
                        return null;
                    }

                    var resolvedProperty = so.FindPropertyIgnoreCase(resolvedPropertyPath);
                    if (resolvedProperty != null)
                        return resolvedProperty;
                }

                property = so.FindPropertyIgnoreCase($"m_{propertyPath}");
                if (property != null)
                {
                    view.StoreAlias(recordKey, property.propertyPath);
                    return property;
                }

                property = so.GetIterator();
                var next = property.NextVisible(true);
                while (next)
                {
                    var propertyName = property.name.Replace(" ", "");
                    if (propertyName.Equals(propertyPath, StringComparison.OrdinalIgnoreCase))
                    {
                        view.StoreAlias(recordKey, property.propertyPath);
                        return property;
                    }
                    next = property.NextVisible(property.hasChildren);
                }

                view.StoreAlias(recordKey, string.Empty);
                so?.Dispose();
                so = null;

                return null;
            }
        }

        internal static string GetEnumValue(SerializedProperty p)
        {
            return p.enumNames[p.enumValueIndex].Replace(" ", "");
        }

        internal static string GetEnumValue(Type type, SerializedProperty p)
        {
            return type.GetEnumValues().GetValue(p.intValue).ToString().Replace(" ", "");
        }

        internal static object GetSerializedPropertyValue(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Character:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Integer:
                    return p.intValue;

                case SerializedPropertyType.Boolean: return p.boolValue;
                case SerializedPropertyType.Float: return p.floatValue;
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Enum: return GetEnumValue(p);
                case SerializedPropertyType.Bounds: return p.boundsValue.size.magnitude;
                case SerializedPropertyType.BoundsInt: return p.boundsIntValue.size.magnitude;
                case SerializedPropertyType.Color: return p.colorValue;
                case SerializedPropertyType.FixedBufferSize: return p.fixedBufferSize;

                case SerializedPropertyType.Rect: return Utils.ToString(p.rectValue);
                case SerializedPropertyType.RectInt: return Utils.ToString(p.rectIntValue);

                case SerializedPropertyType.Vector2: return Utils.ToString(p.vector2Value, 2);
                case SerializedPropertyType.Vector3: return Utils.ToString(p.vector3Value, 3);
                case SerializedPropertyType.Vector4: return Utils.ToString(p.vector4Value, 4);
                case SerializedPropertyType.Vector2Int: return Utils.ToString(p.vector2IntValue);
                case SerializedPropertyType.Vector3Int: return Utils.ToString(p.vector3IntValue);

                case SerializedPropertyType.AnimationCurve: return p.animationCurveValue.ToString();
                case SerializedPropertyType.Quaternion: return Utils.ToString(p.quaternionValue.eulerAngles);

                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue;
                case SerializedPropertyType.ExposedReference: return p.exposedReferenceValue;

                case SerializedPropertyType.Gradient: return p.gradientValue.ToString();
                case SerializedPropertyType.LayerMask: return p.layerMaskBits;
                case SerializedPropertyType.RenderingLayerMask: return p.layerMaskBits;
                case SerializedPropertyType.Hash128: return p.hash128Value.ToString();

                case SerializedPropertyType.ManagedReference:
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
            var templates = SearchUtils.GetTemplates(items.Where(e => e != null).Select(e => e.ToObject()).Where(e => e));

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
                SearchUtils.IterateSupportedProperties(so, p =>
                {
                    var column = new SearchColumn(
                            path: $"{objType.Name}/{p.propertyPath.Replace(".", "/")}",
                            selector: "#" + p.propertyPath,
                            provider: p.propertyType.ToString(),
                            content: new GUIContent(Utils.TrimText(p.displayName, 31), iconType, p.tooltip));
                    ItemSelectors.Styles.itemLabel.CalcMinMaxWidth(column.content, out column.width, out _);
                    if (p.hasVisibleChildren)
                        column.width = Mathf.Min(220, column.width);
                    columns.Add(column);
                });
            }
        }

        [SearchColumnProvider("Color")]
        public static void InitializeColorColumn(SearchColumn column)
        {
            column.cellCreator = col => new ColorField()
            {
                showAlpha = true,
                hdr = false,
                showEyeDropper = false,
                pickingMode = PickingMode.Ignore
            };
            column.binder = (SearchColumnEventArgs args, VisualElement ve) =>
            {
                if (args.value is Color c)
                {
                    var color = (ColorField)ve;
                    color.value = c;
                }
                else if (args.value is MaterialProperty mp)
                {
                    var color = (ColorField)ve;
                    color.value = mp.colorValue;
                }
            };
        }

        [SearchColumnProvider("Texture2D")]
        public static void InitializeTextureColumn(SearchColumn column)
        {
            column.cellCreator = (col) => new Image()
            {
                style =
                {
                    backgroundPositionX = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit),
                    backgroundPositionY = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit),
                    backgroundRepeat = BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleToFit),
                    backgroundSize = BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleToFit)
                }
            };
            column.binder = (SearchColumnEventArgs args, VisualElement ve) =>
            {
                var img = (Image)ve;
                img.image = args.value as Texture;
            };
        }

        [SearchColumnProvider("ObjectReference")]
        public static void InitializeObjectReferenceColumn(SearchColumn column)
        {
            column.cellCreator = (col) => new UIElements.ObjectField() { objectType = col.GetMatchingType() };
            column.binder = (SearchColumnEventArgs args, VisualElement ve) =>
            {
                var field = (UIElements.ObjectField)ve;
                field.pickingMode = PickingMode.Ignore;
                field.objectType = args.value?.GetType() ?? typeof(UnityEngine.Object);
                field.value = args.value as UnityEngine.Object;
                field.Query<VisualElement>(className: "unity-object-field__selector").ForEach(e => e.style.display = DisplayStyle.None);
            };
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
            [SearchColumnProvider("SerializedProperty")]
            public static void InitializeSerializedPropertyColumn(SearchColumn column)
            {
                column.getter = args => Getter(args.item, args.column);
                column.setter = args => Setter(args.item, args.column, args.value, args.multiple);
                column.comparer = args => Comparer(args.lhs.value, args.rhs.value, args.sortAscending);

                column.cellCreator = args => MakePropertyField(args);
                column.binder = (args, ve) => BindPropertyField(args, ve);
            }

            private static VisualElement MakePropertyField(SearchColumn args)
            {
                return new PropertyField() { label = string.Empty };
            }

            private static void BindPropertyField(SearchColumnEventArgs args, VisualElement ve)
            {
                if (ve is not PropertyField f)
                    throw new SearchColumnBindException(args.column, "Rebuild table");

                f.Unbind();
                if (args.value is SerializedProperty p)
                {
                    f.visible = true;
                    if (p.propertyType == SerializedPropertyType.Integer || p.propertyType == SerializedPropertyType.Float)
                        f.label = "\u2022";
                    else
                        f.label = string.Empty;
                    f.BindProperty(p);
                }
                else
                {
                    f.visible = false;
                    f.Unbind();
                }
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

                internal static int CompareStringHandler(SerializedProperty lhs, SerializedProperty rhs)
                {
                    return string.CompareOrdinal(lhs.stringValue, rhs.stringValue);
                }
            }

            public static object Getter(SearchItem item, SearchColumn column)
            {
                var p = GetSerializedProperty(item, column, out var _);

                if (column.drawer != null || column.binder != null)
                {
                    if (p != null)
                        p.isExpanded = true;
                    return p;
                }

                if (p == null)
                    return null;

                return GetSerializedPropertyValue(p);
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

            public static int Comparer(object lhsObj, object rhsObj, bool sortAscending)
            {
                if (!(lhsObj is SerializedProperty lhsProp) ||
                    !(rhsObj is SerializedProperty rhsProp) ||
                    lhsProp.propertyType != rhsProp.propertyType)
                    return 0;

                var sortOrder = sortAscending ? 1 : -1;

                switch (lhsProp.propertyType)
                {
                    case SerializedPropertyType.String: return DefaultDelegates.CompareStringHandler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.Enum: return DefaultDelegates.CompareEnumHandler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.Float: return DefaultDelegates.CompareFloatHandler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.Integer: return DefaultDelegates.CompareIntHandler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.Color: return DefaultDelegates.CompareColorHandler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.Boolean: return DefaultDelegates.CompareCheckboxHandler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.Vector2: return DefaultDelegates.CompareVector2Handler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.Vector2Int: return DefaultDelegates.CompareVector2IntHandler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.Vector3: return DefaultDelegates.CompareVector3Handler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.Vector3Int: return DefaultDelegates.CompareVector3IntHandler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.Vector4: return DefaultDelegates.CompareVector4Handler(lhsProp, rhsProp) * sortOrder;
                    case SerializedPropertyType.ObjectReference: return DefaultDelegates.CompareReferencesHandler(lhsProp, rhsProp) * sortOrder;
                }

                return lhsProp.GetHashCode().CompareTo(rhsProp.GetHashCode()) * sortOrder;
            }
        }
    }
}
