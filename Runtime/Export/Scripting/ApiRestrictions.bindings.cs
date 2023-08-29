// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Scripting/ApiRestrictions.h")]
    [UsedByNativeCode]
    [ExtensionOfNativeClass]
    [StaticAccessor("GetApiRestrictions()", StaticAccessorType.Arrow)]
    internal class ApiRestrictions
    {
        internal enum GlobalRestrictions
        {
            OBJECT_DESTROYIMMEDIATE = 0,
            OBJECT_SENDMESSAGE = 1,
            OBJECT_RENDERING = 2,
            GLOBALCOUNT
        }

        internal enum ContextRestrictions
        {
            RENDERERSCENE_ADDREMOVE = 0,
            OBJECT_ADDCOMPONENTTRANSFORM = 1,
            CONTEXTCOUNT
        }

        extern static internal void PushDisableApiInternal(ContextRestrictions contextApi, Object context,
            GlobalRestrictions globalApi);

        extern static internal void PopDisableApiInternal(ContextRestrictions contextApi, Object context,
            GlobalRestrictions globalApi);

        extern static internal bool TryApiInternal(ContextRestrictions contextApi, Object context,
            GlobalRestrictions globalApi, bool allowErrorLogging);

        static internal void PushDisableApi(ContextRestrictions api, Object owner)
        {
            PushDisableApiInternal(api, owner, GlobalRestrictions.GLOBALCOUNT);
        }

        static internal void PushDisableApi(GlobalRestrictions api)
        {
            PushDisableApiInternal(ContextRestrictions.CONTEXTCOUNT, null, api);
        }

        static internal void PopDisableApi(ContextRestrictions api, Object context)
        {
            PopDisableApiInternal(api, context, GlobalRestrictions.GLOBALCOUNT);
        }

        static internal void PopDisableApi(GlobalRestrictions api)
        {
            PopDisableApiInternal(ContextRestrictions.CONTEXTCOUNT, null, api);
        }

        static internal bool TryApi(ContextRestrictions api, Object context, bool allowErrorLogging = true)
        {
            return TryApiInternal(api, context, GlobalRestrictions.GLOBALCOUNT, allowErrorLogging);
        }

        static internal bool TryApi(GlobalRestrictions api, bool allowErrorLogging = true)
        {
            return TryApiInternal(ContextRestrictions.CONTEXTCOUNT, null, api, allowErrorLogging);
        }
    }

    [NativeHeader("Runtime/Scripting/ApiRestrictions.h")]
    internal readonly struct DisableApiScope : IDisposable
    {
        public DisableApiScope(ApiRestrictions.ContextRestrictions api, Object context)
        {
            m_ContextApi = api;
            m_Context = context;
            ApiRestrictions.PushDisableApi(api, context);

            m_GlobalApi = ApiRestrictions.GlobalRestrictions.GLOBALCOUNT;
        }

        public DisableApiScope(ApiRestrictions.GlobalRestrictions api)
        {
            m_GlobalApi = api;
            m_Context = null;
            ApiRestrictions.PushDisableApi(api);

            m_ContextApi = ApiRestrictions.ContextRestrictions.CONTEXTCOUNT;
        }

        public void Dispose()
        {
            if(m_Context != null)
            {
                ApiRestrictions.PopDisableApi(m_ContextApi, m_Context);
            }
            else
            {
                ApiRestrictions.PopDisableApi(m_GlobalApi);
            }
        }

        private readonly ApiRestrictions.ContextRestrictions m_ContextApi;
        private readonly ApiRestrictions.GlobalRestrictions m_GlobalApi;
        private readonly Object m_Context;
    }

    [NativeHeader("Runtime/Scripting/ApiRestrictions.h")]
    internal readonly struct EnableApiScope : IDisposable
    {
        public EnableApiScope(ApiRestrictions.ContextRestrictions api, Object context)
        {
            m_ContextApi = api;
            m_Context = context;
            ApiRestrictions.PopDisableApi(api, context);

            m_GlobalApi = ApiRestrictions.GlobalRestrictions.GLOBALCOUNT;
        }

        public EnableApiScope(ApiRestrictions.GlobalRestrictions api)
        {
            m_GlobalApi = api;
            m_Context = null;
            ApiRestrictions.PopDisableApi(api);

            m_ContextApi = ApiRestrictions.ContextRestrictions.CONTEXTCOUNT;
        }

        public void Dispose()
        {
            if (m_Context != null)
            {
                ApiRestrictions.PushDisableApi(m_ContextApi, m_Context);
            }
            else
            {
                ApiRestrictions.PushDisableApi(m_GlobalApi);
            }
        }

        private readonly ApiRestrictions.ContextRestrictions m_ContextApi;
        private readonly ApiRestrictions.GlobalRestrictions m_GlobalApi;
        private readonly Object m_Context;
    }
}
