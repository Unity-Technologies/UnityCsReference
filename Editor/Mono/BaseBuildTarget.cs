// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor;

internal abstract class BaseBuildTarget : IBuildTarget
{
    public virtual bool CanBuildOnCurrentHostPlatform => false;
    public virtual string DisplayName => TargetName;
    public abstract RuntimePlatform RuntimePlatform { get; }
    public abstract string TargetName { get; }
    public abstract int GetLegacyId { get; }

    public virtual IBuildPlatformProperties BuildPlatformProperties => Properties as IBuildPlatformProperties;
    public virtual IGraphicsPlatformProperties GraphicsPlatformProperties => Properties as IGraphicsPlatformProperties;
    public virtual IPlayerConnectionPlatformProperties PlayerConnectionPlatformProperties => Properties as IPlayerConnectionPlatformProperties;
    protected virtual IPlatformProperties Properties => null;

    public bool TryGetProperties<T>(out T properties) where T: IPlatformProperties
    {
        if (Properties is T)
        {
            properties = (T)Properties;
            return true;
        }
        properties = default(T);
        return false;
    }
}
