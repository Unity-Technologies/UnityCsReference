// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class SampleToolBarButton : ToolbarButtonBase<Sample, Sample>
    {
        private SampleAction m_SampleAction;

        protected SampleToolBarButton(SampleAction action) : base(action)
        {
            m_SampleAction = action;
        }

        protected override Sample GetSingleItemFromBulkItem(Sample sample) => sample;
    }

    // Concrete implementation for a simple text button
    internal class SampleToolBarSimpleButton : SampleToolBarButton
    {
        private readonly Button m_Button;

        public SampleToolBarSimpleButton(SampleAction action) : base(action)
        {
            m_Button = new Button(TriggerAction);
            m_Button.AddToClassList("action-button");
            Add(m_Button);
        }

        protected override string text
        {
            set => m_Button.text = value;
        }
    }
}
