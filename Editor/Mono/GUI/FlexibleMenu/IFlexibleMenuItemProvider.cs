// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    interface IFlexibleMenuItemProvider
    {
        int Count();
        object GetItem(int index);
        int Add(object obj);
        void Replace(int index, object newPresetObject);
        void Remove(int index);
        object Create();
        void Move(int index, int destIndex, bool insertAfterDestIndex);
        string GetName(int index);
        bool IsModificationAllowed(int index);
        int[] GetSeperatorIndices();
    }
}
