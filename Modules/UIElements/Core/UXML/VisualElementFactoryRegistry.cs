// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class VisualElementFactoryRegistry
    {
        #pragma warning disable CS0618 // Type or member is obsolete

        private static Dictionary<string, List<IUxmlFactory>> s_Factories;
        private static Dictionary<string, List<IUxmlFactory>> s_MovedTypesFactories;

        internal static string GetMovedUIControlTypeName(Type type, MovedFromAttribute attr)
        {
            if (type == null)
                return string.Empty;

            var data = attr.data;
            var namespaceName = data.nameSpaceHasChanged ? data.nameSpace : type.Namespace;
            var typeName = data.classHasChanged ? data.className : type.Name;
            var fullOldName = namespaceName + "." + typeName;
            return fullOldName;
        }

        internal static Dictionary<string, List<IUxmlFactory>> factories
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get
            {
                if (s_Factories == null)
                {
                    s_Factories = new Dictionary<string, List<IUxmlFactory>>();
                    s_MovedTypesFactories = new Dictionary<string, List<IUxmlFactory>>(50);
                    RegisterEngineFactories();
                    RegisterUserFactories();
                }

                return s_Factories;
            }
        }

        protected static void RegisterFactory(IUxmlFactory factory)
        {
            if (factories.TryGetValue(factory.uxmlQualifiedName, out var factoryList))
            {
                foreach (var f in factoryList)
                {
                    if (f.GetType() == factory.GetType())
                    {
                        throw new ArgumentException($"A factory for the type {factory.GetType().FullName} was already registered");
                    }
                }
                factoryList.Add(factory);
            }
            else
            {
                factoryList = new List<IUxmlFactory>();
                factoryList.Add(factory);
                s_Factories.Add(factory.uxmlQualifiedName, factoryList);
                var uxmlType = factory.uxmlType;
                var attr = uxmlType?.GetCustomAttribute<MovedFromAttribute>(false);
                if (attr != null && typeof(VisualElement).IsAssignableFrom(uxmlType))
                {
                    var movedTypeName = GetMovedUIControlTypeName(uxmlType, attr);
                    if (string.IsNullOrEmpty(movedTypeName) == false)
                        s_MovedTypesFactories.Add(movedTypeName, factoryList);
                }
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static bool TryGetValue(string fullTypeName, out List<IUxmlFactory> factoryList)
        {
            var ret = factories.TryGetValue(fullTypeName, out factoryList);
            if (ret == false)
                ret = s_MovedTypesFactories.TryGetValue(fullTypeName, out factoryList);
            return ret;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static bool TryGetValue(Type type, out List<IUxmlFactory> factoryList)
        {
            foreach (var fl in factories.Values)
            {
                if (fl[0].uxmlType == type)
                {
                    factoryList = fl;
                    return true;
                }
            }

            factoryList = null;
            return false;
        }

        // Core UI Toolkit elements must be registered manually for both Editor and Player use cases.
        // For performance in the Player we want to avoid scanning any builtin Unity assembly with reflection.
        // Ideally a mechanism similar to the TypeCache in the Player would exist and remove the need for this.
        static void RegisterEngineFactories()
        {
            IUxmlFactory[] factories =
            {
                // Dummy factories. Just saying that these types exist and what are their attributes.
                // Used for schema generation.
                new UxmlRootElementFactory(),
                new UxmlTemplateFactory(),
                new UxmlStyleFactory(),
                new UxmlAttributeOverridesFactory(),

                // Real object instantiating factories.
                new Button.UxmlFactory(),
                new ToggleButtonGroup.UxmlFactory(),
                new VisualElement.UxmlFactory(),
                new IMGUIContainer.UxmlFactory(),
                new Image.UxmlFactory(),
                new Label.UxmlFactory(),
                new RepeatButton.UxmlFactory(),
                new ScrollView.UxmlFactory(),
                new Scroller.UxmlFactory(),
                new Slider.UxmlFactory(),
                new SliderInt.UxmlFactory(),
                new MinMaxSlider.UxmlFactory(),
                new GroupBox.UxmlFactory(),
                new RadioButton.UxmlFactory(),
                new RadioButtonGroup.UxmlFactory(),
                new Toggle.UxmlFactory(),
                new TextField.UxmlFactory(),
                new TemplateContainer.UxmlFactory(),
                new Box.UxmlFactory(),
                new EnumField.UxmlFactory(),
                new DropdownField.UxmlFactory(),
                new HelpBox.UxmlFactory(),
                new PopupWindow.UxmlFactory(),
                new ProgressBar.UxmlFactory(),
                new ListView.UxmlFactory(),
                new TwoPaneSplitView.UxmlFactory(),
                new TreeView.UxmlFactory(),
                new Foldout.UxmlFactory(),
                new MultiColumnListView.UxmlFactory(),
                new MultiColumnTreeView.UxmlFactory(),
                new BindableElement.UxmlFactory(),
                new TextElement.UxmlFactory(),
                new ButtonStripField.UxmlFactory(),
                new FloatField.UxmlFactory(),
                new DoubleField.UxmlFactory(),
                new Hash128Field.UxmlFactory(),
                new IntegerField.UxmlFactory(),
                new LongField.UxmlFactory(),
                new UnsignedIntegerField.UxmlFactory(),
                new UnsignedLongField.UxmlFactory(),
                new RectField.UxmlFactory(),
                new Vector2Field.UxmlFactory(),
                new RectIntField.UxmlFactory(),
                new Vector3Field.UxmlFactory(),
                new Vector4Field.UxmlFactory(),
                new Vector2IntField.UxmlFactory(),
                new Vector3IntField.UxmlFactory(),
                new BoundsField.UxmlFactory(),
                new BoundsIntField.UxmlFactory(),
                new Tab.UxmlFactory(),
                new TabView.UxmlFactory(),
            };

            foreach (var factory in factories)
            {
                RegisterFactory(factory);
            }
        }

        internal static void RegisterUserFactories()
        {
            // In the Player, we filter assemblies to only introspect types of user assemblies
            // which will exclude Unity builtin assemblies (i.e. runtime modules).
        }
        #pragma warning restore CS0618 // Type or member is obsolete
    }
}
