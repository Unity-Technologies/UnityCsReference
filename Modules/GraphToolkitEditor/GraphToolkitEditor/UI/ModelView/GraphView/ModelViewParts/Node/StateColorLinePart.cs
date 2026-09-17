// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class StateColorLinePart : NodeColorLinePart
    {
        public static StateColorLinePart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is AbstractNodeModel)
                return new StateColorLinePart(name, model, ownerElement, parentClassName);
            return null;
        }

        protected StateColorLinePart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        protected override bool HasMarker => true;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            base.BuildUI(container);

            // The accent bar covers the top of the state, so it must not capture clicks meant for the state.
            Root.pickingMode = PickingMode.Ignore;
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            Root.AddPackageStylesheet("ColorLinePart.uss");
        }
    }
}
