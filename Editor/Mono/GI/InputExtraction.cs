// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.LightBaking;

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
            const string outputFolderPath = "unused"; // We are not using disk IO.
            UnityEditor.LightBaking.LightBaker.BakeInput input = new();
            UnityEditor.LightBaking.InputExtraction.SourceMap map = new();
            bool result = UnityEditor.LightBaking.InputExtraction.ExtractFromScene(outputFolderPath, input, map);
            bakeInput = new BakeInput(input);
            return result;
        }

        public static bool PopulateWorld(BakeInput bakeInput, BakeProgressState progress, IDeviceContext context, IWorld world)
        {
            UnityEditor.LightBaking.LightBaker.Result result = UnityEditor.LightBaking.LightBaker.PopulateWorld(bakeInput.bakeInput, progress, context, world);
            return result.type == UnityEditor.LightBaking.LightBaker.ResultType.Success;
        }

        internal static bool SerializeBakeInput(string path, InputExtraction.BakeInput bakeInput) => LightBaker.Serialize(path, bakeInput.bakeInput);

        internal static bool DeserializeBakeInput(string path, out InputExtraction.BakeInput bakeInput)
        {
            UnityEditor.LightBaking.LightBaker.BakeInput lightBakerBakeInput = new();
            bakeInput = new BakeInput(lightBakerBakeInput);
            return LightBaker.Deserialize(path, bakeInput.bakeInput);
        }
    }
}
