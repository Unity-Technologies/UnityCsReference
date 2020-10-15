using System.Collections.Generic;

// il2cpp or editor may not make use of all fields so suppress unused fields warn
#pragma warning disable CS0649

namespace UnityEditorInternal
{
    internal enum Il2CppMessageType
    {
        Warning,
        Error
    }

    [System.Serializable]
    internal class Message
    {
        [UnityEngine.SerializeField]
        public Il2CppMessageType Type;
        [UnityEngine.SerializeField]
        public string Text;
    }

    [System.Serializable]
    internal class Il2CppToEditorData
    {
        [UnityEngine.SerializeField]
        public List<Message> Messages;
        [UnityEngine.SerializeField]
        public string CommandLine;
    }
}
