// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;

[assembly: InternalsVisibleTo("Unity.VT.Subscenes.Editor")]

namespace UnityEngine.Rendering
{
    namespace VirtualTexturingEditor
    {
        [NativeHeader("Modules/VirtualTexturingEditor/ScriptBindings/VirtualTexturingEditor.bindings.h")]
        [StaticAccessor("VirtualTexturingEditor::Building", StaticAccessorType.DoubleColon)]
        internal static class Building
        {
            extern internal static bool IsPlatformSupportedForPlayer(BuildTarget platform);
            extern internal static bool IsRenderAPISupported(GraphicsDeviceType type, BuildTarget platform, bool checkEditor);

            internal interface IBuildStacks
            {
                IList<Material> OnIncludeAdditionalStacksInPlayer();
                bool OnPreparedStacksInAssetBundle(string bundleName, string variantName, IList<Material> stackOwners);
            }
        }

        internal static partial class StackBuildingFeedbackInterfaces
        {
            [AutoStaticsCleanupOnCodeReload]
            static List<Building.IBuildStacks> stackBuildingFeedbackCallbacks;

            [RequiredByNativeCode]
            internal static void InitCallbacks()
            {
                CleanupCallbacks();

                stackBuildingFeedbackCallbacks = new List<Building.IBuildStacks>();

                foreach (Type t in TypeCache.GetTypesDerivedFrom<Building.IBuildStacks>())
                {
                    if (t.IsAbstract || t.IsInterface)
                        continue;

                    object o = Activator.CreateInstance(t);
                    stackBuildingFeedbackCallbacks.Add(o as Building.IBuildStacks);
                }
            }

            [RequiredByNativeCode]
            internal static void CleanupCallbacks()
            {
                stackBuildingFeedbackCallbacks = null;
            }

            [RequiredByNativeCode]
            internal static bool OnPreparedStacksInAssetBundle(string assetBundleName, string variantName, IList<Material> stackOwners)
            {
                bool implemented = false;

                if (stackBuildingFeedbackCallbacks != null)
                {
                    foreach (Building.IBuildStacks cb in stackBuildingFeedbackCallbacks)
                    {
                        try
                        {
                            if (!cb.OnPreparedStacksInAssetBundle(assetBundleName, variantName, stackOwners))
                            {
                                return false;
                            }
                            implemented = true;
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }

                return implemented;
            }

            // Returns EntityIds: native (VirtualTexturingEditorManager.cpp) keeps the materials as PPtrs.
            [RequiredByNativeCode]
            internal static EntityId[] OnIncludeAdditionalStacksInPlayer()
            {
                List<EntityId> forceIncludes = new List<EntityId>();

                if (stackBuildingFeedbackCallbacks != null)
                {
                    foreach (Building.IBuildStacks cb in stackBuildingFeedbackCallbacks)
                    {
                        try
                        {
                            IList<Material> list = cb.OnIncludeAdditionalStacksInPlayer();
                            forceIncludes.Capacity = Math.Max(forceIncludes.Capacity, forceIncludes.Count + list.Count);
                            foreach (var material in list)
                            {
                                if (material != null)
                                    forceIncludes.Add(material.GetEntityId());
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }

                return forceIncludes.ToArray();
            }
        }
    }
}
