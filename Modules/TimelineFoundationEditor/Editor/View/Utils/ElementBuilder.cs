// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    /// <summary>
    /// Class used to select a method to build VisualElements based on an object type.
    /// </summary>
    /// <typeparam name="TContext">The context to use when building a <typeparamref name="TElement"/></typeparam>
    /// <typeparam name="TKey">The type that will be used to map data to a builder method.</typeparam>
    /// <typeparam name="TElement">The type of visual element we want to build.</typeparam>
    abstract class ElementBuilder<TContext, TKey, TElement>
        where TKey : class
        where TElement : VisualElement
    {
        Dictionary<Type, Func<TContext, TElement>> m_Builders = new Dictionary<Type, Func<TContext, TElement>>();
        Func<TContext, TElement> m_DefaultBuilder;

        protected abstract TKey GetKey(TContext context);

        protected virtual void PostBuildElement(TContext context, TElement element) { }

        public void Register<TType>(Func<TContext, TElement> builder)
            where TType : TKey
        {
            m_Builders.Add(typeof(TType), builder.Invoke);
        }

        public void RegisterDefault(Func<TContext, TElement> builder)
        {
            m_DefaultBuilder = builder;
        }

        public TElement BuildElement(TContext context)
        {
            TKey key = GetKey(context);
            if (key != null && m_Builders.TryGetValue(key.GetType(), out Func<TContext, TElement> functor))
            {
                return BuildElement(functor, context);
            }

            if (m_DefaultBuilder != null)
            {
                return BuildElement(m_DefaultBuilder, context);
            }

            if (key != null)
                Debug.LogError($"{typeof(TElement).Name} builder not registered for type {key.GetType()}");
            return null;
        }

        TElement BuildElement(Func<TContext, TElement> builder, TContext context)
        {
            TElement element = builder.Invoke(context);
            if (element != null)
                PostBuildElement(context, element);
            return element;
        }
    }
}
