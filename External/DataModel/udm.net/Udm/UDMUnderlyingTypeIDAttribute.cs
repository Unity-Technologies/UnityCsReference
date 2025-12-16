using System;

namespace Unity.DataModel
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    internal sealed class UDMUnderlyingTypeIDAttribute : Attribute
    {
        internal string TypeID { get; private set; }

        internal UDMUnderlyingTypeIDAttribute(string typeID)
        {
            TypeID = typeID;
        }
    }

}

