// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine
{
    [System.Obsolete("LightmappingMode has been deprecated. Use LightmapBakeType instead (UnityUpgradable) -> LightmapBakeType", true)]
    public enum LightmappingMode
    {
        [System.Obsolete("LightmappingMode.Realtime has been deprecated. Use LightmapBakeType.Realtime instead (UnityUpgradable) -> LightmapBakeType.Realtime", true)]
        Realtime = 4,
        [System.Obsolete("LightmappingMode.Baked has been deprecated. Use LightmapBakeType.Baked instead (UnityUpgradable) -> LightmapBakeType.Baked", true)]
        Baked = 2,
        [System.Obsolete("LightmappingMode.Mixed has been deprecated. Use LightmapBakeType.Mixed instead (UnityUpgradable) -> LightmapBakeType.Mixed", true)]
        Mixed = 1
    }

    public partial class Light
    {
        [System.Obsolete("Light.lightmappingMode has been deprecated. Use Light.lightmapBakeType instead (UnityUpgradable) -> lightmapBakeType", true)]
        public LightmappingMode lightmappingMode
        {
            get { return LightmappingMode.Realtime; }
            set {}
        }
    }
}

