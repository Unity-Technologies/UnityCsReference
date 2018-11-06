// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public interface IManipulator
    {
        VisualElement target { get; set; }
    }

    public abstract class Manipulator : IManipulator
    {
        protected abstract void RegisterCallbacksOnTarget();
        protected abstract void UnregisterCallbacksFromTarget();

        VisualElement m_Target;
        public VisualElement target
        {
            get { return m_Target; }
            set
            {
                if (target != null)
                {
                    UnregisterCallbacksFromTarget();
                }
                m_Target = value;
                if (target != null)
                {
                    RegisterCallbacksOnTarget();
                }
            }
        }
    }
}
