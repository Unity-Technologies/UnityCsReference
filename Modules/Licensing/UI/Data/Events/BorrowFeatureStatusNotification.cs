// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEditor.Licensing.UI.Data.Events.Base;

namespace UnityEditor.Licensing.UI.Data.Events
{
    [Serializable]
    class BorrowFeatureStatusNotification : NotificationWithDetails<BorrowFeatureStatusNotificationDetails> { }

    [Serializable]
    class BorrowFeatureStatusNotificationDetails
    {
        public BorrowFeatureStatus status;
        public int maxBorrowDurationDays;
    }

    enum BorrowFeatureStatus
    {
        Unknown = 0,
        Allowed = 200,
        NotAllowed = 403,
        NotSupported = 501,
    }
}
