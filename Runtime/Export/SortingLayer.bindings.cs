// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/BaseClasses/TagManager.h")]
    public struct SortingLayer
    {
        private int m_Id;

        public int id { get { return m_Id; } }

        public string name { get { return SortingLayer.IDToName(m_Id); } }

        public int value { get { return SortingLayer.GetLayerValueFromID(m_Id); } }

        public static SortingLayer[] layers
        {
            get
            {
                int[] ids = GetSortingLayerIDsInternal();
                SortingLayer[] layers = new SortingLayer[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    layers[i].m_Id = ids[i];
                }
                return layers;
            }
        }

        [FreeFunction("GetTagManager().GetSortingLayerIDs")]
        extern private static int[] GetSortingLayerIDsInternal();

        // Returns the final sorting value for the layer.
        [FreeFunction("GetTagManager().GetSortingLayerValueFromUniqueID")]
        public extern static int GetLayerValueFromID(int id);

        // Returns the final sorting value for the layer.
        [FreeFunction("GetTagManager().GetSortingLayerValueFromName")]
        public extern static int GetLayerValueFromName(string name);

        // Returns the unique id of the layer with name.
        [FreeFunction("GetTagManager().GetSortingLayerUniqueIDFromName")]
        public extern static int NameToID(string name);

        // Returns the name given the layer's id.
        [FreeFunction("GetTagManager().GetSortingLayerNameFromUniqueID")]
        public extern static string IDToName(int id);

        // Returns true if an id is valid layer id.
        [FreeFunction("GetTagManager().IsSortingLayerUniqueIDValid")]
        public extern static bool IsValid(int id);
    }
}
