// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Text;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    // Clone layout data paired with the path that it was last loaded from.
    // Using a class so we can hold a reference
    class CloneDataFile
    {
        public string Path;
        public CloneData Data;

        const string k_Filename = "CloneData.json";
        public static string CloneDataPath(string directory) => System.IO.Path.Combine(directory, k_Filename);

        public static CloneData LoadFromFile(CloneDataFile dataFile)
        {
            if (File.Exists(dataFile.Path))
            {
                var bytes = Encoding.UTF8.GetString(File.ReadAllBytes(dataFile.Path));
                if (!CloneData.TryDeserialize(bytes, out dataFile.Data))
                {
                    Debug.LogError("Could not load configuration. Using default.");
                }
            }
            else
            {
                // If no file exists, initialize it.
                dataFile.Data = CloneData.NewDefault();
                SaveToFile(dataFile);
            }
            return dataFile.Data;
        }

        public static void SaveToFile(CloneDataFile dataFile)
        {
            var json = CloneData.Serialize(dataFile.Data);
            File.WriteAllBytes(dataFile.Path, Encoding.UTF8.GetBytes(json));
        }
    }
}
