// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    // Derive and implement interface for your own preset library. See GradientPresetLibrary.cs for example.
    internal abstract class PresetLibrary : ScriptableObject
    {
        public abstract int Count();
        public abstract object GetPreset(int index);
        public abstract void Add(object presetObject, string presetName);
        public abstract void Replace(int index, object newPresetObject);
        public abstract void Remove(int index);
        public abstract void Move(int index, int destIndex, bool insertAfterDestIndex);
        public abstract void Draw(Rect rect, int index);
        public abstract void Draw(Rect rect, object presetObject);
        public abstract string GetName(int index);
        public abstract void SetName(int index, string name);
    }


    internal static class PresetLibraryHelpers
    {
        public static void MoveListItem<T>(List<T> list, int index, int destIndex, bool insertAfterDestIndex)
        {
            if (index < 0 || destIndex < 0)
            {
                Debug.LogError("Invalid preset move");
                return;
            }

            if (index == destIndex)
                return;

            if (destIndex > index)
                destIndex--;
            if (insertAfterDestIndex && destIndex < list.Count - 1)
                destIndex++;

            var item = list[index];
            list.RemoveAt(index);
            list.Insert(destIndex, item);
        }
    }
}   // UnityEditor
