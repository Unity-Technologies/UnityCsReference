// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class SampleAction: ActionBase<Sample>
    {
        public override ToolbarButtonBase<Sample, Sample> CreateToolbarButton()
        {
            return new SampleToolBarSimpleButton(this);
        }

        protected abstract override bool TriggerActionImplementation(Sample sample);
        protected override bool TriggerActionImplementation(IReadOnlyCollection<Sample> samples) => false;
        public virtual string GetMultiSelectText(Sample sample) => GetText(sample, false);
    }
}
