// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class BoxedLogCounts
    {
        /***
         * We use this only to have a stable runtime reference with the InMemoryRepository
         * but every where else we use the struct.
         *
         * InMemoryRepository has a class constraint so we are forced to use a class 'somewhere'. However
         * we cannot do away with the struct because then that would make 'object copies' and
         * make InMemoryRepository no longer own the single stable reference.
         */
        public LogCounts LogCounts;
    }

    struct LogCounts
    {
        public int Logs;
        public int Warnings;
        public int Errors;

        public static void Serialize(BinaryWriter writer, in LogCounts counts)
        {
            writer.Write(counts.Logs);
            writer.Write(counts.Warnings);
            writer.Write(counts.Errors);
        }

        public static LogCounts Deserialize(BinaryReader reader)
        {
            var logs = reader.ReadInt32();
            var warnings = reader.ReadInt32();
            var errors = reader.ReadInt32();
            return new LogCounts { Logs = logs, Warnings = warnings, Errors = errors };
        }
    }
}
