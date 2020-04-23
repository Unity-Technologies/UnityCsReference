using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    static class VisualElementFactoryRegistry
    {
        private static Dictionary<string, List<IUxmlFactory>> s_Factories;

        internal static Dictionary<string, List<IUxmlFactory>> factories
        {
            get
            {
                if (s_Factories == null)
                {
                    s_Factories = new Dictionary<string, List<IUxmlFactory>>();
                    RegisterEngineFactories();
                }

                return s_Factories;
            }
        }

        internal static void RegisterFactory(IUxmlFactory factory)
        {
            List<IUxmlFactory> factoryList;
            if (factories.TryGetValue(factory.uxmlQualifiedName, out factoryList))
            {
                foreach (IUxmlFactory f in factoryList)
                {
                    if (f.GetType() == factory.GetType())
                    {
                        throw new ArgumentException("A factory of this type was already registered");
                    }
                }
                factoryList.Add(factory);
            }
            else
            {
                factoryList = new List<IUxmlFactory>();
                factoryList.Add(factory);
                factories.Add(factory.uxmlQualifiedName, factoryList);
            }
        }

        internal static bool TryGetValue(string fullTypeName, out List<IUxmlFactory> factoryList)
        {
            factoryList = null;
            return factories.TryGetValue(fullTypeName, out factoryList);
        }

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
                new Toggle.UxmlFactory(),
                new TextField.UxmlFactory(),
                new TemplateContainer.UxmlFactory(),
                new Box.UxmlFactory(),
                new HelpBox.UxmlFactory(),
                new PopupWindow.UxmlFactory(),
                new ListView.UxmlFactory(),
                new TreeView.UxmlFactory(),
                new Foldout.UxmlFactory(),
                new BindableElement.UxmlFactory(),
            };

            foreach (var factory in factories)
            {
                RegisterFactory(factory);
            }
        }
    }
}
