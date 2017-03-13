// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;


namespace UnityEngine.Windows.Speech
{
    // Mirrored in SpeechSystem.h
    public enum ConfidenceLevel
    {
        High     = 0,
        Medium   = 1,
        Low      = 2,
        Rejected = 3,
    }

    // Mirrored in SpeechSystem.h
    public enum SpeechSystemStatus
    {
        Stopped = 0,
        Running = 1,
        Failed  = 2,
    }

    // Mirrored in SpeechSystem.h
    public enum SpeechError
    {
        NoError                   = 0,
        TopicLanguageNotSupported = 1,
        GrammarLanguageMismatch   = 2,
        GrammarCompilationFailure = 3,
        AudioQualityFailure       = 4,
        PauseLimitExceeded        = 5,
        TimeoutExceeded           = 6,
        NetworkFailure            = 7,
        MicrophoneUnavailable     = 8,
        UnknownError              = 9,
    }

    // Mirrored in DictationRecognizer.h
    public enum DictationTopicConstraint
    {
        WebSearch,
        Form,
        Dictation
    }

    // Mirrored in DictationRecognizer.h
    public enum DictationCompletionCause
    {
        Complete,
        AudioQualityFailure,
        Canceled,
        TimeoutExceeded,
        PauseLimitExceeded,
        NetworkFailure,
        MicrophoneUnavailable,
        UnknownError
    }

    public struct SemanticMeaning
    {
        public string key;
        public string[] values;
    }

    public struct PhraseRecognizedEventArgs
    {
        public readonly ConfidenceLevel confidence;
        public readonly SemanticMeaning[] semanticMeanings;
        public readonly string text;
        public readonly DateTime phraseStartTime;
        public readonly TimeSpan phraseDuration;

        internal PhraseRecognizedEventArgs(string text, ConfidenceLevel confidence, SemanticMeaning[] semanticMeanings, DateTime phraseStartTime, TimeSpan phraseDuration)
        {
            this.text = text;
            this.confidence = confidence;
            this.semanticMeanings = semanticMeanings;
            this.phraseStartTime = phraseStartTime;
            this.phraseDuration = phraseDuration;
        }
    };

    static partial class PhraseRecognitionSystem
    {
        public delegate void ErrorDelegate(SpeechError errorCode);
        public static event ErrorDelegate OnError;

        public delegate void StatusDelegate(SpeechSystemStatus status);
        public static event StatusDelegate OnStatusChanged;

        [RequiredByNativeCode]
        private static void PhraseRecognitionSystem_InvokeErrorEvent(SpeechError errorCode)
        {
            var onError = OnError;
            if (onError != null)
                onError(errorCode);
        }

        [RequiredByNativeCode]
        private static void PhraseRecognitionSystem_InvokeStatusChangedEvent(SpeechSystemStatus status)
        {
            var onStatusChanged = OnStatusChanged;
            if (onStatusChanged != null)
                onStatusChanged(status);
        }
    }

    public abstract partial class PhraseRecognizer : IDisposable
    {
        protected IntPtr m_Recognizer;

        public delegate void PhraseRecognizedDelegate(PhraseRecognizedEventArgs args);
        public event PhraseRecognizedDelegate OnPhraseRecognized;

        internal PhraseRecognizer()
        {
        }

