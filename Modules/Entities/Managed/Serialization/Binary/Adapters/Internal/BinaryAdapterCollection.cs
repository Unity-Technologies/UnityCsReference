// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace Unity.Entities.Serialization.Binary
{
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    unsafe struct BinaryAdapterCollection
    {
        /// <summary>
        /// Enumerates a set of adapters in a pre-defined order. This will iterate user, global and finally internal.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
        public struct Enumerator
        {
            enum State
            {
                User,
                Global,
                Internal,
                End
            }
            
            readonly List<IBinaryAdapter> m_User;
            readonly List<IBinaryAdapter> m_Global;
            readonly IBinaryAdapter m_Internal;
            
            IBinaryAdapter m_Current;
            State m_State;
            int m_Index;

            public Enumerator(List<IBinaryAdapter> user, List<IBinaryAdapter> global, IBinaryAdapter @internal)
            {
                m_User = user;
                m_Global = global;
                m_Internal = @internal;
                m_Current = null;
                m_State = null != user ? State.User : null != global ? State.Global : State.Internal;
                m_Index = -1;
            }

            public IBinaryAdapter Current => m_Current;

            public bool MoveNext()
            {
                for (;;)
                {
                    m_Index++;
                    
                    switch (m_State)
                    {
                        case State.User:
                            if (m_Index < m_User.Count)
                            {
                                m_Current = m_User[m_Index];
                                return true;
                            }
                            m_State = State.Global;
                            m_Index = -1;
                            break;
                        case State.Global:
                            if (m_Index < m_Global.Count)
                            {
                                m_Current = m_Global[m_Index];
                                return true;
                            }
                            m_State = State.Internal;
                            m_Index = -1;
                            break;
                        case State.Internal:
                            m_Current = m_Internal;
                            m_State = State.End;
                            m_Index = -1;
                            return true;
                        case State.End:
                            m_Index = -1;
                            return false;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
        
        public List<IBinaryAdapter> UserDefined;
        public List<IBinaryAdapter> Global;
        public IBinaryAdapter Internal;

        public Enumerator GetEnumerator() 
            => new Enumerator(UserDefined, Global, Internal);
    }
}
