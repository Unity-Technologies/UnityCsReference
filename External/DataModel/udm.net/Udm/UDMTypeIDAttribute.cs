using System;

namespace Unity.DataModel
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    internal sealed class UDMTypeIDAttribute : Attribute
    {
        internal string TypeID { get; private set; }

        internal UDMTypeIDAttribute(string typeID)
        {
            TypeID = typeID;
        }
    }

}

