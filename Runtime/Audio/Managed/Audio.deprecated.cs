// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
namespace UnityEngine
{
    partial class AudioSettings
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("AudioSettings.driverCaps is obsolete. Use driverCapabilities instead (UnityUpgradable) -> driverCapabilities", true)]
        public static AudioSpeakerMode driverCaps { get { return driverCapabilities; } }
    }

    partial class AudioSource
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("AudioSource.panLevel has been deprecated. Use AudioSource.spatialBlend instead (UnityUpgradable) -> spatialBlend", true)]
        public float panLevel { get { return spatialBlend; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("AudioSource.pan has been deprecated. Use AudioSource.panStereo instead (UnityUpgradable) -> panStereo", true)]
        public float pan { get { return panStereo; } set {} }
    }

    partial class AudioLowPassFilter
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("AudioLowPassFilter.lowpassResonaceQ is obsolete. Use lowpassResonanceQ instead (UnityUpgradable) -> lowpassResonanceQ", true)]
        public float lowpassResonaceQ { get { return lowpassResonanceQ; } set {} }
    }

    partial class AudioHighPassFilter
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("AudioHighPassFilter.highpassResonaceQ is obsolete. Use highpassResonanceQ instead (UnityUpgradable) -> highpassResonanceQ", true)]
        public float highpassResonaceQ { get { return highpassResonanceQ; } set {} }
    }

    partial class AudioReverbFilter
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("AudioReverbFilter.lFReference is obsolete. Use lfReference instead (UnityUpgradable) -> lfReference", true)]
        public float lFReference { get { return lfReference; } set {} }
    }
}
