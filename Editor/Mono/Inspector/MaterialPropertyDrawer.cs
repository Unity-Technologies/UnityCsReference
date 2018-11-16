// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor
{
    internal class MaterialPropertyHandler
    {
        private MaterialPropertyDrawer m_PropertyDrawer;
        private List<MaterialPropertyDrawer> m_DecoratorDrawers;

        public MaterialPropertyDrawer propertyDrawer { get { return m_PropertyDrawer; } }

        public bool IsEmpty()
        {
            return m_PropertyDrawer == null && (m_DecoratorDrawers == null || m_DecoratorDrawers.Count == 0);
        }

        public void OnGUI(ref Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            float oldLabelWidth, oldFieldWidth;
            var propHeight = position.height;
            position.height = 0;
            if (m_DecoratorDrawers != null)
            {
                foreach (var decorator in m_DecoratorDrawers)
                {
                    position.height = decorator.GetPropertyHeight(prop, label.text, editor);

                    oldLabelWidth = EditorGUIUtility.labelWidth;
                    oldFieldWidth = EditorGUIUtility.fieldWidth;
                    decorator.OnGUI(position, prop, label, editor);
                    EditorGUIUtility.labelWidth = oldLabelWidth;
                    EditorGUIUtility.fieldWidth = oldFieldWidth;

                    position.y += position.height;
                    propHeight -= position.height;
                }
            }

            position.height = propHeight;
            if (m_PropertyDrawer != null)
            {
                oldLabelWidth = EditorGUIUtility.labelWidth;
                oldFieldWidth = EditorGUIUtility.fieldWidth;
                m_PropertyDrawer.OnGUI(position, prop, label, editor);
                EditorGUIUtility.labelWidth = oldLabelWidth;
                EditorGUIUtility.fieldWidth = oldFieldWidth;
            }
        }

        public float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            var height = 0f;
            if (m_DecoratorDrawers != null)
                foreach (var drawer in m_DecoratorDrawers)
                    height += drawer.GetPropertyHeight(prop, label, editor);
            if (m_PropertyDrawer != null)
                height += m_PropertyDrawer.GetPropertyHeight(prop, label, editor);
            return height;
        }

        private static Dictionary<string, MaterialPropertyHandler> s_PropertyHandlers = new Dictionary<string, MaterialPropertyHandler>();

        private static string GetPropertyString(Shader shader, string name)
        {
            if (shader == null)
                return string.Empty;
            return shader.GetInstanceID() + "_" + name;
        }

        internal static void InvalidatePropertyCache(Shader shader)
        {
            if (shader == null)
                return;
            string keyStart = shader.GetInstanceID() + "_";
            var toDelete = new List<string>();
            foreach (string key in s_PropertyHandlers.Keys)
            {
                if (key.StartsWith(keyStart))
                    toDelete.Add(key);
            }
            foreach (string key in toDelete)
            {
                s_PropertyHandlers.Remove(key);
            }
        }

        private static MaterialPropertyDrawer CreatePropertyDrawer(Type klass, string argsText)
        {
            // no args -> default constructor
            if (string.IsNullOrEmpty(argsText))
                return Activator.CreateInstance(klass) as MaterialPropertyDrawer;

            // split the argument list by commas
            string[] argStrings = argsText.Split(',');
            var args = new object[argStrings.Length];
            for (var i = 0; i < argStrings.Length; ++i)
            {
                float f;
                string arg = argStrings[i].Trim();

                // if can parse as a float, use the float; otherwise pass the string
                if (float.TryParse(arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out f))
                {
                    args[i] = f;
                }
                else
                {
                    args[i] = arg;
                }
            }
            return Activator.CreateInstance(klass, args) as MaterialPropertyDrawer;
        }

        private static MaterialPropertyDrawer GetShaderPropertyDrawer(string attrib, out bool isDecorator)
        {
            isDecorator = false;

            string className = attrib;
            string args = string.Empty;
            Match match = Regex.Match(attrib, @"(\w+)\s*\((.*)\)");
            if (match.Success)
            {
                className = match.Groups[1].Value;
                args = match.Groups[2].Value.Trim();
            }

            //Debug.Log ("looking for class " + className + " args '" + args + "'");
            foreach (var klass in EditorAssemblies.SubclassesOf(typeof(MaterialPropertyDrawer)))
            {
                // When you write [Foo] in shader, get Foo, FooDrawer, MaterialFooDrawer,
                // FooDecorator or MaterialFooDecorator class;
                // "kind of" similar to how C# does attributes.

                //@TODO: namespaces?
                if (klass.Name == className ||
                    klass.Name == className + "Drawer" ||
                    klass.Name == "Material" + className + "Drawer" ||
                    klass.Name == className + "Decorator" ||
                    klass.Name == "Material" + className + "Decorator")
                {
                    try
                    {
                        isDecorator = klass.Name.EndsWith("Decorator");
                        return CreatePropertyDrawer(klass, args);
                    }
                    catch (Exception)
                    {
                        Debug.LogWarningFormat("Failed to create material drawer {0} with arguments '{1}'", className, args);
                        return null;
                    }
                }
            }

            return null;
        }

        private static MaterialPropertyHandler GetShaderPropertyHandler(Shader shader, string name)
        {
            string[] attribs = ShaderUtil.GetShaderPropertyAttributes(shader, name);
            if (attribs == null || attribs.Length == 0)
                return null;

            var handler = new MaterialPropertyHandler();
            foreach (var attr in attribs)
            {
                bool isDecorator;
                MaterialPropertyDrawer drawer = GetShaderPropertyDrawer(attr, out isDecorator);
                if (drawer != null)
                {
                    if (isDecorator)
                    {
                        if (handler.m_DecoratorDrawers == null)
                            handler.m_DecoratorDrawers = new List<MaterialPropertyDrawer>();
                        handler.m_DecoratorDrawers.Add(drawer);
                    }
                    else
                    {
                        if (handler.m_PropertyDrawer != null)
                        {
                            Debug.LogWarning(string.Format("Shader property {0} already has a property drawer", name), shader);
                        }
                        handler.m_PropertyDrawer = drawer;
                    }
                }
            }

            return handler;
        }

        internal static MaterialPropertyHandler GetHandler(Shader shader, string name)
        {
            if (shader == null)
                return null;

            // Use cached handler if available
            MaterialPropertyHandler handler;
            string key = GetPropertyString(shader, name);
            if (s_PropertyHandlers.TryGetValue(key, out handler))
                return handler;

            // Get the handler for this shader property
            handler = GetShaderPropertyHandler(shader, name);
            if (handler != null && handler.IsEmpty())
                handler = null;
            //Debug.Log ("drawer " + drawer);

            // Cache the handler and return. Cache even if it was null, so we can return
            // later requests fast as well.
            s_PropertyHandlers[key] = handler;
            return handler;
        }
    }


    public abstract class MaterialPropertyDrawer
    {
        public virtual void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            OnGUI(position, prop, label.text, editor);
        }

        public virtual void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            EditorGUI.LabelField(position, new GUIContent(label), EditorGUIUtility.TempContent("No GUI Implemented"));
        }

        public virtual float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return EditorGUI.kSingleLineHeight;
        }

        public virtual void Apply(MaterialProperty prop)
        {
            // empty base implementation
        }
    }


    // --------------------------------------------------------------------------
    // Built-in drawers below.
    // They aren't directly used by the code, but can be used by writing attribute-like
    // syntax in shaders, e.g. [Toggle] in front of a shader property will
    // end up using MaterialToggleDrawer to display it as a toggle.


    internal class MaterialToggleDrawer : MaterialPropertyDrawer
    {
        protected readonly string keyword;
        public MaterialToggleDrawer()
        {
        }

        public MaterialToggleDrawer(string keyword)
        {
            this.keyword = keyword;
        }

        static bool IsPropertyTypeSuitable(MaterialProperty prop)
        {
            return prop.type == MaterialProperty.PropType.Float || prop.type == MaterialProperty.PropType.Range;
        }

        protected virtual void SetKeyword(MaterialProperty prop, bool on)
        {
            SetKeywordInternal(prop, on, "_ON");
        }

        protected void SetKeywordInternal(MaterialProperty prop, bool on, string defaultKeywordSuffix)
        {
            // if no keyword is provided, use <uppercase property name> + defaultKeywordSuffix
            string kw = string.IsNullOrEmpty(keyword) ? prop.name.ToUpperInvariant() + defaultKeywordSuffix : keyword;
            // set or clear the keyword
            foreach (Material material in prop.targets)
            {
                if (on)
                    material.EnableKeyword(kw);
                else
                    material.DisableKeyword(kw);
            }
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (!IsPropertyTypeSuitable(prop))
            {
                return EditorGUI.kSingleLineHeight * 2.5f;
            }
            return base.GetPropertyHeight(prop, label, editor);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (!IsPropertyTypeSuitable(prop))
            {
                GUIContent c = EditorGUIUtility.TempContent("Toggle used on a non-float property: " + prop.name,
                        EditorGUIUtility.GetHelpIcon(MessageType.Warning));
                EditorGUI.LabelField(position, c, EditorStyles.helpBox);
                return;
            }

            EditorGUI.BeginChangeCheck();

            bool value = (Math.Abs(prop.floatValue) > 0.001f);
            EditorGUI.showMixedValue = prop.hasMixedValue;
            value = EditorGUI.Toggle(position, label, value);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = value ? 1.0f : 0.0f;
                SetKeyword(prop, value);
            }
        }

        public override void Apply(MaterialProperty prop)
        {
            base.Apply(prop);
            if (!IsPropertyTypeSuitable(prop))
                return;

            if (prop.hasMixedValue)
                return;

            SetKeyword(prop, (Math.Abs(prop.floatValue) > 0.001f));
        }
    }

    // Variant of the ToggleDrawer that defines a keyword when it's not on
    // This is useful when adding Toggles to existing shaders while maintaining backwards compatibility
    internal class MaterialToggleOffDrawer : MaterialToggleDrawer
    {
        public MaterialToggleOffDrawer()
        {
        }

        public MaterialToggleOffDrawer(string keyword) : base(keyword)
        {
        }

        protected override void SetKeyword(MaterialProperty prop, bool on)
        {
            SetKeywordInternal(prop, !on, "_OFF");
        }
    }

    internal class MaterialPowerSliderDrawer : MaterialPropertyDrawer
    {
        private readonly float power;

        public MaterialPowerSliderDrawer(float power)
        {
            this.power = power;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Range)
            {
                return EditorGUI.kSingleLineHeight * 2.5f;
            }
            return base.GetPropertyHeight(prop, label, editor);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Range)
            {
                GUIContent c = EditorGUIUtility.TempContent("PowerSlider used on a non-range property: " + prop.name,
                        EditorGUIUtility.GetHelpIcon(MessageType.Warning));
                EditorGUI.LabelField(position, c, EditorStyles.helpBox);
                return;
            }

            MaterialEditor.DoPowerRangeProperty(position, prop, label, power);
        }
    }

    internal class MaterialIntRangeDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Range)
            {
                GUIContent c = EditorGUIUtility.TempContent("IntRange used on a non-range property: " + prop.name,
                        EditorGUIUtility.GetHelpIcon(MessageType.Warning));
                EditorGUI.LabelField(position, c, EditorStyles.helpBox);
                return;
            }

            MaterialEditor.DoIntRangeProperty(position, prop, label);
        }
    }

    internal class MaterialKeywordEnumDrawer : MaterialPropertyDrawer
    {
        private readonly GUIContent[] keywords;

        public MaterialKeywordEnumDrawer(string kw1) : this(new[] { kw1 }) {}
        public MaterialKeywordEnumDrawer(string kw1, string kw2) : this(new[] { kw1, kw2 }) {}
        public MaterialKeywordEnumDrawer(string kw1, string kw2, string kw3) : this(new[] { kw1, kw2, kw3 }) {}
        public MaterialKeywordEnumDrawer(string kw1, string kw2, string kw3, string kw4) : this(new[] { kw1, kw2, kw3, kw4 }) {}
        public MaterialKeywordEnumDrawer(string kw1, string kw2, string kw3, string kw4, string kw5) : this(new[] { kw1, kw2, kw3, kw4, kw5 }) {}
        public MaterialKeywordEnumDrawer(string kw1, string kw2, string kw3, string kw4, string kw5, string kw6) : this(new[] { kw1, kw2, kw3, kw4, kw5, kw6 }) {}
        public MaterialKeywordEnumDrawer(string kw1, string kw2, string kw3, string kw4, string kw5, string kw6, string kw7) : this(new[] { kw1, kw2, kw3, kw4, kw5, kw6, kw7 }) {}
        public MaterialKeywordEnumDrawer(string kw1, string kw2, string kw3, string kw4, string kw5, string kw6, string kw7, string kw8) : this(new[] { kw1, kw2, kw3, kw4, kw5, kw6, kw7, kw8 }) {}
        public MaterialKeywordEnumDrawer(string kw1, string kw2, string kw3, string kw4, string kw5, string kw6, string kw7, string kw8, string kw9) : this(new[] { kw1, kw2, kw3, kw4, kw5, kw6, kw7, kw8, kw9 }) {}
        public MaterialKeywordEnumDrawer(params string[] keywords)
        {
            this.keywords = new GUIContent[keywords.Length];
            for (int i = 0; i < keywords.Length; ++i)
                this.keywords[i] = new GUIContent(keywords[i]);
        }

        static bool IsPropertyTypeSuitable(MaterialProperty prop)
        {
            return prop.type == MaterialProperty.PropType.Float || prop.type == MaterialProperty.PropType.Range;
        }

        void SetKeyword(MaterialProperty prop, int index)
        {
            for (int i = 0; i < keywords.Length; ++i)
            {
                string keyword = GetKeywordName(prop.name, keywords[i].text);
                foreach (Material material in prop.targets)
                {
                    if (index == i)
                        material.EnableKeyword(keyword);
                    else
                        material.DisableKeyword(keyword);
                }
            }
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (!IsPropertyTypeSuitable(prop))
            {
                return EditorGUI.kSingleLineHeight * 2.5f;
            }
            return base.GetPropertyHeight(prop, label, editor);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (!IsPropertyTypeSuitable(prop))
            {
                GUIContent c = EditorGUIUtility.TempContent("KeywordEnum used on a non-float property: " + prop.name,
                        EditorGUIUtility.GetHelpIcon(MessageType.Warning));
                EditorGUI.LabelField(position, c, EditorStyles.helpBox);
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = prop.hasMixedValue;
            var value = (int)prop.floatValue;
            value = EditorGUI.Popup(position, label, value, keywords);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = value;
                SetKeyword(prop, value);
            }
        }

        public override void Apply(MaterialProperty prop)
        {
            base.Apply(prop);
            if (!IsPropertyTypeSuitable(prop))
                return;

            if (prop.hasMixedValue)
                return;

            SetKeyword(prop, (int)prop.floatValue);
        }

        // Final keyword name: property name + "_" + display name. Uppercased,
        // and spaces replaced with underscores.
        private static string GetKeywordName(string propName, string name)
        {
            string n = propName + "_" + name;
            return n.Replace(' ', '_').ToUpperInvariant();
        }
    }


    internal class MaterialEnumDrawer : MaterialPropertyDrawer
    {
        private readonly GUIContent[] names;
        private readonly float[] values;

        // Single argument: enum type name; entry names & values fetched via reflection
        public MaterialEnumDrawer(string enumName)
        {
            var loadedTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => AssemblyHelper.GetTypesFromAssembly(x)).ToArray();
            try
            {
                var enumType = loadedTypes.FirstOrDefault(
                        x => x.IsSubclassOf(typeof(Enum)) && (x.Name == enumName || x.FullName == enumName)
                        );
                var enumNames = Enum.GetNames(enumType);
                this.names = new GUIContent[enumNames.Length];
                for (int i = 0; i < enumNames.Length; ++i)
                    this.names[i] = new GUIContent(enumNames[i]);

                var enumVals = Enum.GetValues(enumType);
                values = new float[enumVals.Length];
                for (var i = 0; i < enumVals.Length; ++i)
                    values[i] = (int)enumVals.GetValue(i);
            }
            catch (Exception)
            {
                Debug.LogWarningFormat("Failed to create MaterialEnum, enum {0} not found", enumName);
                throw;
            }
        }

        // name,value,name,value,... pairs: explicit names & values
        public MaterialEnumDrawer(string n1, float v1) : this(new[] {n1}, new[] {v1}) {}
        public MaterialEnumDrawer(string n1, float v1, string n2, float v2) : this(new[] { n1, n2 }, new[] { v1, v2 }) {}
        public MaterialEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3) : this(new[] { n1, n2, n3 }, new[] { v1, v2, v3 }) {}
        public MaterialEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4) : this(new[] { n1, n2, n3, n4 }, new[] { v1, v2, v3, v4 }) {}
        public MaterialEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5) : this(new[] { n1, n2, n3, n4, n5 }, new[] { v1, v2, v3, v4, v5 }) {}
        public MaterialEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6) : this(new[] { n1, n2, n3, n4, n5, n6 }, new[] { v1, v2, v3, v4, v5, v6 }) {}
        public MaterialEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7) : this(new[] { n1, n2, n3, n4, n5, n6, n7 }, new[] { v1, v2, v3, v4, v5, v6, v7 }) {}
        public MaterialEnumDrawer(string[] enumNames, float[] vals)
        {
            this.names = new GUIContent[enumNames.Length];
            for (int i = 0; i < enumNames.Length; ++i)
                this.names[i] = new GUIContent(enumNames[i]);

            values = new float[vals.Length];
            for (int i = 0; i < vals.Length; ++i)
                values[i] = vals[i];
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Float && prop.type != MaterialProperty.PropType.Range)
            {
                return EditorGUI.kSingleLineHeight * 2.5f;
            }
            return base.GetPropertyHeight(prop, label, editor);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Float && prop.type != MaterialProperty.PropType.Range)
            {
                GUIContent c = EditorGUIUtility.TempContent("Enum used on a non-float property: " + prop.name,
                        EditorGUIUtility.GetHelpIcon(MessageType.Warning));
                EditorGUI.LabelField(position, c, EditorStyles.helpBox);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            var value = prop.floatValue;
            int selectedIndex = -1;
            for (var index = 0; index < values.Length; index++)
            {
                var i = values[index];
                if (i == value)
                {
                    selectedIndex = index;
                    break;
                }
            }

            var selIndex = EditorGUI.Popup(position, label, selectedIndex, names);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = values[selIndex];
            }
        }
    }

    // [Space] or [Space(height)] decorator creates a vertical space before shader property.
    internal class MaterialSpaceDecorator : MaterialPropertyDrawer
    {
        private readonly float height;

        public MaterialSpaceDecorator()
        {
            height = 6f;
        }

        public MaterialSpaceDecorator(float height)
        {
            this.height = height;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return height;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
        }
    }

    // [Header(foobar)] decorator shows "foobar" header before shader property.
    internal class MaterialHeaderDecorator : MaterialPropertyDrawer
    {
        private readonly string header;

        public MaterialHeaderDecorator(string header)
        {
            this.header = header;
        }

        // so that we can accept Header(1) and display that as text
        public MaterialHeaderDecorator(float headerAsNumber)
        {
            this.header = headerAsNumber.ToString(CultureInfo.InvariantCulture);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return 24f;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            position.y += 8;
            position = EditorGUI.IndentedRect(position);
            GUI.Label(position, header, EditorStyles.boldLabel);
        }
    }
}
