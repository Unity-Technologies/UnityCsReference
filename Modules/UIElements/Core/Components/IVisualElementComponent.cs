// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.ComponentModel;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// The interface for all UI Toolkit component structs.
    /// </summary>
    /// <remarks>
    /// You don't implement this interface yourself: when you declare a <c>partial struct</c> with
    /// the <see cref="VisualElementComponentAttribute"/>, Unity's source generator adds the interface to the
    /// generated part of the struct. The interface is an implementation detail of that generator: its
    /// members are not a stable API, and Unity can add, change, or remove them in any version.
    /// The component methods on <see cref="VisualElement"/>, such as
    /// <see cref="VisualElement.AddComponent{T}"/>, accept any struct that has it.
    /// The hooks are emitted by the source generator, never written by hand. They let
    /// <c>AddComponent</c> / <c>RemoveComponent</c> (un)register a component's
    /// <c>[RegisterCallback]</c> handlers and validate its <c>[RequiresElementOfType]</c> owner
    /// constraint through a constrained generic call, resolved by the compiler, so there is no
    /// reflection and no boxing of the component struct. A component that declares no callbacks
    /// (or no owner constraint) gets empty generated bodies. The storage hot path
    /// (<c>GetComponent</c>) never calls these, so it is unaffected.
    /// </remarks>
    public interface IVisualElementComponent
    {
        // Generator-emitted; do not call directly. Registers the component's [RegisterCallback]
        // handlers on the owner element. Empty when the component declares none.
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void __RegisterComponentCallbacks(VisualElement owner);

        // Generator-emitted; do not call directly. Mirror of __RegisterComponentCallbacks, invoked
        // from RemoveComponent.
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void __UnregisterComponentCallbacks(VisualElement owner);

        // Generator-emitted; do not call directly. Throws if the owner doesn't satisfy the
        // component's [RequiresElementOfType] constraint. Empty when the component is unconstrained.
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void __ValidateOwnerType(VisualElement owner);

        // Generator-emitted; do not call directly. Returns the component's shared [OnComponentChanged]
        // dispatcher (a per-type static delegate, so no per-instance allocation), or null when the
        // component declares no handler. AddComponent stores the result in the component's slot.
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        System.Action<VisualElement> __GetComponentChangedDispatcher();

        // Generator-emitted; do not call directly. Returns the shared per-type dispatcher that routes
        // component-targeted data bindings (${component:Type}.field) into the live component data.
        // AddComponent stores the result in the component's slot.
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ComponentBindingDispatcher __GetComponentBindingDispatcher();

        // Generator-emitted; do not call directly. Returns the component's [UxmlCreateInstanceMethod]
        // factory as a Func<T>, or null when the component has none. GetOrAddComponent uses it to
        // create a new component.
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        System.Delegate __GetComponentCreateInstanceFactory();

        // Generator-emitted; do not call directly. Adds the component's [RequiresComponentOfType]
        // components, in declaration order, with GetOrAddComponent. Does nothing when the component
        // has no requirements.
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void __ResolveComponentRequirements(VisualElement owner);

        // Generator-emitted; do not call directly. Returns the type handles of the component's
        // [RequiresComponentOfType] declarations, or null when there are none. Only used by the
        // editor-only warning in RemoveComponent.
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        System.RuntimeTypeHandle[] __GetRequiredComponentTypes();

        // Generator-emitted; do not call directly. Runs the component's [OnComponentAdded] method
        // with the live component data; the add path calls it last, after storage and callback
        // registration. Returns whether a method ran (it may have changed the component list).
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool __InvokeComponentAdded(VisualElement owner);

        // Generator-emitted; do not call directly. Mirror of __InvokeComponentAdded for the
        // [OnComponentRemoved] method; RemoveComponent calls it first, before anything is torn down.
        /// <undoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool __InvokeComponentRemoved(VisualElement owner);
    }
}
