// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;

namespace UnityEditor.Search
{
    enum PrefabFilter
    {
        [System.ComponentModel.Description("Search prefab roots")]
        Root,
        [System.ComponentModel.Description("Search objects that are part of a prefab instance")]
        Instance,
        [System.ComponentModel.Description("Search top-level prefab root instances")]
        Top,
        [System.ComponentModel.Description("Search prefab objects that are not part of an asset")]
        NonAsset,
        [System.ComponentModel.Description("Search prefab objects that are part of an asset")]
        Asset,
        [System.ComponentModel.Description("Search prefabs")]
        Any,
        [System.ComponentModel.Description("Search prefab objects that are part of a model")]
        Model,
        [System.ComponentModel.Description("Search regular prefab objects")]
        Regular,
        [System.ComponentModel.Description("Search variant prefab objects")]
        Variant,
        [System.ComponentModel.Description("Search modified prefab assets")]
        Modified,
        [System.ComponentModel.Description("Search modified prefab instances")]
        Altered,
        [System.ComponentModel.Description("Search base prefabs")] //TODO: This isn't used?
        Base
    }
}
