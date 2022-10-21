// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    [NativeHeader("Modules/Director/ScriptBindings/PlayableSystems.bindings.h")]
    [StaticAccessor("PlayableSystemsBindings", StaticAccessorType.DoubleColon)]
    internal static class PlayableSystems
    {
        public delegate void PlayableSystemDelegate(IReadOnlyList<DataPlayableOutput> outputs);

        public enum PlayableSystemStage : ushort
        {
            FixedUpdate,
            FixedUpdatePostPhysics,
            Update,
            AnimationBegin,
            AnimationEnd,
            LateUpdate,
            Render
        }

        public static void RegisterSystemPhaseDelegate<TDataStream>(PlayableSystemStage stage, PlayableSystemDelegate systemDelegate)
            where TDataStream : new()
        {
            RegisterSystemPhaseDelegate(typeof(TDataStream), stage, systemDelegate);
        }

        static void RegisterSystemPhaseDelegate(System.Type streamType, PlayableSystemStage stage, PlayableSystemDelegate systemDelegate)
        {
            int typeIndex = RegisterStreamStage(streamType, (int)stage);
            try
            {
                s_RWLock.EnterWriteLock();
                s_SystemTypes.TryAdd(typeIndex, streamType);
                int combinedId = CombineTypeAndIndex(typeIndex, stage);
                if (!s_Delegates.TryAdd(combinedId, systemDelegate))
                {
                    s_Delegates[combinedId] = systemDelegate;
                }
            }
            finally
            {
                s_RWLock.ExitWriteLock();
            }
        }

        static int CombineTypeAndIndex(int typeIndex, PlayableSystemStage stage)
        {
            return typeIndex << 16 | (int)stage;
        }

        private unsafe class DataPlayableOutputList : IReadOnlyList<DataPlayableOutput>
        {
            public DataPlayableOutputList(PlayableOutputHandle* outputs, int count)
            {
                m_Outputs = outputs;
                m_Count = count;
            }

            public DataPlayableOutput this[int index]
            {
                get
                {
                    if (index >= m_Count)
                        throw new IndexOutOfRangeException($"index {index} is greater than the number of items: {m_Count}");
                    if (index < 0)
                        throw new IndexOutOfRangeException($"index cannot be negative");

                    return new DataPlayableOutput(m_Outputs[index]);
                }
            }

            public int Count => m_Count;

            public IEnumerator<DataPlayableOutput> GetEnumerator()
            {
                return new DataPlayableOutputEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class DataPlayableOutputEnumerator : IEnumerator<DataPlayableOutput>
            {
                public DataPlayableOutputEnumerator(DataPlayableOutputList list)
                {
                    m_List = list;
                    m_Index = -1;
                }
                public DataPlayableOutput Current
                {
                    get
                    {
                        try
                        {
                            return m_List[m_Index];
                        }
                        catch (IndexOutOfRangeException)
                        {
                            throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                        }
                    }
                }

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    m_List = null;
                }

                public bool MoveNext()
                {
                    m_Index++;
                    return m_Index < m_List.Count;
                }

                public void Reset()
                {
                    m_Index = -1;
                }

                DataPlayableOutputList m_List;
                int m_Index;
            }

            PlayableOutputHandle* m_Outputs;
            int m_Count;

        }

        [RequiredByNativeCode]
        private unsafe static bool Internal_CallSystemDelegate(int systemIndex, PlayableSystemStage stage, IntPtr outputsPtr, int numOutputs)
        {
            PlayableOutputHandle* outputs = (PlayableOutputHandle*)outputsPtr;

            int combinedId = CombineTypeAndIndex(systemIndex, stage);


            bool typeFound = false;
            bool systemFound = false;
            PlayableSystemDelegate systemDelegate = null;
            s_RWLock.EnterReadLock();
            typeFound = s_SystemTypes.TryGetValue(systemIndex, out Type systemType);
            if (typeFound)
            {
                systemFound = s_Delegates.TryGetValue(combinedId, out systemDelegate) && systemDelegate != null;
            }
            s_RWLock.ExitReadLock();

            if (!typeFound || !systemFound)
                return false;

            var outputsArgument = new DataPlayableOutputList(outputs, numOutputs);
            systemDelegate(outputsArgument);

            return true;
        }

        [ThreadAndSerializationSafe]
        private extern static int RegisterStreamStage(System.Type streamType, int stage);

        static PlayableSystems()
        {
            s_Delegates = new Dictionary<int, PlayableSystemDelegate>();
            s_SystemTypes = new Dictionary<int, Type>();
            s_RWLock = new ReaderWriterLockSlim();
        }

        static Dictionary<int, Type> s_SystemTypes;
        static Dictionary<int, PlayableSystemDelegate> s_Delegates;
        static ReaderWriterLockSlim s_RWLock;
    }
}
