// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UnityEngine.Experimental.UIElements
{
    internal interface IUxmlFactory
    {
        Type CreatesType { get; }
        VisualElement Create(IUxmlAttributes bag, CreationContext cc);
    }

    public abstract class UxmlFactory<T> : IUxmlFactory where T : VisualElement
    {
        public Type CreatesType { get { return typeof(T); } }
        public VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            return DoCreate(bag, cc);
        }

        protected abstract T DoCreate(IUxmlAttributes bag, CreationContext cc);
    }

    internal static class Factories
    {
        private static Dictionary<string, Func<IUxmlAttributes, CreationContext, VisualElement>> s_Factories;

        internal static void RegisterFactory(string fullTypeName, Func<IUxmlAttributes, CreationContext, VisualElement> factory)
        {
            DiscoverFactories();
            s_Factories.Add(fullTypeName, factory);
        }

        internal static void RegisterFactory<T>(Func<IUxmlAttributes, CreationContext, VisualElement> factory) where T : VisualElement
        {
            RegisterFactory(typeof(T).FullName, factory);
        }

        private static void DiscoverFactories()
        {
            if (s_Factories != null)
                return;

            s_Factories = new Dictionary<string, Func<IUxmlAttributes, CreationContext, VisualElement>>();
            CoreFactories.RegisterAll();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            HashSet<string> userAssemblies = new HashSet<string>(ScriptingRuntime.GetAllUserAssemblies());
            foreach (Assembly assembly in currentDomain.GetAssemblies())
            {
                if (!userAssemblies.Contains(assembly.GetName().Name + ".dll"))
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IUxmlFactory).IsAssignableFrom(type))
                        {
                            var factory = (IUxmlFactory)Activator.CreateInstance(type);
                            RegisterFactory(factory.CreatesType.FullName, factory.Create);
                        }
                    }
                }
                catch (TypeLoadException e)
                {
                    Debug.LogWarningFormat("Error while loading types from assembly {0}: {1}", assembly.FullName, e);
                }
            }
        }

        internal static bool TryGetValue(string fullTypeName, out Func<IUxmlAttributes, CreationContext, VisualElement> factory)
        {
            DiscoverFactories();

            factory = null;
            return s_Factories != null && s_Factories.TryGetValue(fullTypeName, out factory);
        }
    }

    internal static class CoreFactories
    {
        internal static void RegisterAll()
        {
            Factories.RegisterFactory<Button>(CreateButton);
            Factories.RegisterFactory<IMGUIContainer>(CreateIMGUIContainer);
            Factories.RegisterFactory<Image>((_, __) => new Image());
            Factories.RegisterFactory<Label>((_, __) => new Label());
            Factories.RegisterFactory<RepeatButton>(CreateRepeatButton);
            Factories.RegisterFactory<ScrollerButton>(CreateScrollerButton);
            Factories.RegisterFactory<ScrollView>((_, __) => new ScrollView());
            Factories.RegisterFactory<Scroller>(CreateScroller);
            Factories.RegisterFactory<Slider>(CreateSlider);
            Factories.RegisterFactory<TextField>((_, __) => new TextField());
            Factories.RegisterFactory<Toggle>(CreateToggle);
            Factories.RegisterFactory<VisualContainer>((_, __) => new VisualContainer());
            Factories.RegisterFactory<VisualElement>((_, __) => new VisualElement());
            Factories.RegisterFactory<TemplateContainer>(CreateTemplate);
        }

        private static VisualElement CreateButton(IUxmlAttributes bag, CreationContext ctx)
        {
            return new Button(null);
        }

        private static VisualElement CreateTemplate(IUxmlAttributes bag, CreationContext ctx)
        {
            string alias = ((TemplateAsset)bag).templateAlias;
            VisualTreeAsset vea = ctx.visualTreeAsset.ResolveUsing(alias);

            var tc = new TemplateContainer(alias);

            if (vea == null)
                tc.Add(new Label(string.Format("Unknown Element: '{0}'", alias)));
            else
                vea.CloneTree(tc, ctx.slotInsertionPoints);

            if (vea == null)
                Debug.LogErrorFormat("Could not resolve template with alias '{0}'", alias);

            return tc;
        }

        private static VisualElement CreateIMGUIContainer(IUxmlAttributes bag, CreationContext ctx)
        {
            return new IMGUIContainer(null);
        }

        private static VisualElement CreateRepeatButton(IUxmlAttributes bag, CreationContext ctx)
        {
            return new RepeatButton(null, bag.GetPropertyLong("delay", 0), bag.GetPropertyLong("interval", 0));
        }

        private static VisualElement CreateScrollerButton(IUxmlAttributes bag, CreationContext ctx)
        {
            return new ScrollerButton(null, bag.GetPropertyLong("delay", 0), bag.GetPropertyLong("interval", 0));
        }

        private static VisualElement CreateScroller(IUxmlAttributes bag, CreationContext ctx)
        {
            return new Scroller(
                bag.GetPropertyFloat("lowValue", 0),
                bag.GetPropertyFloat("highValue", 0),
                null,
                bag.GetPropertyEnum("direction", Slider.Direction.Horizontal)
                );
        }

        private static VisualElement CreateSlider(IUxmlAttributes bag, CreationContext ctx)
        {
            return new Slider(
                bag.GetPropertyFloat("lowValue", 0),
                bag.GetPropertyFloat("highValue", 0),
                null,
                bag.GetPropertyEnum("direction", Slider.Direction.Horizontal)
                );
        }

        private static VisualElement CreateToggle(IUxmlAttributes bag, CreationContext ctx)
        {
            return new Toggle(null);
        }
    }
}
