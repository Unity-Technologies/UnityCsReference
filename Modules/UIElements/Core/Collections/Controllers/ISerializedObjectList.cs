// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;

namespace UnityEngine.UIElements
{
    internal interface ISerializedObjectList : IList
    {
        public void ApplyChanges();
        public void RemoveAt(int index, int listCount);
        public void Move(int srcIndex, int destIndex);
        public int minArraySize { get; }
        public int arraySize { get; set; }
    }
}
