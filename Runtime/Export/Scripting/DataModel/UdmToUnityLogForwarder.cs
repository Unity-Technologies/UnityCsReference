// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.DataModel;

internal class UdmToUnityLogForwarder
{
    internal UdmToUnityLogForwarder(UdmLogCapture logCapture)
    {
        LogCapture = logCapture;
    }

    internal void LogEachMessage(ConstructedUnityObjectSet objects)
    {
        var objectIdToUnityObjectMap = new Dictionary<UdmObjectId, UnityEngine.Object>();
        for (int i = 0; i < objects.allObjectIds.Length; i++)
        {
            objectIdToUnityObjectMap[objects.allObjectIds[i]] = objects.allInstances[i];
        }

        foreach (var udmLogType in UdmLogCapture.LogTypesDescending)
        {
            var unityLogType = UdmLogTypeToUnityLogType(udmLogType);
            foreach (var entry in LogCapture.GetEntriesByType(udmLogType))
            {
                if (objectIdToUnityObjectMap.TryGetValue(entry.ObjectId, out var obj))
                {
                    Debug.unityLogger.Log(unityLogType, (object)entry.Message, obj);
                }
                else
                {
                    Debug.unityLogger.Log(unityLogType, entry.Message);
                }
            }
        }
    }

    private static LogType UdmLogTypeToUnityLogType(UdmLogType udmLogType)
    {
        return udmLogType switch
        {
            UdmLogType.Info => LogType.Log,
            UdmLogType.Warning => LogType.Warning,
            UdmLogType.Error => LogType.Error,
            _ => LogType.Log
        };
    }

    private readonly UdmLogCapture LogCapture;
}
