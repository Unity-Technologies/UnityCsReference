// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements
{
    // Routes a data binding whose target path selects a component ("${component:TypeName}.field") into
    // that component's live data on the element, instead of the element itself.
    //
    // The selector is a virtual first path part, in the shape the upcoming Unity.Properties virtual
    // path parts use: it starts with "${", the resolver name runs to the first ':' (always "component"
    // here), the identifier runs from the ':' to the closing '}' and may contain dots (a namespaced
    // type name). Paths that don't match the shape fall through to the normal element path, which
    // reports the standard invalid-path error. The remaining parts form the sub-path inside the
    // component, applied against its generated property bag through ConverterGroup so value conversion
    // still happens; TValue is preserved (no boxing) through the dispatcher's generic method.
    static class ComponentBinding
    {
        const string k_SelectorPrefix = "${component:";
        const char k_SelectorSuffix = '}';

        // True when bindingPath is "${component:TypeName}.<sub-path>" and TypeName is attached to
        // element. Outputs the component's type handle and the sub-path addressing a field within it.
        internal static bool TryResolve(VisualElement element, in PropertyPath bindingPath,
            out RuntimeTypeHandle handle, out PropertyPath subPath)
        {
            handle = default;
            subPath = default;

            if (element == null || bindingPath.Length < 2)
                return false;

            var first = bindingPath[0];
            if (!first.IsName)
                return false;

            var name = first.Name;
            if (name == null || !name.StartsWith(k_SelectorPrefix, StringComparison.Ordinal))
                return false;

            // Rebuild the identifier up to the closing brace. The dots of a namespaced type name are
            // consumed as path separators, so keep appending name parts until one ends the selector.
            var identifier = name.Substring(k_SelectorPrefix.Length);
            var subStart = 1;
            while (identifier.Length == 0 || identifier[identifier.Length - 1] != k_SelectorSuffix)
            {
                // The selector never closed: not a component path, let the normal path report it.
                if (subStart >= bindingPath.Length)
                    return false;

                var next = bindingPath[subStart];
                if (!next.IsName)
                    return false;

                identifier = identifier + "." + next.Name;
                subStart++;
            }

            identifier = identifier.Substring(0, identifier.Length - 1);
            // "${component:}" carries no type name; not a component path.
            if (identifier.Length == 0)
                return false;

            // The selector alone is not a binding target; a field sub-path must follow.
            if (subStart >= bindingPath.Length)
                return false;

            if (!element.TryResolveComponentHandleByName(identifier, out handle))
                return false;

            var sub = new PropertyPath();
            for (var i = subStart; i < bindingPath.Length; i++)
            {
                var part = bindingPath[i];
                if (part.IsName)
                    sub = PropertyPath.AppendName(sub, part.Name);
                else if (part.IsIndex)
                    sub = PropertyPath.AppendIndex(sub, part.Index);
                else
                    sub = PropertyPath.AppendKey(sub, part.Key);
            }

            subPath = sub;
            return true;
        }

        // Writes value into the selected component's data, converting through the source->UI group.
        internal static bool SetUI<TValue>(ConverterGroup group, VisualElement element, RuntimeTypeHandle handle,
            in PropertyPath subPath, TValue value, out VisitReturnCode returnCode)
        {
            var dispatcher = element.GetComponentBindingDispatcher(handle);
            if (dispatcher == null)
            {
                returnCode = VisitReturnCode.NullContainer;   // component removed since the path resolved
                return false;
            }
            return dispatcher.SetUI(group, element, subPath, value, out returnCode);
        }

        // UI->source: read the current value out of the selected component's field. Boxed to object — the
        // source field type isn't known here, and UpdateSource's converter group converts object->source.
        internal static bool TryReadValue(VisualElement element, RuntimeTypeHandle handle, in PropertyPath subPath, out object value)
        {
            var dispatcher = element.GetComponentBindingDispatcher(handle);
            if (dispatcher == null)
            {
                value = null;
                return false;
            }
            return dispatcher.Read(element, subPath, out value);
        }
    }

    /// <summary>
    /// Dispatches component-targeted data bindings (<c>${component:Type}.field</c>) into the live
    /// component data of an element. The generated <c>__GetComponentBindingDispatcher</c> hook returns
    /// <see cref="ComponentBindingDispatcher{T}.Instance"/>, and AddComponent caches it in the
    /// component's slot, so binding updates dispatch without reflection.
    /// </summary>
    public abstract class ComponentBindingDispatcher
    {
        internal ComponentBindingDispatcher() { }

        internal abstract bool SetUI<TValue>(ConverterGroup group, VisualElement element, in PropertyPath subPath,
            TValue value, out VisitReturnCode returnCode);

        internal abstract bool Read(VisualElement element, in PropertyPath subPath, out object value);
    }

    /// <summary>
    /// The shared <see cref="ComponentBindingDispatcher"/> for component type <typeparamref name="T"/>.
    /// Generated code references <see cref="Instance"/> from the component's
    /// <c>__GetComponentBindingDispatcher</c> hook, closing the component type statically.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    public sealed class ComponentBindingDispatcher<T> : ComponentBindingDispatcher where T : struct, IVisualElementComponent
    {
        /// <summary>The shared dispatcher for <typeparamref name="T"/>.</summary>
        [NoAutoStaticsCleanup] // stateless shared singleton
        public static readonly ComponentBindingDispatcher<T> Instance = new ComponentBindingDispatcher<T>();

        ComponentBindingDispatcher() { }

        internal override bool SetUI<TValue>(ConverterGroup group, VisualElement element, in PropertyPath subPath,
            TValue value, out VisitReturnCode returnCode)
        {
            // ref into the live component data (unmanaged slot, or StrongBox for a managed component).
            ref var component = ref element.GetComponent<T>();
            var didSet = group.TrySetValue(ref component, subPath, value, out returnCode);

            // A binding writes the field directly (bypassing the generated notifying setters), so flag the
            // change ourselves: this lets [OnComponentChanged] fire for binding-driven updates, the same way
            // it does for imperative writes. The coalescing updater runs each handler at most once per frame.
            if (didSet)
                element.MarkComponentDirty<T>();

            return didSet;
        }

        internal override bool Read(VisualElement element, in PropertyPath subPath, out object value)
        {
            ref var component = ref element.GetComponent<T>();
            return PropertyContainer.TryGetValue(ref component, subPath, out value);
        }
    }
}
