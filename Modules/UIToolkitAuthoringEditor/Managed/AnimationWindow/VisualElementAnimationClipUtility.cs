// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal static class VisualElementAnimationClipUtility
    {

        // Predicates are static lambdas so there's no closure allocation per call.

        internal static VisualElement FindClipOwner(VisualElement element)
        {
            if (element == null)
                return null;
            if (element.resolvedStyle.unityAnimationClip != null)
                return element;
            return element.GetFirstAncestorWhere(static x => x.resolvedStyle.unityAnimationClip != null);
        }

        internal static PanelRenderer FindPanelRenderer(VisualElement element)
        {
            if (element == null)
                return null;

            // Predicate also checks panelComponent is PanelRenderer so the walk skips past
            // a UIDocumentRootElement ancestor (which is also IPanelComponentRootElement
            // but holds a UIDocument) instead of stopping there.
            var rootElement = element.GetFirstAncestorWhere(static x =>
                x is IPanelComponentRootElement r && r.panelComponent is PanelRenderer)
                as IPanelComponentRootElement;
            return rootElement?.panelComponent as PanelRenderer;
        }

        // AnimationClip::SetEditorPPtrCurve rejects bindings whose type is not a Component;
        // float/discrete only require Object, hence UIAnimationClip works there but not for PPtr.
        internal static readonly Type PerElementPPtrDiscriminatorType = typeof(PanelRenderer);
    }
}
