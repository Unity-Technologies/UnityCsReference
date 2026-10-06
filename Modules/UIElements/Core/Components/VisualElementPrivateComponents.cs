// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    // Storage for VisualElement members that only a small fraction of elements ever use. A group is the set
    // of members one feature reads and writes together, so an element that opts into one field pays for the
    // others it will most likely also use. Storage is created on the first non-default write and then kept
    // for the element's lifetime: removing it would shift the element's other component slots and
    // invalidate any ref handed out by GetComponent.

    [VisualElementComponent(exposeToUxml: false), HideInInspector]
    internal partial struct VisualElementDataBindingComponent
    {
        internal object dataSource;
        internal PathRef dataSourcePath;
        internal Type dataSourceType;
        internal List<Binding> bindings;   // uxml serialization authoring only
    }

    [VisualElementComponent(exposeToUxml: false), HideInInspector]
    internal partial struct VisualElementAnimationComponent
    {
        internal List<IValueAnimationUpdate> runningAnimations;
        internal int runningAnimationCount;
        internal int completedAnimationCount;
    }

    [VisualElementComponent(exposeToUxml: false), HideInInspector]
    internal partial struct VisualElementPaintingComponent
    {
        internal Action<MeshGenerationContext> generateVisualContent;
        internal List<MeshModifierRegistration> meshModifiers;
    }

    [VisualElementComponent(exposeToUxml: false), HideInInspector]
    internal partial struct VisualElementViewDataComponent
    {
        internal string viewDataKey;
    }

    [VisualElementComponent(exposeToUxml: false), HideInInspector]
    internal partial struct VisualElementStyleSheetsComponent
    {
        internal List<StyleSheet> styleSheetList;
    }
}
