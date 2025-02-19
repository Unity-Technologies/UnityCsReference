// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties.Internal;

namespace Unity.Properties
{
    static partial class PropertyBag
    {
        /// <summary>
        /// Constructs a new instance of the given <typeparamref name="TContainer"/> type.
        /// </summary>
        /// <typeparam name="TContainer">The container type to construct.</typeparam>
        /// <returns>A new instance of <typeparamref name="TContainer"/>.</returns>
        public static TContainer CreateInstance<TContainer>()
        {
            var propertyBag = PropertyBagStore.GetPropertyBag<TContainer>();

            if (null == propertyBag)
                throw new MissingPropertyBagException(typeof(TContainer));

            return propertyBag.CreateInstance();
        }
    }
}
