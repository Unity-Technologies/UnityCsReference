// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal class SimulationOnPlayMenuItemProvider : IFlexibleMenuItemProvider
    {
        public int Count()
        {
            // The simulator window only supports "play normally (focuced/unfocused) and play maximized
            // fullscreen support may be added in the future.
            int optionCount = GameViewOnPlayMenu.kPlayModeBaseOptionCount;
            return optionCount;
        }

        public object GetItem(int index)
        {
            return null;
        }

        public int Add(object obj)
        {
            throw new NotImplementedException("Operation not supported");
        }

        public void Replace(int index, object obj)
        {
            throw new NotImplementedException("Operation not supported");
        }

        public void Remove(int index)
        {
            throw new NotImplementedException("Operation not supported");
        }

        public object Create()
        {
            throw new NotImplementedException("Operation not supported");
        }

        public void Move(int index, int destIndex, bool insertAfterDestIndex)
        {
            throw new NotImplementedException("Operation not supported");
        }

        public string GetName(int index)
        {
            return GameViewOnPlayMenu.GetOnPlayBehaviorName(index);
        }

        public bool IsModificationAllowed(int index)
        {
            return false;
        }

        public int[] GetSeperatorIndices()
        {
            return null;
        }
    }

    internal class GameViewOnPlayMenuItemProvider : IFlexibleMenuItemProvider
    {
        public int Count()
        {
            // "Play Normal" and "Play Maximized" should always be options.
            // The play fullscreen option will be added per-display.
            int optionCount = GameViewOnPlayMenu.kPlayModeBaseOptionCount;
            optionCount += EditorFullscreenController.GetConnectedDisplayNames().Length;
            return optionCount;
        }

        public object GetItem(int index)
        {
            return null;
        }

        public int Add(object obj)
        {
            throw new NotImplementedException("Operation not supported");
        }

        public void Replace(int index, object obj)
        {
            throw new NotImplementedException("Operation not supported");
        }

        public void Remove(int index)
        {
            throw new NotImplementedException("Operation not supported");
        }

        public object Create()
        {
            throw new NotImplementedException("Operation not supported");
        }

        public void Move(int index, int destIndex, bool insertAfterDestIndex)
        {
            throw new NotImplementedException("Operation not supported");
        }

        public string GetName(int index)
        {
            return GameViewOnPlayMenu.GetOnPlayBehaviorName(index);
        }

        public bool IsModificationAllowed(int index)
        {
            return false;
        }

        public int[] GetSeperatorIndices()
        {
            return null;
        }
    }
}
