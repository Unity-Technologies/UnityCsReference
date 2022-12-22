// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using JetBrains.Annotations;
using Unity.Properties;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    interface IBuilderAttributesTestObject
    {
        object objAttr { get; set; }
        string stringAttr { get; set; }
        double doubleAttr  { get; set; }
        float floatAttr { get; set; }
        int intAttr { get; set; }
        long longAttr { get; set; }
        bool boolAttr { get; set; }
        Color colorAttr { get; set; }
        BuilderAttributesTestElement.Existance enumAttr { get; set; }
        Texture2D assetAttr { get; set; }
        event PropertyChangedEventHandler propertyChanged;
    }

    class BuilderAttributesTestFieldsView : VisualElement
    {
        private Dictionary<string, VisualElement> m_Controls = new();
        private IBuilderAttributesTestObject m_Source;

        public BuilderAttributesTestFieldsView(IBuilderAttributesTestObject source)
        {
            m_Source = source;
            source.propertyChanged += OnPropertyChanged;
            AddControls(this);
        }

        void UpdateControlValue(string propertyName)
        {
            var control = m_Controls[propertyName];
            var controlType = control.GetType();
            var sourceType = m_Source.GetType();
            var sourcePropInfo = sourceType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            var value = sourcePropInfo.GetValue(m_Source);

            if (control is BuilderObjectField objField)
            {
                UpdateControlObjectValue(objField, value);
            }
            else
            {
                var valuePropInfo = controlType.GetProperty("value", BindingFlags.Instance | BindingFlags.Public);
                valuePropInfo.SetValue(control, value);
            }
        }

        void UpdateControlObjectValue(BuilderObjectField objField, object value)
        {
            if (value != null && value is not Object)
            {
                objField.SetNonUnityObject(value);
            }
            else
            {
                objField.SetValueWithoutNotify(value as Object);
            }
        }

        public TControl AddControl<TControl>(string propertyName,  VisualElement parent, Action<TControl> setup = null) where TControl : VisualElement, new()
        {
            var type = m_Source.GetType();
            var propInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            var control = new TControl();

            control.GetType().GetProperty("label", BindingFlags.Instance | BindingFlags.Public).SetValue(control, propertyName + " - " + propInfo.PropertyType.Name);
            setup?.Invoke(control);
            parent.Add(control);
            m_Controls[propertyName] = control;
            UpdateControlValue(propertyName);

            return control;
        }

        public void AddControls(VisualElement parent)
        {
            AddControl<BuilderObjectField>("objAttr", parent);
            AddControl<TextField>("stringAttr", parent);
            AddControl<FloatField>("floatAttr", parent);
            AddControl<DoubleField>("doubleAttr", parent);
            AddControl<IntegerField>("intAttr", parent);
            AddControl<LongField>("longAttr", parent);
            AddControl<Toggle>("boolAttr", parent);
            AddControl<ColorField>("colorAttr", parent);
            AddControl<EnumField>("enumAttr", parent, (enumField) => enumField.Init(BuilderAttributesTestElement.Existance.Bad));
            AddControl<BuilderObjectField>("assetAttr", parent, (objField) => objField.objectType = typeof(Texture2D));
        }

        void OnPropertyChanged(object obj, PropertyChangedEventArgs args)
        {
            UpdateControlValue(args.PropertyName);
        }
    }

    class BuilderAttributesTestObject : IBuilderAttributesTestObject
    {
        [UsedImplicitly]
        internal class UxmlObjectFactory : UxmlObjectFactory<BuilderAttributesTestObject, UxmlObjectTraits> {}

        internal class UxmlObjectTraits : UnityEngine.UIElements.UxmlObjectTraits<BuilderAttributesTestObject>
        {
            UxmlAssetAttributeDescription<Object> m_ObjAttr = new() { name = "obj-attr" };
            UxmlStringAttributeDescription m_String = new() { name = "string-attr", defaultValue = "default_value" };
            UxmlFloatAttributeDescription m_Float = new() { name = "float-attr", defaultValue = 0.1f };
            UxmlDoubleAttributeDescription m_Double = new() { name = "double-attr", defaultValue = 0.1 };
            UxmlIntAttributeDescription m_Int = new() { name = "int-attr", defaultValue = 2 };
            UxmlLongAttributeDescription m_Long = new() { name = "long-attr", defaultValue = 3 };
            UxmlBoolAttributeDescription m_Bool = new() { name = "bool-attr", defaultValue = false };
            UxmlColorAttributeDescription m_Color = new() { name = "color-attr", defaultValue = Color.red };
            UxmlEnumAttributeDescription<BuilderAttributesTestElement.Existance> m_Enum = new() { name = "enum-attr", defaultValue = BuilderAttributesTestElement.Existance.Bad };
            UxmlAssetAttributeDescription<Texture2D> m_Asset = new() { name = "asset-attr" };

            public override void Init(ref BuilderAttributesTestObject obj, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ref obj, bag, cc);

                obj.objAttr = m_ObjAttr.GetValueFromBag(bag, cc);
                obj.stringAttr = m_String.GetValueFromBag(bag, cc);
                obj.floatAttr = m_Float.GetValueFromBag(bag, cc);
                obj.doubleAttr = m_Double.GetValueFromBag(bag, cc);
                obj.intAttr = m_Int.GetValueFromBag(bag, cc);
                obj.longAttr = m_Long.GetValueFromBag(bag, cc);
                obj.boolAttr = m_Bool.GetValueFromBag(bag, cc);
                obj.colorAttr = m_Color.GetValueFromBag(bag, cc);
                obj.enumAttr = m_Enum.GetValueFromBag(bag, cc);
                obj.assetAttr = m_Asset.GetValueFromBag(bag, cc);
            }
        }

        private object m_ObjAttr;
        private string m_StringAttr;
        private float m_FloatAttr;
        private double m_DoubleAttr;
        private int m_IntAttr;
        private long m_LongAttr;
        private bool m_BoolAttr;
        private Color m_ColorAttr;
        private BuilderAttributesTestElement.Existance m_EnumAttr;
        private Texture2D m_AssetAttr;

        [CreateProperty]
        public object objAttr
        {
            get => m_ObjAttr;
            set
            {
                if (m_ObjAttr == value)
                    return;
                m_ObjAttr = value;
                NotifyPropertyChanged(nameof(objAttr));
            }
        }

        [CreateProperty]
        public string stringAttr
        {
            get => m_StringAttr;
            set
            {
                if (m_StringAttr == value)
                    return;
                m_StringAttr = value;
                NotifyPropertyChanged(nameof(stringAttr));
            }
        }

        [CreateProperty]
        public double doubleAttr
        {
            get => m_DoubleAttr;
            set
            {
                if (m_DoubleAttr == value)
                    return;
                m_DoubleAttr = value;
                NotifyPropertyChanged(nameof(doubleAttr));
            }
        }

        [CreateProperty]
        public float floatAttr
        {
            get => m_FloatAttr;
            set
            {
                if (m_FloatAttr == value)
                    return;
                m_FloatAttr = value;
                NotifyPropertyChanged(nameof(floatAttr));
            }
        }

        [CreateProperty]
        public int intAttr
        {
            get => m_IntAttr;
            set
            {
                if (m_IntAttr == value)
                    return;
                m_IntAttr = value;
                NotifyPropertyChanged(nameof(intAttr));
            }
        }

        [CreateProperty]
        public long longAttr
        {
            get => m_LongAttr;
            set
            {
                if (m_LongAttr == value)
                    return;
                m_LongAttr = value;
                NotifyPropertyChanged(nameof(longAttr));
            }
        }

        [CreateProperty]
        public bool boolAttr
        {
            get => m_BoolAttr;
            set
            {
                if (m_BoolAttr == value)
                    return;
                m_BoolAttr = value;
                NotifyPropertyChanged(nameof(boolAttr));
            }
        }

        [CreateProperty]
        public Color colorAttr
        {
            get => m_ColorAttr;
            set
            {
                if (m_ColorAttr == value)
                    return;
                m_ColorAttr = value;
                NotifyPropertyChanged(nameof(colorAttr));
            }
        }

        [CreateProperty]
        public BuilderAttributesTestElement.Existance enumAttr
        {
            get => m_EnumAttr;
            set
            {
                if (m_EnumAttr == value)
                    return;
                m_EnumAttr = value;
                NotifyPropertyChanged(nameof(enumAttr));
            }
        }

        [CreateProperty]
        public Texture2D assetAttr
        {
            get => m_AssetAttr;
            set
            {
                if (m_AssetAttr == value)
                    return;
                m_AssetAttr = value;
                NotifyPropertyChanged(nameof(assetAttr));
            }
        }

        public event PropertyChangedEventHandler propertyChanged;

        void NotifyPropertyChanged(string property)
        {
            propertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    internal class BuilderAttributesTestElement : Button, IBuilderAttributesTestObject
    {
        public enum Existance
        {
            None,
            Good,
            Bad
        }

        public new class UxmlFactory : UxmlFactory<BuilderAttributesTestElement, UxmlTraits> { }

        public new class UxmlTraits : Button.UxmlTraits
        {
            UxmlAssetAttributeDescription<Object> m_ObjAttr = new() { name = "obj-attr" };
            UxmlStringAttributeDescription m_String = new() { name = "string-attr", defaultValue = "default_value" };
            UxmlFloatAttributeDescription m_Float = new() { name = "float-attr", defaultValue = 0.1f };
            UxmlDoubleAttributeDescription m_Double = new() { name = "double-attr", defaultValue = 0.1 };
            UxmlIntAttributeDescription m_Int = new() { name = "int-attr", defaultValue = 2 };
            UxmlLongAttributeDescription m_Long = new() { name = "long-attr", defaultValue = 3 };
            UxmlBoolAttributeDescription m_Bool = new() { name = "bool-attr", defaultValue = false };
            UxmlColorAttributeDescription m_Color = new() { name = "color-attr", defaultValue = Color.red };
            UxmlEnumAttributeDescription<Existance> m_Enum = new() { name = "enum-attr", defaultValue = Existance.Bad };
            UxmlAssetAttributeDescription<Texture2D> m_Asset = new() { name = "asset-attr" };
            readonly UxmlObjectListAttributeDescription<BuilderAttributesTestObject> m_Objects = new();
            UxmlBoolAttributeDescription m_Unbindable = new() { name = "unbindable-attr" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ate = ve as BuilderAttributesTestElement;

                ate.Clear();

                ate.objAttr = m_ObjAttr.GetValueFromBag(bag, cc);
                ate.stringAttr = m_String.GetValueFromBag(bag, cc);
                ate.floatAttr = m_Float.GetValueFromBag(bag, cc);
                ate.doubleAttr = m_Double.GetValueFromBag(bag, cc);
                ate.intAttr = m_Int.GetValueFromBag(bag, cc);
                ate.longAttr = m_Long.GetValueFromBag(bag, cc);
                ate.boolAttr = m_Bool.GetValueFromBag(bag, cc);
                ate.colorAttr = m_Color.GetValueFromBag(bag, cc);
                ate.enumAttr = m_Enum.GetValueFromBag(bag, cc);
                ate.assetAttr = m_Asset.GetValueFromBag(bag, cc);
                ate.unbindableAttr = m_Unbindable.GetValueFromBag(bag, cc);

                ate.Add(new BuilderAttributesTestFieldsView(ate));

                ate.objectsAttr = m_Objects.GetValueFromBag(bag, cc);

                var foldout = new FoldoutField() { text = "Objects"};

                if (ate.objectsAttr != null)
                {
                    for (var i = 0; i < ate.objectsAttr.Count; i++)
                    {
                        var obj = ate.objectsAttr[i];
                        var itemFoldout = new FoldoutField() {text = "Object #" + i};

                        itemFoldout.Add(new BuilderAttributesTestFieldsView(obj));
                        foldout.Add(itemFoldout);
                    }
                }

                ate.Add(foldout);
            }
        }

        private object m_ObjAttr;
        private string m_StringAttr;
        private float m_FloatAttr;
        private double m_DoubleAttr;
        private int m_IntAttr;
        private long m_LongAttr;
        private bool m_BoolAttr;
        private Color m_ColorAttr;
        private Existance m_EnumAttr;
        private Texture2D m_AssetAttr;

        [CreateProperty]
        public object objAttr
        {
            get => m_ObjAttr;
            set
            {
                if (m_ObjAttr == value)
                    return;
                m_ObjAttr = value;
                NotifyPropertyChanged(nameof(objAttr));
            }
        }

        [CreateProperty]
        public string stringAttr
        {
            get => m_StringAttr;
            set
            {
                if (m_StringAttr == value)
                    return;
                m_StringAttr = value;
                NotifyPropertyChanged(nameof(stringAttr));
            }
        }

        [CreateProperty]
        public double doubleAttr
        {
            get => m_DoubleAttr;
            set
            {
                if (m_DoubleAttr == value)
                    return;
                m_DoubleAttr = value;
                NotifyPropertyChanged(nameof(doubleAttr));
            }
        }

        [CreateProperty]
        public float floatAttr
        {
            get => m_FloatAttr;
            set
            {
                if (m_FloatAttr == value)
                    return;
                m_FloatAttr = value;
                NotifyPropertyChanged(nameof(floatAttr));
            }
        }

        [CreateProperty]
        public int intAttr
        {
            get => m_IntAttr;
            set
            {
                if (m_IntAttr == value)
                    return;
                m_IntAttr = value;
                NotifyPropertyChanged(nameof(intAttr));
            }
        }

        [CreateProperty]
        public long longAttr
        {
            get => m_LongAttr;
            set
            {
                if (m_LongAttr == value)
                    return;
                m_LongAttr = value;
                NotifyPropertyChanged(nameof(longAttr));
            }
        }

        [CreateProperty]
        public bool boolAttr
        {
            get => m_BoolAttr;
            set
            {
                if (m_BoolAttr == value)
                    return;
                m_BoolAttr = value;
                NotifyPropertyChanged(nameof(boolAttr));
            }
        }

        [CreateProperty]
        public Color colorAttr
        {
            get => m_ColorAttr;
            set
            {
                if (m_ColorAttr == value)
                    return;
                m_ColorAttr = value;
                NotifyPropertyChanged(nameof(colorAttr));
            }
        }

        [CreateProperty]
        public BuilderAttributesTestElement.Existance enumAttr
        {
            get => m_EnumAttr;
            set
            {
                if (m_EnumAttr == value)
                    return;
                m_EnumAttr = value;
                NotifyPropertyChanged(nameof(enumAttr));
            }
        }

        [CreateProperty]
        public Texture2D assetAttr
        {
            get => m_AssetAttr;
            set
            {
                if (m_AssetAttr == value)
                    return;
                m_AssetAttr = value;
                NotifyPropertyChanged(nameof(assetAttr));
            }
        }

        public List<BuilderAttributesTestObject> objectsAttr { get; set; }

        public bool unbindableAttr { get; set; }

        public event PropertyChangedEventHandler propertyChanged;

        void NotifyPropertyChanged(string property)
        {
            propertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

    }
}
