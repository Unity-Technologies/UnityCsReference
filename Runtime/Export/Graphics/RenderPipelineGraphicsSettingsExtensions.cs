// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering
{
    public static class RenderPipelineGraphicsSettingsExtensions
    {
        public static void SetValueAndNotify<T>(this IRenderPipelineGraphicsSettings settings, ref T currentPropertyValue, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (GraphicsSettings.s_PropertyHelper.SetProperty(settings, ref currentPropertyValue, newValue, propertyName))
            {
                GraphicsSettings.Internal_SetAllRenderPipelineSettingsDirty();
            }
        }

        public static void NotifyValueChanged(this IRenderPipelineGraphicsSettings settings, [CallerMemberName] string propertyName = null)
        {
            GraphicsSettings.s_PropertyHelper.NotifyValueChange(settings, propertyName);
            GraphicsSettings.Internal_SetAllRenderPipelineSettingsDirty();
        }
    }
}
