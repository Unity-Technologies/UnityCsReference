// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Windows.Speech
{
    public static partial class PhraseRecognitionSystem
    {
        public extern static bool isSupported
        {
            [ThreadSafe]
            [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
            get;
        }

        public extern static SpeechSystemStatus Status
        {
            [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
            get;
        }

        [NativeThrows]
        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        public extern static void Restart();

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        public extern static void Shutdown();
    }

    public abstract partial class PhraseRecognizer : IDisposable
    {
        [NativeThrows]
        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        protected static extern IntPtr CreateFromKeywords(object self, string[] keywords, ConfidenceLevel minimumConfidence);

        [NativeThrows]
        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        protected static extern IntPtr CreateFromGrammarFile(object self, string grammarFilePath, ConfidenceLevel minimumConfidence);

        [NativeThrows]
        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static void Start_Internal(IntPtr recognizer);

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static void Stop_Internal(IntPtr recognizer);

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static bool IsRunning_Internal(IntPtr recognizer);

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static void Destroy(IntPtr recognizer);

        [ThreadSafe]
        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static void DestroyThreaded(IntPtr recognizer);
    }

    public partial class DictationRecognizer
    {
        [NativeThrows]
        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private static extern IntPtr Create(object self, ConfidenceLevel minimumConfidence, DictationTopicConstraint topicConstraint);

        [NativeThrows]
        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static void Start(IntPtr self);

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static void Stop(IntPtr self);

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static void Destroy(IntPtr self);

        [ThreadSafe]
        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static void DestroyThreaded(IntPtr self);

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static SpeechSystemStatus GetStatus(IntPtr self);

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static float GetAutoSilenceTimeoutSeconds(IntPtr self);

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static void SetAutoSilenceTimeoutSeconds(IntPtr self, float value);

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static float GetInitialSilenceTimeoutSeconds(IntPtr self);

        [NativeHeader("PlatformDependent/Win/Bindings/SpeechBindings.h")]
        private extern static void SetInitialSilenceTimeoutSeconds(IntPtr self, float value);
    }
}
