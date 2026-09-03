// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Derive from this class to add custom UI to the view generated for a specific <see cref="Condition"/> type in
    /// the transition inspector.
    /// </summary>
    /// <remarks>
    /// A <see cref="UnityEngine.UIElements.VisualElement"/> row is built for every <see cref="Condition"/> that
    /// appears in a transition's condition list. A <see cref="ConditionView{T}"/> lets you inject custom UI into that
    /// row. Subclasses are discovered by their type parameter: for a <see cref="Condition"/> of type <c>T</c>, the
    /// concrete <see cref="ConditionView{T}"/> whose type argument matches is used, walking up the
    /// <see cref="Condition"/> inheritance chain if there is no exact match.
    ///
    /// The <see cref="Condition"/> instance is available through <see cref="Condition"/> once the view is constructed. 
    /// To add custom UI next to the built-in condition UI, override <see cref="OnViewBuilt"/> and add
    /// your custom UI to <see cref="IConditionView.Root"/>.
    /// Do not remove or reparent the built-in elements. Use instead the relevant display toggle.
    /// <seealso cref="DisplayValueField"/>
    /// <seealso cref="DisplayTitleLabel"/>
    /// <seealso cref="Condition{T}.DisplayComparisonDropdown"/>
    ///
    /// <b>Important:</b> allocate custom UI in <see cref="OnViewBuilt"/>. Do not allocate UI in
    /// <see cref="OnViewAttached"/>: that callback can fire multiple times during the view's lifetime, and any UI
    /// you allocate there accumulates as duplicates. Instances of this class are created with the row and are never
    /// serialized; do not store state in them that must outlive the view.
    ///
    /// Exceptions thrown by the view's constructor or callbacks are logged and do not break the inspector: the
    /// condition row still renders its built-in UI.
    /// </remarks>
    /// <typeparam name="T">The <see cref="Condition"/> type this view customizes.</typeparam>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// // The Condition type that this view customizes.
    /// [Serializable]
    /// public class Health : Condition<float>
    /// {
    ///     protected override string Title => "Health";
    /// }
    ///
    /// // This view is constructed for every Health condition row in the transition inspector.
    /// class HealthConditionView : ConditionView<Health>
    /// {
    ///     // Replace the built-in value field with the controls built in OnViewBuilt.
    ///     protected override bool DisplayValueField => false;
    ///
    ///     public override void OnViewBuilt()
    ///     {
    ///         // Allocate custom UI once, after the built-in UI is ready.
    ///         var slider = new Slider(0f, 100f) { value = Condition.Value };
    ///         slider.RegisterValueChangedCallback(evt =>
    ///         {
    ///             // Record the write so it is undoable and marks the asset dirty.
    ///             var stateMachine = Condition.StateMachine;
    ///             stateMachine.UndoBeginRecordStateMachine("Change health threshold", Condition);
    ///             Condition.Value = evt.newValue;
    ///             stateMachine.UndoEndRecordStateMachine();
    ///         });
    ///
    ///         // Style custom UI with USS: load a stylesheet onto the row's content container...
    ///         View.Root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/HealthCondition.uss"));
    ///         // ...and tag elements with classes for it to match.
    ///         slider.AddToClassList("health-condition__slider");
    ///
    ///         View.Root.Add(slider);
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class ConditionView<T> : IUserConditionView
        where T : Condition
    {
        /// <summary>
        /// The <see cref="Condition"/> instance this view customizes.
        /// </summary>
        /// <remarks>
        /// This property is set before any callback fires, but is not available in the view's constructor. Use it
        /// to read the condition's data when building custom UI in <see cref="OnViewBuilt"/>.
        /// </remarks>
        public T Condition { get; internal set; }

        /// <summary>
        /// The generated view for this condition. Add custom UI to <see cref="IConditionView.Root"/>.
        /// </summary>
        /// <remarks>
        /// This property is set before any callback fires, but is not available in the view's constructor.
        /// </remarks>
        public IConditionView View { get; internal set; }

        /// <summary>
        /// Whether the built-in field for the condition's value is displayed.
        /// </summary>
        /// <remarks>
        /// Override this property to return <see langword="false"/> to remove the built-in value field and draw your
        /// own controls for the condition instead. The built-in title label then carries the condition's name; see
        /// <see cref="DisplayTitleLabel"/>. The comparison operator dropdown is unaffected; it remains controlled by
        /// <see cref="Condition{T}.DisplayComparisonDropdown"/>.
        /// </remarks>
        protected virtual bool DisplayValueField => true;

        /// <summary>
        /// Whether the built-in title label is displayed.
        /// </summary>
        /// <remarks>
        /// The title label appears when the condition shows the comparison dropdown, or when
        /// <see cref="DisplayValueField"/> removes the value field that would otherwise carry the condition's name.
        /// For a valueless condition, this property controls the built-in label that carries its name.
        /// Override this property to return <see langword="false"/> to remove it and provide your own labeling.
        /// </remarks>
        protected virtual bool DisplayTitleLabel => true;

        void IUserConditionView.Initialize(Condition condition, IConditionView view)
        {
            Condition = (T)condition;
            View = view;
        }

        bool IUserConditionView.DisplayValueField => DisplayValueField;

        bool IUserConditionView.DisplayTitleLabel => DisplayTitleLabel;

        /// <summary>
        /// Called once, after the condition's built-in UI is fully constructed.
        /// </summary>
        /// <remarks>
        /// This is the recommended entry point for allocating custom UI and adding it to
        /// <see cref="IConditionView.Root"/>. It fires exactly once per view instance, so any element allocated here
        /// can be safely cached in a field.
        /// </remarks>
        public virtual void OnViewBuilt() { }

        /// <summary>
        /// Called when the condition's row is attached to a UI panel.
        /// </summary>
        /// <remarks>
        /// This can fire multiple times during the view's lifetime — for example when the inspector is rebuilt or
        /// the row is reordered. Use this callback for logic that needs to respond to panel-attach events; do not
        /// allocate UI here.
        /// </remarks>
        public virtual void OnViewAttached() { }

        /// <summary>
        /// Called when the condition's row is detached from its UI panel.
        /// </summary>
        /// <remarks>
        /// Like <see cref="OnViewAttached"/>, this can fire multiple times during the view's lifetime, and a
        /// matching <see cref="OnViewAttached"/> may follow. Use this callback to release resources tied to panel
        /// presence, such as event subscriptions.
        /// </remarks>
        public virtual void OnViewDetached() { }
    }
}
