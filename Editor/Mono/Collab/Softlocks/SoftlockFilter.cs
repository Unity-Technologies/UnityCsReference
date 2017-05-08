// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEditor.Collaboration
{
    internal class SoftLockFilters : AbstractFilters
    {
        public override void InitializeFilters()
        {
            filters = new List<string[]>() {
                new string[] { "All In Progress" , "s:inprogress"},
            };
        }

        public SoftLockFilters()
        {
            InitializeFilters();
        }

        public void OnSettingStatusChanged(CollabSettingType type, CollabSettingStatus status)
        {
            if (type == CollabSettingType.InProgressEnabled && (status == CollabSettingStatus.Available))
            {
                if (Collab.instance.IsCollabEnabledForCurrentProject() && CollabSettingsManager.inProgressEnabled)
                    ShowInFavoriteSearchFilters();
                else
                    HideFromFavoriteSearchFilters();
            }
        }
    }
}
