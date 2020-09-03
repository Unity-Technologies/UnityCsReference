// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;

namespace UnityEditorInternal
{
    internal abstract class BaseUnityLinkerPlatformProvider
    {
        protected readonly BuildTarget m_Target;

        public BaseUnityLinkerPlatformProvider(BuildTarget target)
        {
            this.m_Target = target;
        }

        public abstract string Platform { get; }

        public virtual string Architecture => null;

        public virtual bool AllowOutputToBeMadePlatformDependent => true;

        public virtual bool AllowOutputToBeMadeArchitectureDependent
        {
            get
            {
                // For now we are not leveraging this capability but I don't want to remove the plumbing to use it
                // in case we ever want to take advantage of it
                return false;
            }
        }

        public virtual bool supportsEngineStripping
        {
            get { return BuildPipeline.IsFeatureSupported("ENABLE_ENGINE_CODE_STRIPPING", m_Target); }
        }

        public virtual string modulesAssetFile
        {
            get { return Path.Combine(BuildPipeline.GetPlaybackEngineDirectory(EditorUserBuildSettings.activeBuildTarget, 0), "modules.asset"); }
        }
    }
}
