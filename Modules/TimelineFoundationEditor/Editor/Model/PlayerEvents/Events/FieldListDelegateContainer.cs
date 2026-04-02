// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.Timeline.Foundation.Model.Internals
{
    readonly struct FieldListDelegateContainer<TArg>
    {
        public readonly IReadOnlyList<string> fields;
        public readonly FieldDelegate<TArg> fieldDelegate;
        public readonly FieldDelegateLookup<TArg>.Comparer customComparer;

        public FieldListDelegateContainer(FieldDelegate<TArg> fieldDelegate)
        {
            fields = new List<string>();
            this.fieldDelegate = fieldDelegate;
            customComparer = null;
        }

        public FieldListDelegateContainer(string field, FieldDelegate<TArg> fieldDelegate, FieldDelegateLookup<TArg>.Comparer customComparer = null)
        {
            fields = new List<string> { field };
            this.fieldDelegate = fieldDelegate;
            this.customComparer = customComparer;
        }

        public FieldListDelegateContainer(IReadOnlyCollection<string> fields, FieldDelegate<TArg> fieldDelegate, FieldDelegateLookup<TArg>.Comparer customComparer = null)
        {
            this.fields = new List<string>(fields);
            this.fieldDelegate = fieldDelegate;
            this.customComparer = customComparer;
        }
    }
}
