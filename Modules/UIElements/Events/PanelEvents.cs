// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public interface IPanelChangedEvent
    {
    }

    public abstract class PanelChangedEventBase<T> : EventBase<T>, IPanelChangedEvent, IPropagatableEvent where T : PanelChangedEventBase<T>, new()
    {
        public IPanel originPanel { get; private set; }
        public IPanel destinationPanel { get; private set; }

        protected override void Init()
        {
            base.Init();
            originPanel = null;
            destinationPanel = null;
        }

        public static T GetPooled(IPanel originPanel, IPanel destinationPanel)
        {
            T e = GetPooled();
            e.originPanel = originPanel;
            e.destinationPanel = destinationPanel;
            return e;
        }

        protected PanelChangedEventBase()
        {
            Init();
        }
    }

    public class AttachToPanelEvent : PanelChangedEventBase<AttachToPanelEvent>
    {
    }

    public class DetachFromPanelEvent : PanelChangedEventBase<DetachFromPanelEvent>
    {
    }
}
