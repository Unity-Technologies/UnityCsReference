// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.WSA;


namespace UnityEngine.XR.WSA.Persistence
{
    [MovedFrom("UnityEngine.VR.WSA.Persistence")]
    public partial class WorldAnchorStore
    {
        public delegate void GetAsyncDelegate(WorldAnchorStore store);

        public static void GetAsync(GetAsyncDelegate onCompleted)
        {
        }

        public bool Save(string id, WorldAnchor anchor)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id must not be null or empty", "id");
            }

            if (anchor == null)
            {
                throw new ArgumentNullException("anchor");
            }
            return false;
        }

        public WorldAnchor Load(string id, GameObject go)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id must not be null or empty", "id");
            }

            if (go == null)
            {
                throw new ArgumentNullException("anchor");
            }
            return null;
        }

        public bool Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id must not be null or empty", "id");
            }
            return false;
        }

        public void Clear()
        {
        }

        public int anchorCount
        {
            get
            {
                return 0;
            }
        }

        public int GetAllIds(string[] ids)
        {
            if (ids == null)
                throw new ArgumentNullException("ids");

            return 0;
        }

        public string[] GetAllIds()
        {
            return new string[0];
        }

        private WorldAnchorStore(IntPtr nativePtr)
        {
        }

        [RequiredByNativeCode]
        private static void InvokeGetAsyncDelegate(GetAsyncDelegate handler, IntPtr nativePtr)
        {
            s_Instance = new WorldAnchorStore(nativePtr);
            handler(s_Instance);
        }

        private static WorldAnchorStore s_Instance = null;

    }
}

namespace UnityEngine.XR.WSA.Sharing
{
    [MovedFrom("UnityEngine.VR.WSA.Sharing")]
    public enum SerializationCompletionReason
    {
        Succeeded = 0,
        NotSupported = 1,
        AccessDenied = 2,
        UnknownError = 3
    }

    [MovedFrom("UnityEngine.VR.WSA.Sharing")]
    public partial class WorldAnchorTransferBatch : IDisposable
    {
        public delegate void SerializationDataAvailableDelegate(byte[] data);
        public delegate void SerializationCompleteDelegate(SerializationCompletionReason completionReason);
        public delegate void DeserializationCompleteDelegate(SerializationCompletionReason completionReason, WorldAnchorTransferBatch deserializedTransferBatch);

        public static void ExportAsync(WorldAnchorTransferBatch transferBatch, SerializationDataAvailableDelegate onDataAvailable, SerializationCompleteDelegate onCompleted)
        {
            if (transferBatch == null)
            {
                throw new ArgumentNullException("transferBatch");
            }
            if (onDataAvailable == null)
            {
                throw new ArgumentNullException("onDataAvailable");
            }
            if (onCompleted == null)
            {
                throw new ArgumentNullException("onCompleted");
            }

            onCompleted(SerializationCompletionReason.NotSupported);
        }

        public static void ImportAsync(byte[] serializedData, DeserializationCompleteDelegate onComplete)
        {
            ImportAsync(serializedData, 0, serializedData.Length, onComplete);
        }

        public static void ImportAsync(byte[] serializedData, int offset, int length, DeserializationCompleteDelegate onComplete)
        {
            if (serializedData == null)
            {
                throw new ArgumentNullException("serializedData");
            }
            if (serializedData.Length < 1)
            {
                throw new ArgumentException("serializedData is empty!", "serializedData");
            }
            if ((offset + length) > serializedData.Length)
            {
                throw new ArgumentException("offset + length is greater that serializedData.Length!");
            }
            if (onComplete == null)
            {
                throw new ArgumentNullException("onComplete");
            }

            onComplete(SerializationCompletionReason.NotSupported, new WorldAnchorTransferBatch());
        }

        public bool AddWorldAnchor(string id, WorldAnchor anchor)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id is null or empty!", "id");
            }
            if (anchor == null)
            {
                throw new ArgumentNullException("anchor");
            }
            return false;
        }

        public int anchorCount
        {
            get
            {
                return 0;
            }
        }

        public int GetAllIds(string[] ids)
        {
            if (ids == null)
                throw new ArgumentNullException("ids");

            return 0;
        }

        public string[] GetAllIds()
        {
            return new string[0];
        }

        public WorldAnchor LockObject(string id, GameObject go)
        {
            return null;
        }

        public WorldAnchorTransferBatch()
        {
        }

        ~WorldAnchorTransferBatch()
        {
        }

        public void Dispose()
        {
        }

        private WorldAnchorTransferBatch(IntPtr nativePtr)
        {
        }

        [RequiredByNativeCode]
        private static void InvokeWorldAnchorSerializationDataAvailableDelegate(SerializationDataAvailableDelegate onSerializationDataAvailable, byte[] data)
        {
            onSerializationDataAvailable(data);
        }

        [RequiredByNativeCode]
        private static void InvokeWorldAnchorSerializationCompleteDelegate(SerializationCompleteDelegate onSerializationComplete, SerializationCompletionReason completionReason)
        {
            onSerializationComplete(completionReason);
        }

        [RequiredByNativeCode]
        private static void InvokeWorldAnchorDeserializationCompleteDelegate(DeserializationCompleteDelegate onDeserializationComplete, SerializationCompletionReason completionReason, IntPtr nativePtr)
        {
            WorldAnchorTransferBatch watb = new WorldAnchorTransferBatch(nativePtr);
            onDeserializationComplete(completionReason, watb);
        }

    }
}

