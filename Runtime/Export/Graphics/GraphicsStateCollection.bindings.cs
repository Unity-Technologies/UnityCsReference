// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using Unity.Jobs;

namespace UnityEngine.Experimental.Rendering
{
    [NativeHeader("Runtime/Graphics/GraphicsStateCollection.h")]
    public sealed partial class GraphicsStateCollection : Object
    {
        extern public bool BeginTrace();
        extern public void EndTrace();
        extern public bool isTracing {[NativeName("IsTracing")] get; }
        extern public int version {[NativeName("GetVersion")] get; [NativeName("SetVersion")] set;}
        extern public GraphicsDeviceType graphicsDeviceType {[NativeName("GetDeviceRenderer")] get; [NativeName("SetDeviceRenderer")] set;}
        extern public RuntimePlatform runtimePlatform {[NativeName("GetRuntimePlatform")] get; [NativeName("SetRuntimePlatform")] set;}
        extern public string qualityLevelName {[NativeName("GetQualityLevelName")] get; [NativeName("SetQualityLevelName")] set;}
        extern public bool LoadFromFile(string filePath);
        extern public bool SaveToFile(string filePath);
        extern public bool SendToEditor(string fileName);
        [NativeName("Warmup")] extern public JobHandle WarmUp(JobHandle dependency = new JobHandle());
        [NativeName("WarmupProgressively")] extern public JobHandle WarmUpProgressively(int count, JobHandle dependency = new JobHandle());
        extern public int totalGraphicsStateCount { get; }
        extern public int completedWarmupCount { get; }
        extern public bool isWarmedUp {[NativeName("IsWarmedUp")] get; }

        extern private void GetVariants([Out] ShaderVariant[] results);
        public void GetVariants(List<ShaderVariant> results)
        {
            if (results == null)
                throw new ArgumentNullException("The result shader variant list cannot be null.");
            results.Clear();
            NoAllocHelpers.EnsureListElemCount(results, variantCount);
            GetVariants(NoAllocHelpers.ExtractArrayFromList(results));
        }
        extern private void GetGraphicsStatesForVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords, [Out] GraphicsState[] results);
        public void GetGraphicsStatesForVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords, List<GraphicsState> results)
        {
            if (results == null)
                throw new ArgumentNullException("The result graphics state list cannot be null.");
            results.Clear();
            NoAllocHelpers.EnsureListElemCount(results, GetGraphicsStateCountForVariant(shader, passId, keywords));
            GetGraphicsStatesForVariant(shader, passId, keywords, NoAllocHelpers.ExtractArrayFromList(results));
        }
        extern public int variantCount { get; }
        extern public int GetGraphicsStateCountForVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords); 

        extern public bool AddVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords);
        extern public bool RemoveVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords);
        extern public bool ContainsVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords);
        extern public void ClearVariants();
        extern public bool AddGraphicsStateForVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords, GraphicsState setup);
        extern public bool RemoveGraphicsStatesForVariant(Shader shader, PassIdentifier passId, LocalKeyword[] keywords);

        [NativeName("CreateFromScript")] extern private static void Internal_Create([Writable] GraphicsStateCollection gsc);
    }
}
