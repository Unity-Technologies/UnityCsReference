// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Threading;
using UnityEngine.Assertions;
using UnityEngine.LightTransport;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
using static UnityEditor.LightBaking.LightBaker;

namespace UnityEditor.LightBaking
{
    internal static class UnityComputeBake
    {
        [RequiredByNativeCode]
        internal static bool BakeWithDummyProgress(string bakeInputPath, string lightmapRequestsPath, string lightProbeRequestsPath, string bakeOutputFolderPath)
        {
            using BakeProgressState dummyProgressState = new();
            bool success = Bake(bakeInputPath, lightmapRequestsPath, lightProbeRequestsPath, bakeOutputFolderPath, dummyProgressState);

            return success;
        }

        [RequiredByNativeCode]
        internal static bool Bake(string bakeInputPath, string lightmapRequestsPath, string lightProbeRequestsPath, string bakeOutputFolderPath, BakeProgressState progressState)
        {
            Type strangler = Type.GetType("UnityEditor.PathTracing.LightBakerBridge.LightBakerStrangler, Unity.PathTracing.Editor");
            if (strangler == null)
                return false;

            MethodInfo bakeMethod = strangler.GetMethod("Bake", BindingFlags.Static | BindingFlags.NonPublic);
            if (bakeMethod == null)
                return false;

            var bakeFunc = (Func<string, string, string, string, BakeProgressState, bool>)Delegate.CreateDelegate(typeof(Func<string, string, string, string, BakeProgressState, bool>), bakeMethod);
            return bakeFunc(bakeInputPath, lightmapRequestsPath, lightProbeRequestsPath, bakeOutputFolderPath, progressState);
        }

        internal delegate bool BakeAction(
            string bakeInputPath,
            string lightmapRequestsPath,
            string lightProbeRequestsPath,
            string bakeOutputFolderPath,
            int progressPort);

        [RequiredByNativeCode]
        internal static bool BakeWithWorkerProcess(string bakeInputPath, string lightmapRequestsPath, string lightProbeRequestsPath, string bakeOutputFolderPath, int progressPort, out bool typeMissing)
        {
            typeMissing = false;

            Type importer = Type.GetType("UnityEditor.PathTracing.LightBakerBridge.LightBakerWorkerProcessImporter, Unity.PathTracing.Editor");
            if (importer == null)
            {
                typeMissing = true;
                return false;
            }

            MethodInfo bakeMethod = importer.GetMethod("BakeWithWorkerProcess", BindingFlags.Static | BindingFlags.NonPublic);
            if (bakeMethod == null)
            {
                typeMissing = true;
                return false;
            }

            BakeAction action = (BakeAction)bakeMethod.CreateDelegate(typeof(BakeAction));

            return action(bakeInputPath, lightmapRequestsPath, lightProbeRequestsPath, bakeOutputFolderPath, progressPort);
        }
    }
}
