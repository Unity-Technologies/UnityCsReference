using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Class which contains information about every element contained within the text object.
    /// </summary>
    class TextInfo
    {
        public int materialCount;

        public List<TextMeshInfo> meshInfos;

        public bool isDirty;

        // Default Constructor
        public TextInfo()
        {
            meshInfos = new List<TextMeshInfo>();
            materialCount = 0;
            isDirty = true;
        }
    }
}
