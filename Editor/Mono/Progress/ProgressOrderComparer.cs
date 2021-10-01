// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor
{
    internal class ProgressOrderComparer : IComparer<Progress.Item>
    {
        bool m_Reverse;

        public ProgressOrderComparer(bool reverse = false)
        {
            m_Reverse = reverse;
        }

        public int Compare(Progress.Item source, Progress.Item compared)
        {
            int compare = CompareStatus(source.status, compared.status);
            if (compare == 0)
            {
                compare = source.priority.CompareTo(compared.priority);
                if (compare == 0)
                    compare = source.startTime.CompareTo(compared.startTime);
                if (compare == 0)
                    compare = source.id.CompareTo(compared.id);
            }

            if (m_Reverse)
                return compare * -1;
            return compare;
        }

        private static int CompareStatus(Progress.Status statusSource, Progress.Status statusToCompare)
        {
            // Pause and Running have the same priority (we don't want the item to change position when clicking pause/resume)
            if ((statusSource == Progress.Status.Running || statusSource == Progress.Status.Paused) && (statusToCompare != Progress.Status.Running && statusToCompare != Progress.Status.Paused))
                return 1;
            if (statusSource == Progress.Status.Failed && (statusToCompare != Progress.Status.Failed && statusToCompare != Progress.Status.Running && statusToCompare != Progress.Status.Paused))
                return 1;
            if (statusSource == Progress.Status.Canceled && statusToCompare == Progress.Status.Succeeded)
                return 1;
            if (statusSource == statusToCompare
                // Same thing, Pause and Running have the same priority (we don't want the item to change position when clicking pause/resume)
                || (statusSource == Progress.Status.Running || statusSource == Progress.Status.Paused) && (statusToCompare == Progress.Status.Running || statusToCompare == Progress.Status.Paused))
                return 0;
            return -1;
        }
    }
}
