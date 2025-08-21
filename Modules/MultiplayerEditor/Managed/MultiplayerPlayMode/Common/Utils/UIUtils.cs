// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal static class UIUtils
    {
        internal static void Spin(VisualElement element)
        {
            const int k_UpdateIntervalMS = 32;
            const float k_RotationSpeedDegSec = 360f;

            var startTime = EditorApplication.timeSinceStartup;

            element.schedule.Execute(_ =>
            {
                var elapsedTime = (float)(EditorApplication.timeSinceStartup - startTime);
                element.style.rotate = new StyleRotate(new Rotate(new Angle(k_RotationSpeedDegSec * elapsedTime, AngleUnit.Degree)));
            }).Every(k_UpdateIntervalMS);
        }
    }
}
