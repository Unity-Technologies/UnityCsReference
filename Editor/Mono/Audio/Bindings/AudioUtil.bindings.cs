// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/Audio/Bindings/AudioUtil.bindings.h")]
    [StaticAccessor("AudioUtilScriptBindings", StaticAccessorType.DoubleColon)]
    internal class AudioUtil
    {
        [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
        extern public static bool resetAllAudioClipPlayCountsOnPlay { get; set; }

        [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
        extern public static void PlayPreviewClip(AudioClip clip, int startSample = 0, bool loop = false);

        [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
        extern public static void PausePreviewClip();

        [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
        extern public static void ResumePreviewClip();

        [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
        extern public static void LoopPreviewClip(bool on);

        [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
        extern public static bool IsPreviewClipPlaying();

        [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
        extern public static void StopAllPreviewClips();

        [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
        extern public static float GetPreviewClipPosition();

        [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
        extern public static int GetPreviewClipSamplePosition();

        [StaticAccessor("GetAudioManager()", StaticAccessorType.Dot)]
        extern public static void SetPreviewClipSamplePosition(AudioClip clip, int iSamplePosition);

        extern public static int GetSampleCount(AudioClip clip);
        extern public static int GetChannelCount(AudioClip clip);
        extern public static int GetBitRate(AudioClip clip);
        extern public static int GetBitsPerSample(AudioClip clip);
        extern public static int GetFrequency(AudioClip clip);
        extern public static int GetSoundSize(AudioClip clip);
        extern public static AudioCompressionFormat GetSoundCompressionFormat(AudioClip clip);
        extern public static AudioCompressionFormat GetTargetPlatformSoundCompressionFormat(AudioClip clip);

        extern public static bool canUseSpatializerEffect
        {
            [FreeFunction(Name = "GetAudioManager().CanUseSpatializerEffect")]
            get;
        }

        extern public static string[] GetAmbisonicDecoderPluginNames();
        extern public static bool HasPreview(AudioClip clip);
        extern public static AudioImporter GetImporterFromClip(AudioClip clip);
        extern public static float[] GetMinMaxData(AudioImporter importer);
        extern public static double GetDuration(AudioClip clip);

        [FreeFunction(Name = "GetAudioManager().GetMemoryAllocated")]
        extern public static int GetFMODMemoryAllocated();

        [FreeFunction(Name = "GetAudioManager().GetCPUUsage")]
        extern public static float GetFMODCPUUsage();

        extern public static bool IsTrackerFile(AudioClip clip);
        extern public static int GetMusicChannelCount(AudioClip clip);

        extern public static AnimationCurve GetLowpassCurve(AudioLowPassFilter lowPassFilter);
        extern public static Vector3 GetListenerPos();
        extern public static void UpdateAudio();
        extern public static void SetListenerTransform(Transform t);

        extern public static bool HasAudioCallback(MonoBehaviour behaviour);

        extern public static int GetCustomFilterChannelCount(MonoBehaviour behaviour);
        extern public static int GetCustomFilterProcessTime(MonoBehaviour behaviour);
        extern public static float GetCustomFilterMaxIn(MonoBehaviour behaviour, int channel);
        extern public static float GetCustomFilterMaxOut(MonoBehaviour behaviour, int channel);

        extern internal static void SetProfilerShowAllGroups(bool value);
    }
}
