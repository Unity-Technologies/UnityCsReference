// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class BackgroundPropertyHelper
    {
        public static BackgroundPosition ConvertScaleModeToBackgroundPosition(ScaleMode scaleMode = ScaleMode.StretchToFill)
        {
            return new BackgroundPosition(BackgroundPositionKeyword.Center);
        }

        public static BackgroundRepeat ConvertScaleModeToBackgroundRepeat(ScaleMode scaleMode = ScaleMode.StretchToFill)
        {
            return new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        }

        public static BackgroundSize ConvertScaleModeToBackgroundSize(ScaleMode scaleMode = ScaleMode.StretchToFill)
        {
            if (scaleMode == ScaleMode.ScaleAndCrop)
            {
                return new BackgroundSize(BackgroundSizeType.Cover);
            }
            else if (scaleMode == ScaleMode.ScaleToFit)
            {
                return new BackgroundSize(BackgroundSizeType.Contain);
            }
            else // ScaleMode.StretchToFill
            {
                return new BackgroundSize(Length.Percent(100.0f), Length.Percent(100.0f));
            }
        }

        public static ScaleMode ResolveUnityBackgroundScaleMode(BackgroundPosition backgroundPositionX, BackgroundPosition backgroundPositionY,
            BackgroundRepeat backgroundRepeat, BackgroundSize backgroundSize, out bool valid)
        {

            if (backgroundPositionX == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop) &&
                backgroundPositionY == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop) &&
                backgroundRepeat == BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleAndCrop) &&
                backgroundSize == BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleAndCrop))
            {
                valid = true;
                return ScaleMode.ScaleAndCrop;
            }
            else if (backgroundPositionX == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit) &&
                     backgroundPositionY == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit) &&
                     backgroundRepeat == BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleToFit) &&
                     backgroundSize == BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleToFit))
            {
                valid = true;
                return ScaleMode.ScaleToFit;
            }
            else if (backgroundPositionX == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.StretchToFill) &&
                     backgroundPositionY == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.StretchToFill) &&
                     backgroundRepeat == BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.StretchToFill) &&
                     backgroundSize == BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.StretchToFill))
            {
                valid = true;
                return ScaleMode.StretchToFill;
            }
            else
            {
                valid = false;
                return ScaleMode.StretchToFill;
            }
        }
    }
}
