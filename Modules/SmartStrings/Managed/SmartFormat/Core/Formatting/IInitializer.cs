// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

namespace Unity.SmartStrings.Core.Extensions;

/// <summary>
/// Initializes an <see cref="ISource"/> or <see cref="IFormatter"/>.
/// </summary>
public interface IInitializer
{
    /// <summary>
    /// Initializes an <see cref="ISource"/> or <see cref="IFormatter"/>.
    /// The method gets called when adding an extension to a <see cref="SmartFormatter"/> instance.
    /// </summary>
    /// <param name="smartFormatter">Formatter that the extension is being added to.</param>
    void Initialize(SmartFormatter smartFormatter);
}
