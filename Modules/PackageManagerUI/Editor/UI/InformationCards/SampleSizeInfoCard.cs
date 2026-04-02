// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SampleSizeInfoCard: SampleInformationCard
    {
        protected override string titleText => L10n.Tr("Sample Size");
        protected override InformationCardSize cardSize => InformationCardSize.Small;

        public override void Refresh(Sample sample)
        {
            contentText = UIUtils.ConvertToHumanReadableSize(sample.sizeInBytes);
        }
    }
}