        ~PhraseRecognizer()
        {
            if (m_Recognizer != IntPtr.Zero)
            {
                DestroyThreaded(m_Recognizer);
                m_Recognizer = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }

        public void Start()
        {
            if (m_Recognizer == IntPtr.Zero)
                return;

            Start_Internal(m_Recognizer);
        }

        public void Stop()
        {
            if (m_Recognizer == IntPtr.Zero)
                return;

            Stop_Internal(m_Recognizer);
        }

        public void Dispose()
        {
            if (m_Recognizer != IntPtr.Zero)
            {
                Destroy(m_Recognizer);
                m_Recognizer = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        public bool IsRunning { get { return m_Recognizer != IntPtr.Zero && IsRunning_Internal(m_Recognizer); } }

        [RequiredByNativeCode]
        private void InvokePhraseRecognizedEvent(string text, ConfidenceLevel confidence, SemanticMeaning[] semanticMeanings, long phraseStartFileTime, long phraseDurationTicks)
        {
            var onPhraseRecognized = OnPhraseRecognized;
            if (onPhraseRecognized != null)
                onPhraseRecognized(new PhraseRecognizedEventArgs(text, confidence, semanticMeanings, DateTime.FromFileTime(phraseStartFileTime), TimeSpan.FromTicks(phraseDurationTicks)));
        }

        [RequiredByNativeCode]
        private static unsafe SemanticMeaning[] MarshalSemanticMeaning(IntPtr keys, IntPtr values, IntPtr valueSizes, int valueCount)
        {
            var result = new SemanticMeaning[valueCount];
            int valueIndex = 0;

            for (int i = 0; i < valueCount; i++)
            {
                var ithValueSize = ((uint*)valueSizes)[i];
                var semanticMeaning = new SemanticMeaning
                {
                    key = new string(((char**)keys)[i]),
                    values = new string[ithValueSize]
                };

                for (int j = 0; j < ithValueSize; j++)
                    semanticMeaning.values[j] = new string(((char**)values)[valueIndex + j]);

                result[i] = semanticMeaning;
                valueIndex += (int)ithValueSize;
            }

            return result;
        }
    }

    sealed public class KeywordRecognizer : PhraseRecognizer
    {
        public IEnumerable<string> Keywords { get; private set; }


        public KeywordRecognizer(string[] keywords) :
            this(keywords, ConfidenceLevel.Medium)
        {
        }

        public KeywordRecognizer(string[] keywords, ConfidenceLevel minimumConfidence)
        {
            if (keywords == null)
                throw new ArgumentNullException("keywords");

            if (keywords.Length == 0)
                throw new ArgumentException("At least one keyword must be specified.", "keywords");

            var keywordsLength = keywords.Length;
            for (int i = 0; i < keywordsLength; i++)
            {
                if (keywords[i] == null)
                    throw new ArgumentNullException(string.Format("Keyword at index {0} is null.", i));
            }

            Keywords = keywords;
            m_Recognizer = CreateFromKeywords(keywords, minimumConfidence);
        }
    }

    sealed public class GrammarRecognizer : PhraseRecognizer
    {
        public string GrammarFilePath { get; private set; }

        public GrammarRecognizer(string grammarFilePath) :
            this(grammarFilePath, ConfidenceLevel.Medium)
        {
        }

        public GrammarRecognizer(string grammarFilePath, ConfidenceLevel minimumConfidence)
        {
            if (grammarFilePath == null)
                throw new ArgumentNullException("grammarFilePath");

            if (grammarFilePath.Length == 0)
                throw new ArgumentException("Grammar file path cannot be empty.");

            GrammarFilePath = grammarFilePath;
            m_Recognizer = CreateFromGrammarFile(grammarFilePath, minimumConfidence);
        }
    }

    sealed public partial class DictationRecognizer : IDisposable
    {
        private IntPtr m_Recognizer;

        public delegate void DictationHypothesisDelegate(string text);
        public delegate void DictationResultDelegate(string text, ConfidenceLevel confidence);
        public delegate void DictationCompletedDelegate(DictationCompletionCause cause);
        public delegate void DictationErrorHandler(string error, int hresult);

        public event DictationHypothesisDelegate DictationHypothesis;
        public event DictationResultDelegate DictationResult;
        public event DictationCompletedDelegate DictationComplete;
        public event DictationErrorHandler DictationError;

        public SpeechSystemStatus Status { get { return m_Recognizer != IntPtr.Zero ? GetStatus(m_Recognizer) : SpeechSystemStatus.Stopped; } }

        public float AutoSilenceTimeoutSeconds
        {
            get
            {
                if (m_Recognizer == IntPtr.Zero)
                    return 0.0f;

                return GetAutoSilenceTimeoutSeconds(m_Recognizer);
            }
            set
            {
                if (m_Recognizer == IntPtr.Zero)
                    return;

                SetAutoSilenceTimeoutSeconds(m_Recognizer, value);
            }
        }

        public float InitialSilenceTimeoutSeconds
        {
            get
            {
                if (m_Recognizer == IntPtr.Zero)
                    return 0.0f;

                return GetInitialSilenceTimeoutSeconds(m_Recognizer);
            }
            set
            {
                if (m_Recognizer == IntPtr.Zero)
                    return;

                SetInitialSilenceTimeoutSeconds(m_Recognizer, value);
            }
        }

        public DictationRecognizer() :
            this(ConfidenceLevel.Medium, DictationTopicConstraint.Dictation)
        {
        }

        public DictationRecognizer(ConfidenceLevel confidenceLevel) :
            this(confidenceLevel, DictationTopicConstraint.Dictation)
        {
        }

        public DictationRecognizer(DictationTopicConstraint topic) :
            this(ConfidenceLevel.Medium, topic)
        {
        }

        public DictationRecognizer(ConfidenceLevel minimumConfidence, DictationTopicConstraint topic)
        {
            m_Recognizer = Create(minimumConfidence, topic);
        }

        ~DictationRecognizer()
        {
            if (m_Recognizer != IntPtr.Zero)
            {
                DestroyThreaded(m_Recognizer);
                m_Recognizer = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }

        public void Start()
        {
            if (m_Recognizer == IntPtr.Zero)
                return;

            Start(m_Recognizer);
        }

        public void Stop()
        {
            if (m_Recognizer == IntPtr.Zero)
                return;

            Stop(m_Recognizer);
        }

        public void Dispose()
        {
            if (m_Recognizer != IntPtr.Zero)
            {
                Destroy(m_Recognizer);
                m_Recognizer = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        [RequiredByNativeCode]
        private void DictationRecognizer_InvokeHypothesisGeneratedEvent(string keyword)
        {
            var handler = DictationHypothesis;
            if (handler != null)
                handler(keyword);
        }

        [RequiredByNativeCode]
        private void DictationRecognizer_InvokeResultGeneratedEvent(string keyword, ConfidenceLevel minimumConfidence)
        {
            var handler = DictationResult;
            if (handler != null)
                handler(keyword, minimumConfidence);
        }

        [RequiredByNativeCode]
        private void DictationRecognizer_InvokeCompletedEvent(DictationCompletionCause cause)
        {
            var handler = DictationComplete;
            if (handler != null)
                handler(cause);
        }

        [RequiredByNativeCode]
        private void DictationRecognizer_InvokeErrorEvent(string error, int hresult)
        {
            var handler = DictationError;
            if (handler != null)
                handler(error, hresult);
        }
    }
}

