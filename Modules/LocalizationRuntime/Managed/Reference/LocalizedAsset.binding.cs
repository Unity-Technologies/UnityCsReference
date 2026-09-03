// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization;

[UxmlObject]
public partial class LocalizedAsset<TObject>
{
    TObject m_BoundValue;

    private protected override void StartBinding()
    {
        ApplyEditorPreviewLocale();
        // Subscribing resolves and delivers, so this also primes the first value.
        AssetChanged += OnBoundValueChanged;
    }

    private protected override void StopBinding()
    {
        AssetChanged -= OnBoundValueChanged;
        m_BoundValue = null;
    }

    private protected override BindingResult WriteBoundValue(in BindingContext context)
        => WriteToTarget(context, m_BoundValue);

    void OnBoundValueChanged(TObject value)
    {
        m_BoundValue = value;
        MarkDirty();
    }
}

[UxmlObject]
public partial class LocalizedTexture
{
    /// <summary>
    /// Creates a localized texture reference.
    /// </summary>
    /// <remarks>
    /// The reference resolves a Texture, but style and image fields ask for the narrower Texture2D or a Background,
    /// and no built-in converter widens from Texture to either.
    /// </remarks>
    public LocalizedTexture()
    {
        Converters.AddConverter((ref Texture texture) => texture as Texture2D);
        Converters.AddConverter((ref Texture texture) => Background.FromTexture2D(texture as Texture2D));
    }
}

[UxmlObject]
public partial class LocalizedGameObject { }

[UxmlObject]
public partial class LocalizedObject { }

[UxmlObject]
public partial class LocalizedSprite { }

[UxmlObject]
public partial class LocalizedMaterial { }

[UxmlObject]
public partial class LocalizedFont { }
