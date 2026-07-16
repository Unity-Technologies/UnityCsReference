// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class ContainerWindowProxy
    {
        private readonly ContainerWindow m_BackingObject;
        internal ContainerWindow BackingObject => m_BackingObject;

        public ContainerWindowProxy(ContainerWindow backingObject)
        {
            m_BackingObject = backingObject;
            // Doing after the cast so that we cover both NULL and UnityNULL
            Debug.Assert(m_BackingObject != null);
        }

        public void Close() => m_BackingObject.Close();
        public EntityId GetEntityId() => m_BackingObject.GetEntityId();

        public static void SetMppmCanCloseCallback(Func<bool> mppmCanCloseCallback)
        {
            ContainerWindow.SetMppmCanCloseCallback(mppmCanCloseCallback);
        }

        public string title
        {
            set => m_BackingObject.title = value;
        }

        public static ContainerWindowProxy FromInstanceID(EntityId entityId)
        {
            var obj = EditorUtility.EntityIdToObject(entityId) as ContainerWindow;
            return obj == null ? null : new ContainerWindowProxy(obj);
        }

        public static implicit operator bool(ContainerWindowProxy proxy) => proxy?.m_BackingObject;
    }
}
