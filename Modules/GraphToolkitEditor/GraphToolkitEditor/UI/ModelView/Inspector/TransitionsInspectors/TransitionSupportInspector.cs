// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class TransitionSupportInspector : ModelInspector
    {
        public new static readonly string ussClassName = "ge-transition-inspector";

        static readonly string k_PropertiesContainerName = "properties-container";

        ModelView m_TransitionSupportEditor;

        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        TransitionSupportModel TransitionSupportModel => Models.First() as TransitionSupportModel;
#pragma warning restore RS0030

        protected override void BuildUI()
        {
            if (Context is InspectorSectionContext inspectorSectionContext)
            {
                if (inspectorSectionContext.Section.SectionType == SectionType.Properties)
                {
                    m_TransitionSupportEditor = ModelViewFactory.CreateUI<ModelView>(RootView, TransitionSupportModel, null, this);
                    m_TransitionSupportEditor.AddToClassList(ussClassName.WithUssElement(k_PropertiesContainerName));
                    Add(m_TransitionSupportEditor);
                }
            }
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            AddToClassList(ussClassName);
        }

        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            m_TransitionSupportEditor?.UpdateUIFromModel(visitor);
        }

        public override bool IsEmpty()
        {
            if (Context is InspectorSectionContext inspectorSectionContext)
            {
                switch (inspectorSectionContext.Section.SectionType)
                {
                    case SectionType.Properties:
                        return false;
                    case SectionType.Options:
                    case SectionType.Advanced:
                        return true;
                }
            }
            return true;
        }

        public override void RemoveFromRootView()
        {
            m_TransitionSupportEditor.RemoveFromRootView();
            base.RemoveFromRootView();
        }
    }
}
