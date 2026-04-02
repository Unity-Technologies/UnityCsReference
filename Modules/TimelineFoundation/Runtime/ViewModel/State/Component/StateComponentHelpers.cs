// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.CSO;

namespace Unity.Timeline.Foundation.ViewModel.Internals
{
    static class StateComponentHelpers
    {
        public static TData GetData<TData>(this IState state) where TData : struct, IReadOnlyData
        {
            IComponent<TData> component = GetComponentForData<TData>(state);
            if (component == null)
                return default;
            return component.readonlyData;
        }

        public static Component<TData> GetComponentForData<TData>(this IState state) where TData : struct, IReadOnlyData
        {
            return GetComponent<Component<TData>>(state);
        }

        public static TComponent GetComponent<TComponent>(this IState state) where TComponent : Component
        {
            foreach (IStateComponent component in state.AllStateComponents)
            {
                if (component is TComponent specificComponent)
                    return specificComponent;
            }

            return null;
        }

        public static IEnumerable<Component> GetAllComponents(this IState state)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return state.AllStateComponents.OfType<Component>();
#pragma warning restore UA2001
        }
    }
}
