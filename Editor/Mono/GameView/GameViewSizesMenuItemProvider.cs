// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class GameViewSizesMenuItemProvider : IFlexibleMenuItemProvider
    {
        private readonly GameViewSizeGroup m_GameViewSizeGroup;

        public GameViewSizesMenuItemProvider(GameViewSizeGroupType gameViewSizeGroupType)
        {
            m_GameViewSizeGroup = GameViewSizes.instance.GetGroup(gameViewSizeGroupType);
        }

        public int Count()
        {
            return m_GameViewSizeGroup.GetTotalCount();
        }

        public object GetItem(int index)
        {
            return m_GameViewSizeGroup.GetGameViewSize(index);
        }

        public int Add(object obj)
        {
            GameViewSize gameViewSize = CastToGameViewSize(obj);
            if (gameViewSize == null)
                return -1;

            m_GameViewSizeGroup.AddCustomSize(gameViewSize);
            GameViewSizes.instance.SaveToHDD();
            return Count() - 1; // assumes that custom sizes is after builtin sizes
        }

        public void Replace(int index, object obj)
        {
            GameViewSize newResolution = CastToGameViewSize(obj);
            if (newResolution == null)
                return;

            if (index < m_GameViewSizeGroup.GetBuiltinCount())
            {
                Debug.LogError("Only custom game view sizes can be changed");
                return;
            }

            GameViewSize gameViewSize = m_GameViewSizeGroup.GetGameViewSize(index);
            if (gameViewSize != null)
            {
                gameViewSize.Set(newResolution);
                GameViewSizes.instance.SaveToHDD();
            }
        }

        public void Remove(int index)
        {
            if (index < m_GameViewSizeGroup.GetBuiltinCount())
            {
                Debug.LogError("Only custom game view sizes can be changed");
                return;
            }

            m_GameViewSizeGroup.RemoveCustomSize(index);
            GameViewSizes.instance.SaveToHDD();
        }

        public object Create()
        {
            return new GameViewSize(GameViewSizeType.FixedResolution, 0, 0, "");
        }

        public void Move(int index, int destIndex, bool insertAfterDestIndex)
        {
            Debug.LogError("Missing impl");
        }

        public string GetName(int index)
        {
            GameViewSize gameViewSize = m_GameViewSizeGroup.GetGameViewSize(index);
            if (gameViewSize != null)
                return gameViewSize.displayText;
            return "";
        }

        public bool IsModificationAllowed(int index)
        {
            return m_GameViewSizeGroup.IsCustomSize(index); // builtin sizes cannot be modified
        }

        public int[] GetSeperatorIndices()
        {
            return new int[] {m_GameViewSizeGroup.GetBuiltinCount() - 1};
        }

        // Private section

        private static GameViewSize CastToGameViewSize(object obj)
        {
            GameViewSize newResolution = obj as GameViewSize;
            if (obj == null)
            {
                Debug.LogError("Incorrect input");
                return null;
            }
            return newResolution;
        }
    }
}

// namespace
