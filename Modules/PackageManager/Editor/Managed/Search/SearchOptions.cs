// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    [NativeType(IntermediateScriptingStructName = "PackageManager_SearchOptions")]
    internal sealed class SearchOptions
    {
        [SerializeField]
        [NativeName("query")]
        private string m_Query = "";

        [SerializeField]
        [NativeName("type")]
        private string m_Type = "";

        [SerializeField]
        [NativeName("entitlement")]
        private SearchEntitlement m_Entitlement;

        [SerializeField]
        [NativeName("offset")]
        private long m_Offset = -1;

        [SerializeField]
        [NativeName("limit")]
        private long m_Limit = -1;

        [SerializeField]
        [NativeName("orderBy")]
        private SearchOrderBy m_OrderBy;

        [SerializeField]
        [NativeName("order")]
        private SearchOrder m_Order;

        [SerializeField]
        [NativeName("registry")]
        private string m_Registry = "";

        internal SearchOptions(string query = "",
                               string type = "",
                               SearchEntitlement entitlement = default(SearchEntitlement),
                               long offset = -1,
                               long limit = -1,
                               SearchOrderBy orderBy = default(SearchOrderBy),
                               SearchOrder order = default(SearchOrder),
                               string registry = "")
        {
            m_Query = query;
            m_Type = type;
            m_Entitlement = entitlement;
            m_Offset = offset;
            m_Limit = limit;
            m_OrderBy = orderBy;
            m_Order = order;
            m_Registry = registry;
        }

        public string query { get { return m_Query; } }
        public string type { get { return m_Type; } }
        public SearchEntitlement entitlement { get { return m_Entitlement; } }
        public long offset { get { return m_Offset; } }
        public long limit { get { return m_Limit; } }
        public SearchOrderBy orderBy { get { return m_OrderBy; } }
        public SearchOrder order { get { return m_Order; } }
        public string registry { get { return m_Registry; } }
    }
}
