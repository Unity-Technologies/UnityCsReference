// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.PackageManager;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Custom Binding that waits on the <see cref="VisualElement.dataSource"/>
    /// to be set to a valid <see cref="PackageInfo"/> before setting applying the binding value.
    /// </summary>
    /// <remarks>
    /// This allows all bindings to be set in the uxml
    /// while the PackageInfo struct is not set to its parent VisualElement yet.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    [UxmlObject]
    abstract partial class DelayedPackageBinding<T> : CustomBinding where T : VisualElement
    {
        public DelayedPackageBinding()
        {
            updateTrigger = BindingUpdateTrigger.WhenDirty;
        }

        // Subscribed on activation rather than in the constructor: a constructor subscription is never
        // undone, so every binding ever created would stay on the static event, and it would not
        // survive a code reload either (UAL0015).
        protected internal override void OnActivated(in BindingActivationContext context)
        {
            base.OnActivated(context);
            Events.registeredPackages += OnRegisteredPackages;
        }

        protected internal override void OnDeactivated(in BindingActivationContext context)
        {
            base.OnDeactivated(context);
            Events.registeredPackages -= OnRegisteredPackages;
        }

        void OnRegisteredPackages(PackageRegistrationEventArgs _) => MarkDirty();

        protected internal override BindingResult Update(in BindingContext context)
        {
            if (context.targetElement is not T element)
            {
                return new BindingResult(BindingStatus.Failure, $"Target element is not expected type {typeof(T)}");
            }

            if (context.bindingId != bindingId)
            {
                return new BindingResult(BindingStatus.Failure, $"Target property is not expected field {bindingId}");
            }

            if (context.dataSource is PackageInfo item)
            {
                SetValue(element, item);
                return new BindingResult(BindingStatus.Success);
            }

            return new BindingResult(BindingStatus.Pending);
        }

        protected abstract BindingId bindingId { get; }

        protected abstract void SetValue(T element, PackageInfo item);
    }

    [UxmlObject]
    partial class DelayedPackageNameBinding : DelayedPackageBinding<Label>
    {
        protected override BindingId bindingId => new(nameof(Label.text));

        protected override void SetValue(Label element, PackageInfo item)
        {
            element.text = item.displayName;
        }
    }

    [UxmlObject]
    partial class DelayedPackageDescriptionBinding : DelayedPackageBinding<Label>
    {
        protected override BindingId bindingId => new(nameof(Label.text));

        protected override void SetValue(Label element, PackageInfo item)
        {
            element.text = item.description;
        }
    }

    [UxmlObject]
    partial class DelayedPackageDocumentationBinding : DelayedPackageBinding<HelpIcon>
    {
        protected override BindingId bindingId => new(nameof(HelpIcon.DocumentationUrl));

        protected override void SetValue(HelpIcon element, PackageInfo item)
        {
            element.PackageName = item.name;

            var url = item.documentationUrl;
            if (string.IsNullOrEmpty(url))
                url = $"https://docs.unity3d.com/Packages/{item.name}@latest/";

            element.DocumentationUrl = url;
        }
    }

    [UxmlObject]
    partial class DelayedPackageIdBinding : DelayedPackageBinding<PackageIcon>
    {
        protected override BindingId bindingId => new(nameof(PackageIcon.PackageId));

        protected override void SetValue(PackageIcon element, PackageInfo item)
        {
            element.PackageId = item.name;
        }
    }

    [UxmlObject]
    partial class DelayedPackageInstalledBinding : DelayedPackageBinding<VisualElement>
    {
        protected override BindingId bindingId => new("IsInstalled");

        protected override void SetValue(VisualElement element, PackageInfo item)
        {
            var info = PackageInfo.FindForPackageName(item.name);
            element.EnableInClassList(StyleClasses.Hidden, info == null);
        }
    }
}
