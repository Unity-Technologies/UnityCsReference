// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class MaterialSelectors
    {
        [SearchSelector(@"#(?<propertyPath>.+)", provider: "asset", priority: 9998)]
        public static object GetMaterialPropertyValue(SearchSelectorArgs args)
        {
            if (!(args["propertyPath"] is string propertyPath))
                return null;

            var item = args.current;
            var material = item.ToObject<Material>();
            if (!material)
                return null;

            var matProp = MaterialEditor.GetMaterialProperty(new Object[] { material }, propertyPath);
            if (matProp == null || matProp.name == null)
            {
                var materialProperties = MaterialEditor.GetMaterialProperties(new Object[] { material });
                for (var i = 0; i < materialProperties.Length; i++)
                {
                    if (!materialProperties[i].name.EndsWith(propertyPath, System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    matProp = materialProperties[i];
                    break;
                }
            }

            return GetMaterialPropertyValue(matProp);
        }

        internal static object GetMaterialPropertyValue(MaterialProperty p)
        {
            if (p == null || p.name == null)
                return null;

            switch (p.type)
            {
                case MaterialProperty.PropType.Color: return p.colorValue;
                case MaterialProperty.PropType.Vector: return p.vectorValue;
                case MaterialProperty.PropType.Float: return p.floatValue;
                case MaterialProperty.PropType.Range: return p.floatValue;
                case MaterialProperty.PropType.Texture: return p.textureValue;
            }
            return null;
        }

        public static IEnumerable<SearchColumn> Enumerate(IEnumerable<SearchItem> items)
        {
            var descriptors = new List<SearchColumn>();
            var shaderProcessed = new HashSet<string>();

            bool materialRootItemAdded = false;

            foreach (var item in items)
            {
                var material = item.ToObject<Material>();
                if (!material)
                    continue;

                if (!materialRootItemAdded)
                {
                    descriptors.Add(new SearchColumn("Material", new GUIContent("Material", Utils.FindTextureForType(typeof(Material)))));
                    materialRootItemAdded = true;
                }

                if (shaderProcessed.Contains(material.shader.name))
                    continue;
                shaderProcessed.Add(material.shader.name);

                var shaderPath = "Material/" + material.shader.name;
                var shaderIcon = Utils.FindTextureForType(typeof(Shader));
                descriptors.Add(new SearchColumn(shaderPath, new GUIContent(material.shader.name, shaderIcon)));

                var materialProperties = MaterialEditor.GetMaterialProperties(new Object[] { material });
                for (var i = 0; i < materialProperties.Length; i++)
                {
                    var m = materialProperties[i];
                    var propName = m.name;
                    var propPath = shaderPath + "/" + propName;
                    var col = new SearchColumn(propPath, "#" + propName, provider: $"{m.type}",
                        new GUIContent(m.displayName, shaderIcon, m.name));
                    descriptors.Add(col);
                }
            }

            return descriptors;
        }
    }

    static class MaterialPropertyColumnProvider
    {
        class SearchMaterialPropertyCell : VisualElement, IBindable, ISearchTableViewCellValue
        {
            private readonly SearchColumn m_SearchColumn;
            private VisualElement m_ValueElement;
            private MaterialProperty.PropType? m_CurrentBindedType;

            IBinding IBindable.binding { get => (m_ValueElement as IBindable)?.binding; set { if (m_ValueElement is IBindable b) b.binding = value; } }
            string IBindable.bindingPath { get => (m_ValueElement as IBindable)?.bindingPath; set { if (m_ValueElement is IBindable b) b.bindingPath = value; } }

            public SearchMaterialPropertyCell(SearchColumn column)
            {
                m_SearchColumn = column;
            }

            private void Create(MaterialProperty property, SearchColumnEventArgs args)
            {
                Clear();

                m_ValueElement = null;
                switch (property.type)
                {
                    case MaterialProperty.PropType.Color: m_ValueElement = new UIElements.ColorField(); break;
                    case MaterialProperty.PropType.Float:
                        m_ValueElement = new FloatField() { label = "\u2022", style = { flexDirection = FlexDirection.Row } };
                        break;
                    case MaterialProperty.PropType.Range:
                        m_ValueElement = new Slider(0f, 1f);
                        m_ValueElement.Add(new Label());
                        m_ValueElement.RegisterCallback<ChangeEvent<float>>(evt =>
                        {
                            m_ValueElement.Q<Label>().text = m_ValueElement.tooltip = evt.newValue.ToString();
                        });
                        break;
                    case MaterialProperty.PropType.Vector: m_ValueElement = new Vector4Field(); break;
                    case MaterialProperty.PropType.Texture:
                        m_ValueElement = new ObjectField() { objectType = typeof(Texture) };
                        break;
                }

                visible = true;
                m_CurrentBindedType = property.type;
                if (m_ValueElement != null)
                {
                    Add(m_ValueElement);
                    m_ValueElement.style.flexGrow = 1f;
                    m_ValueElement.style.flexDirection = FlexDirection.Row;
                }
            }

            public void Bind(SearchColumnEventArgs args)
            {
                if (args.value is not MaterialProperty matProp)
                {
                    visible = false;
                    return;
                }

                if (!m_CurrentBindedType.HasValue || m_CurrentBindedType != matProp.type)
                    Create(matProp, args);

                switch (matProp.type)
                {
                    case MaterialProperty.PropType.Color:
                        {
                            if (m_ValueElement is INotifyValueChanged<Color> v)
                                v.SetValueWithoutNotify(matProp.colorValue);
                        }
                        break;
                    case MaterialProperty.PropType.Vector:
                        {
                            if (m_ValueElement is INotifyValueChanged<Vector4> v)
                                v.SetValueWithoutNotify(matProp.vectorValue);
                        }
                        break;
                    case MaterialProperty.PropType.Float:
                        {
                            if (m_ValueElement is INotifyValueChanged<float> v)
                                v.SetValueWithoutNotify(matProp.floatValue);
                        }
                        break;
                    case MaterialProperty.PropType.Range:
                        {
                            if (m_ValueElement is Slider s)
                            { 
                                s.SetValueWithoutNotify(matProp.floatValue);
                                s.lowValue = matProp.rangeLimits.x;
                                s.SetHighValueWithoutNotify(matProp.rangeLimits.y);
                                s.Q<Label>().text = s.tooltip = matProp.floatValue.ToString();
                            }
                        }
                        break;
                    case MaterialProperty.PropType.Texture:
                        {
                            if (m_ValueElement is INotifyValueChanged<Object> v)
                                v.SetValueWithoutNotify(matProp.textureValue);
                        }
                        break;
                }
            }

            void ISearchTableViewCellValue.Update(object newValue)
            {

            }
        }

        [SearchColumnProvider("MaterialProperty")]
        public static void InitializeMaterialPropertyColumn(SearchColumn column)
        {
            column.getter = args => MaterialPropertyGetter(args.item, args.column);
            column.setter = args => SetMaterialPropertyValue(args.item, args.column, args.value);
            column.comparer = args => MaterialPropertyComparer(args.lhs.value, args.rhs.value, args.sortAscending);
            column.cellCreator = args => MaterialMakePropertyField(args);
            column.binder = (args, ve) => MaterialBindPropertyField(args, ve);
        }

        private static VisualElement MaterialMakePropertyField(SearchColumn column)
        {
            return new SearchMaterialPropertyCell(column);
        }

        private static void MaterialBindPropertyField(SearchColumnEventArgs args, VisualElement ve)
        {
            if (ve is SearchMaterialPropertyCell smpc)
                smpc.Bind(args);
        }

        internal static MaterialProperty GetMaterialProperty(SearchItem item, SearchColumn column)
        {
            var mat = item.ToObject<Material>();
            if (!mat)
                return null;

            foreach (var m in SelectorManager.Match(column.selector, item.provider?.type))
            {
                var selectorArgs = new SearchSelectorArgs(m, item);
                if (selectorArgs.name == null)
                    continue;

                if (!mat.HasProperty(selectorArgs.name))
                    continue;

                return MaterialEditor.GetMaterialProperty(new Object[] { mat }, selectorArgs.name);
            }

            return null;
        }

        static object MaterialPropertyGetter(SearchItem item, SearchColumn column)
        {
            var matProp = GetMaterialProperty(item, column);
            if (matProp == null)
                return null;

            return matProp;
        }

        internal static void SetMaterialPropertyValue(SearchItem item, SearchColumn column, object newValue)
        {
            var matProp = GetMaterialProperty(item, column);
            if (matProp == null)
                return;

            switch (matProp.type)
            {
                case MaterialProperty.PropType.Color:
                    if (newValue is Color c && matProp.colorValue != c)
                        matProp.colorValue = c;
                    break;

                case MaterialProperty.PropType.Vector:
                    if (newValue is Vector4 v && matProp.vectorValue != v)
                        matProp.vectorValue = v;
                    break;

                case MaterialProperty.PropType.Float:
                    {
                        if (newValue is float f && matProp.floatValue != f)
                            matProp.floatValue = f;
                    }
                    break;

                case MaterialProperty.PropType.Range:
                    {
                        if (newValue is float f && matProp.floatValue != f)
                            matProp.floatValue = f;
                    }
                    break;

                case MaterialProperty.PropType.Texture:
                    if (matProp.textureValue != (Texture)newValue)
                        matProp.textureValue = (Texture)newValue;
                    break;
            }
        }

        static object MaterialPropertyDrawer(Rect r, object prop)
        {
            if (!(prop is MaterialProperty matProp))
                return null;

            switch (matProp.type)
            {
                case MaterialProperty.PropType.Color:
                    return MaterialEditor.ColorPropertyInternal(r, matProp, GUIContent.none);
                case MaterialProperty.PropType.Float:
                    return MaterialEditor.FloatPropertyInternal(r, matProp, GUIContent.none);
                case MaterialProperty.PropType.Range:
                    return MaterialEditor.RangePropertyInternal(r, matProp, GUIContent.none);
                case MaterialProperty.PropType.Vector:
                    return MaterialEditor.VectorPropertyInternal(r, matProp, GUIContent.none);
                case MaterialProperty.PropType.Texture:
                    return EditorGUI.DoObjectField(r, r, GUIUtility.GetControlID(FocusType.Passive), matProp.textureValue, matProp.targets[0], typeof(Texture), null, true);
            }

            return null;
        }

        static int MaterialPropertyComparer(object lhsObj, object rhsObj, bool sortAscending)
        {
            if (!(lhsObj is MaterialProperty lm) || !(rhsObj is MaterialProperty rm) || lm.type != rm.type)
                return 0;

            switch (lm.type)
            {
                case MaterialProperty.PropType.Color:
                    Color.RGBToHSV(lm.colorValue, out float lh, out _, out _);
                    Color.RGBToHSV(rm.colorValue, out float rh, out _, out _);
                    return lh.CompareTo(rh);

                case MaterialProperty.PropType.Range:
                case MaterialProperty.PropType.Float:
                    return lm.floatValue.CompareTo(rm.floatValue);

                case MaterialProperty.PropType.Texture:
                    return string.CompareOrdinal(lm.textureValue?.name, rm.textureValue?.name);

                case MaterialProperty.PropType.Vector:
                    return lm.vectorValue.magnitude.CompareTo(rm.vectorValue.magnitude);
            }

            return 0;
        }
    }
}
