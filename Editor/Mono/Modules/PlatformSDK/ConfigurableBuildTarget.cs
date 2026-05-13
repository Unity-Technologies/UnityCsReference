// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Modules;

class ConfigurableBuildTarget : IBuildTarget
{
    public GUID Guid { get; protected set; }
    public string DisplayName { get; protected set; }
    public string TargetName { get; protected set; }

    protected virtual IBuildTarget m_BuildTarget { get; set; }
    protected string m_IconName;
    Dictionary<Type, IPlatformProperties> m_PlatformPropertiesOverrides = new();

    public RuntimePlatform RuntimePlatform => m_BuildTarget.RuntimePlatform;
    public string RootSystemType => m_BuildTarget.RootSystemType;
    public int GetLegacyId => m_BuildTarget.GetLegacyId;

    public IBuildPlatformProperties BuildPlatformProperties
    {
        get
        {
            if (TryGetPropertiesOverride(out IBuildPlatformProperties properties))
                return properties;
            else
                return m_BuildTarget.BuildPlatformProperties;
        }
    }

    public IGraphicsPlatformProperties GraphicsPlatformProperties
    {
        get
        {
            if (TryGetPropertiesOverride(out IGraphicsPlatformProperties properties))
                return properties;
            else
                return m_BuildTarget.GraphicsPlatformProperties;
        }
    }

    public IPlayerConnectionPlatformProperties PlayerConnectionPlatformProperties
    {
        get
        {
            if (TryGetPropertiesOverride(out IPlayerConnectionPlatformProperties properties))
                return properties;
            else
                return m_BuildTarget.PlayerConnectionPlatformProperties;
        }
    }

    public IIconPlatformProperties IconPlatformProperties
    {
        get
        {
            if (TryGetPropertiesOverride(out IIconPlatformProperties properties))
                return properties;
            else
                return m_BuildTarget.IconPlatformProperties;
        }
    }

    public IUIPlatformProperties UIPlatformProperties
    {
        get
        {
            if (TryGetPropertiesOverride(out IUIPlatformProperties properties))
                return properties;
            else
                return m_BuildTarget.UIPlatformProperties;
        }
    }

    public IAudioPlatformProperties AudioPlatformProperties
    {
        get
        {
            if (TryGetPropertiesOverride(out IAudioPlatformProperties properties))
                return properties;
            else
                return m_BuildTarget.AudioPlatformProperties;
        }
    }

    public IVRPlatformProperties VRPlatformProperties
    {
        get
        {
            if (TryGetPropertiesOverride(out IVRPlatformProperties properties))
                return properties;
            else
                return m_BuildTarget.VRPlatformProperties;
        }
    }

    public ISubtargetPlatformProperties TextureSubtargetPlatformProperties
    {
        get
        {
            if (TryGetPropertiesOverride(out ISubtargetPlatformProperties properties))
                return properties;
            else
                return m_BuildTarget.TextureSubtargetPlatformProperties;
        }
    }

    public IScriptingPlatformProperties ScriptingPlatformProperties
    {
        get
        {
            if (TryGetPropertiesOverride(out IScriptingPlatformProperties properties))
                return properties;
            else
                return m_BuildTarget.ScriptingPlatformProperties;
        }
    }

    public bool TryGetProperties<T>(out T properties) where T : IPlatformProperties
    {
        if (TryGetPropertiesOverride(out properties))
            return true;
        else
            return m_BuildTarget.TryGetProperties(out properties);
    }

    public ConfigurableBuildTarget With<T>(T properties) where T : IPlatformProperties
    {
        m_PlatformPropertiesOverrides.Add(typeof(T), properties);
        return this;
    }

    bool TryGetPropertiesOverride<T>(out T properties) where T : IPlatformProperties
    {
        if (m_PlatformPropertiesOverrides.TryGetValue(typeof(T), out var propertiesObject) 
            && propertiesObject is T typedProperties)
        {
            properties = typedProperties;
            return true;
        }

        properties = default;
        return false;
    }
}
