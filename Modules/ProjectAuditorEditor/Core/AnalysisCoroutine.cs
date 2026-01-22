// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class AnalysisCoroutine
    {
        struct YieldProcessor
        {
            enum DataType : byte
            {
                None = 0,
                WaitForEndOfFrame = 1,
                EditorCoroutine = 2,
                AsyncOp = 3,
            }
            struct ProcessorData
            {
                public DataType type;
                public object current;
            }

            ProcessorData data;

            public void Set(object yield)
            {
                if (yield == data.current)
                    return;

                var type = yield.GetType();
                var dataType = DataType.None;

                if (type == typeof(WaitForEndOfFrame))
                {
                    dataType = DataType.WaitForEndOfFrame;
                }
                else if (type == typeof(AnalysisCoroutine))
                {
                    dataType = DataType.EditorCoroutine;
                }
                else if (type == typeof(AsyncOperation) || type.IsSubclassOf(typeof(AsyncOperation)))
                {
                    dataType = DataType.AsyncOp;
                }

                data = new ProcessorData { current = yield, type = dataType };
            }

            public bool MoveNext(IEnumerator enumerator, out bool skipFrame)
            {
                bool advance = false;
                skipFrame = false;
                switch (data.type)
                {
                    case DataType.WaitForEndOfFrame:
                        skipFrame = true;
                        advance = true;
                        break;
                    case DataType.EditorCoroutine:
                        advance = (data.current as AnalysisCoroutine).m_Status == Status.Done;
                        break;
                    case DataType.AsyncOp:
                        advance = (data.current as AsyncOperation).isDone;
                        break;
                    default:
                        advance = data.current == enumerator.Current; //a IEnumerator or a plain object was passed to the implementation
                        break;
                }

                if (advance)
                {
                    data = default;
                    return enumerator.MoveNext();
                }

                return true;
            }
        }

        enum Status
        {
            Invalid,
            Stopped,
            Running,
            Done
        }

        WeakReference m_Owner;
        IEnumerator m_Routine;
        YieldProcessor m_Processor;
        Status m_Status = Status.Invalid;
        Action<long> m_ElapsedTimeDelegate; // Returns how long each invocation of the coroutine took

        const int kParallelCoroutineCount = 2; // Not really parallel, just number we allow to run at the same time (if you change it, consider changing kDuration too to balance overall fps)
        static Stack<IEnumerator> kIEnumeratorProcessingStack = new Stack<IEnumerator>(32);
        static List<EditorApplication.CallbackFunction> kIEnumeratorProcessingInProgress = new List<EditorApplication.CallbackFunction>(kParallelCoroutineCount);
        static Queue<EditorApplication.CallbackFunction> kIEnumeratorProcessingQueue = new Queue<EditorApplication.CallbackFunction>(32);

        internal AnalysisCoroutine(IEnumerator routine, object owner, Action<long> elapsedTimeDelegate, bool async = true)
        {
            if (routine == null)
                throw new ArgumentNullException("Argument 'routine' must be non-null");

            m_Processor = new YieldProcessor();
            m_Owner = new WeakReference(owner);
            m_Routine = routine;
            m_Status = Status.Running;
            m_ElapsedTimeDelegate = elapsedTimeDelegate;

            Enqueue(async);
        }

        private void Enqueue(bool async)
        {
            if (async && kIEnumeratorProcessingInProgress.Count < kParallelCoroutineCount)
            {
                kIEnumeratorProcessingInProgress.Add(MoveNext);
                EditorApplication.update += MoveNext;
            }
            else
            {
                kIEnumeratorProcessingQueue.Enqueue(MoveNext);
            }
        }

        private void Dequeue()
        {
            EditorApplication.update -= MoveNext;
            kIEnumeratorProcessingInProgress.Remove(MoveNext);

            if (kIEnumeratorProcessingQueue.TryDequeue(out var nextFunc))
            {
                kIEnumeratorProcessingInProgress.Add(nextFunc);
                EditorApplication.update += nextFunc;
            }
        }

        internal static void ExecuteSynchronously(IEnumerator routine, object owner)
        {
            var coroutine = new AnalysisCoroutine(routine, owner, null, false);
            while (kIEnumeratorProcessingQueue.TryPeek(out var nextFunc) && nextFunc == coroutine.MoveNext)
                nextFunc.Invoke();
        }

        internal static bool ForceMoveNext()
        {
            if (kIEnumeratorProcessingInProgress.Count > 0)
            {
                kIEnumeratorProcessingInProgress[0].Invoke();
                return true;
            }
            return false;
        }

        internal void MoveNext()
        {
            if ((m_Owner != null && !m_Owner.IsAlive) || (m_Status != Status.Running))
            {
                Dequeue();
                return;
            }

            // Run the coroutine until X milliseconds has elapsed
            const int kDuration = 30;
            var startTime = DateTime.UtcNow;
            do
            {
                if (!ProcessIEnumeratorRecursive(m_Routine, out bool skipFrame))
                {
                    m_Status = Status.Done;
                    Dequeue();
                    break;
                }

                if (skipFrame)
                    break;
            }
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < kDuration);

            m_ElapsedTimeDelegate?.Invoke((long)(DateTime.UtcNow - startTime).TotalMilliseconds);
        }

        private bool ProcessIEnumeratorRecursive(IEnumerator enumerator, out bool skipFrame)
        {
            var root = enumerator;
            while (enumerator.Current as IEnumerator != null)
            {
                kIEnumeratorProcessingStack.Push(enumerator);
                enumerator = enumerator.Current as IEnumerator;
            }

            //process leaf
            m_Processor.Set(enumerator.Current);
            var result = m_Processor.MoveNext(enumerator, out skipFrame);

            while (kIEnumeratorProcessingStack.Count > 1)
            {
                if (!result)
                    result = kIEnumeratorProcessingStack.Pop().MoveNext();
                else
                    kIEnumeratorProcessingStack.Clear();
            }

            if (kIEnumeratorProcessingStack.Count > 0 && !result && root == kIEnumeratorProcessingStack.Pop())
            {
                result = root.MoveNext();
            }

            return result;
        }

        internal void Stop()
        {
            m_Owner = null;
            m_Routine = null;
            m_Status = Status.Stopped;
            Dequeue();
        }
    }
}
