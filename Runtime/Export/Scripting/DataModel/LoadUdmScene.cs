// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.DataModel;
using System;
using Unity.Collections;
using Unity.Scripting.LifecycleManagement;
using Unity.EntitiesLike;

namespace Unity.DataModel
{
    [RequiredByNativeCode]
    [NativeHeader("Runtime/PreloadManager/LoadSceneOperation.h")]
    internal partial struct LoadUdmScene
    {
        internal delegate void UDMMergeAllPrefabsDuringLoad(DocumentModel documentModel, string assetPathBeingLoaded, UdmLogger logger);
        internal delegate void UDMSceneVerificationDuringLoad(DocumentModel documentModel, bool isLoadingPrefab);

        [AutoStaticsCleanupOnCodeReload]
        internal static UDMMergeAllPrefabsDuringLoad mergeAllUDMPrefabsDuringLoad;
        [AutoStaticsCleanupOnCodeReload]
        internal static UDMSceneVerificationDuringLoad verifyUDMSceneDuringLoad;

        [RequiredByNativeCode]
        internal static unsafe void LoadUdmSceneInternalBindings(string assetPath, IntPtr documentModelPtr, bool isLoadingPrefab, IntPtr awakeFromLoadQueuePtr, IntPtr exclusiveEntityTransactionIntPtr)
        {
            var logCapture = new UdmLogCapture();
            using var logCaptureHandler = new UdmLogCaptureHandler(logCapture);

            if (mergeAllUDMPrefabsDuringLoad != null)
                mergeAllUDMPrefabsDuringLoad(documentModelPtr, assetPath, logCaptureHandler.Logger);

            if (verifyUDMSceneDuringLoad != null)
                verifyUDMSceneDuringLoad(documentModelPtr, isLoadingPrefab);

            DocumentModel documentModel = documentModelPtr;
            var exclusiveEntityTransactionPtr = (ExclusiveEntityTransaction*)exclusiveEntityTransactionIntPtr.ToPointer();

            var objects = LoadUdm.CreateLiveObjectsFromDocumentInternal(documentModel, exclusiveEntityTransactionPtr->EntityManager, SerializeInstructionFlags.ReadWriteFromSerializedFile, false).unityObjects;
            LoadUdm.SetAwakeFromLoadQueue(objects.allInstances, awakeFromLoadQueuePtr);

            if (logCapture.Any)
            {
                string message;
                if (isLoadingPrefab)
                    message = $"Problem detected while loading the contents of the Prefab file: '{assetPath}'.\nCheck the following logs for more details.";
                else
                    message = $"Problem detected while opening the Scene file: '{assetPath}'.\nCheck the following logs for more details.";
                // TODO ping main object
                if (logCapture.GetEntriesByType(UdmLogType.Error).Count > 0)
                    Debug.LogError(message);
                else
                    Debug.Log(message);
                var logForwarder = new UdmToUnityLogForwarder(logCapture);
                logForwarder.LogEachMessage(objects); // Log each message so the user can ping the object associated with each message
            }
        }
    }
}
