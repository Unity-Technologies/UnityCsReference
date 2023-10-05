// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.LightTransport
{
    public static class InputExtraction
    {
        // Completely opaque object in the public API.
        public class BakeInput
        {
            internal BakeInput(UnityEditor.LightBaking.LightBaker.BakeInput editorBakeInput)
            {
                bakeInput = editorBakeInput;
            }
            internal UnityEditor.LightBaking.LightBaker.BakeInput bakeInput { get; }
        }

        public static bool ExtractFromScene(out BakeInput bakeInput)
        {
            string outputFolderPath = "unused"; // We are not using disk IO.
            UnityEditor.LightBaking.LightBaker.BakeInput input = new UnityEditor.LightBaking.LightBaker.BakeInput();
            UnityEditor.LightBaking.InputExtraction.SourceMap map = new UnityEditor.LightBaking.InputExtraction.SourceMap();
            bool result = UnityEditor.LightBaking.InputExtraction.ExtractFromScene(outputFolderPath, input, map);
            bakeInput = new BakeInput(input);
            return result;
        }

        public static bool PopulateWorld(BakeInput bakeInput, BakeProgressState progress, IDeviceContext context, IWorld world)
        {
            UnityEditor.LightBaking.LightBaker.Result result = UnityEditor.LightBaking.LightBaker.PopulateWorld(bakeInput.bakeInput, progress, context, world);
            return result.type == UnityEditor.LightBaking.LightBaker.ResultType.Success;
        }
    }
}
