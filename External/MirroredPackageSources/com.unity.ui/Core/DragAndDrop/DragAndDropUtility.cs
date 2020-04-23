using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal static class DragAndDropUtility
    {
        private static Func<IDragAndDrop> s_MakeClientFunc;
        private static IDragAndDrop s_DragAndDrop;

        public static IDragAndDrop dragAndDrop
        {
            get
            {
                if (s_DragAndDrop == null)
                {
                    if (s_MakeClientFunc != null)
                        s_DragAndDrop = s_MakeClientFunc.Invoke();
                    else
                        s_DragAndDrop = new DefaultDragAndDropClient();
                }

                return s_DragAndDrop;
            }
        }

        internal static void RegisterMakeClientFunc(Func<IDragAndDrop> makeClient)
        {
            if (s_MakeClientFunc != null)
                throw new UnityException($"The MakeClientFunc has already been registered. Registration denied.");

            s_MakeClientFunc = makeClient;
        }
    }

    internal class DefaultDragAndDropClient : IDragAndDrop, IDragAndDropData
    {
        private StartDragArgs m_StartDragArgs;
        public object userData => m_StartDragArgs?.userData;
        public IEnumerable<Object> unityObjectReferences => m_StartDragArgs?.unityObjectReferences;

        public void StartDrag(StartDragArgs args)
        {
            m_StartDragArgs = args;
        }

        public void AcceptDrag()
        {
            m_StartDragArgs = null;
        }

        public void SetVisualMode(DragVisualMode visualMode)
        {
        }

        public IDragAndDropData data
        {
            get { return this; }
        }

        public object GetGenericData(string key)
        {
            if (m_StartDragArgs == null)
                return null;

            return m_StartDragArgs.genericData.ContainsKey(key) ? m_StartDragArgs.genericData[key] : null;
        }
    }
}
