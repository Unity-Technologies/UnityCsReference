// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Parsing;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Registers reusable templates and applies them by name.
/// </summary>
[Serializable]
public class TemplateFormatter : FormatterBase, IInitializer, ISerializationCallbackReceiver
{
    [Serializable]
    class Template
    {
        public string name;
        public string text;
        public Format Format { get; set; }
    }

    [SerializeField] List<Template> m_Templates = new List<Template>();

    SmartFormatter m_Formatter;
    IDictionary<string, Format> m_TemplatesDict;

    IDictionary<string, Format> Templates
    {
        get
        {
            if (m_TemplatesDict == null)
            {
                var stringComparer = m_Formatter.Settings.GetCaseSensitivityComparer();
                m_TemplatesDict = new Dictionary<string, Format>(stringComparer);

                foreach (var t in m_Templates)
                {
                    if (!string.IsNullOrEmpty(t.name))
                    {
                        try
                        {
                            m_TemplatesDict[t.name] = m_Formatter.Parser.ParseFormat(t.text);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }

            return m_TemplatesDict;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="TemplateFormatter"/> never can handle auto-detection.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if set to <see langword="true"/>, because <see cref="TemplateFormatter"/> cannot auto-detect.</exception>
    public override bool CanAutoDetect
    {
        get => false;
        set
        {
            if (value) throw new ArgumentException($"{nameof(TemplateFormatter)} cannot handle auto-detection");
        }
    }

    /// <inheritdoc/>
    public override string DefaultName => "t";

    ///<inheritdoc />
    public override bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var templateName = formattingInfo.FormatterOptions;
        if (templateName == string.Empty)
        {
            if (formattingInfo.Format is {HasNested : true}) return false;
            templateName = formattingInfo.Format.RawText;
        }

        if (!Templates.TryGetValue(templateName, out var template))
        {
            throw new FormatException($"Formatter named '{formattingInfo.Placeholder?.FormatterName}' found no registered template named '{templateName}'");
        }

        if (IsRecursive(formattingInfo, template))
        {
            throw new FormatException($"Template '{templateName}' calls itself recursively");
        }

        formattingInfo.FormatAsChild(template, formattingInfo.CurrentValue);
        return true;
    }

    static bool IsRecursive(IFormattingInfo formattingInfo, Format template)
    {
        if (formattingInfo is not Core.Formatting.FormattingInfo info)
            return false;

        for (var ancestor = info.Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor.Format, template) && IsSameValue(ancestor.CurrentValue, info.CurrentValue))
                return true;
        }

        return false;
    }

    static bool IsSameValue(object a, object b)
    {
        return a is ValueType ? Equals(a, b) : ReferenceEquals(a, b);
    }

    /// <summary>
    /// Registers a new template.
    /// </summary>
    /// <param name="templateName">Unique name for the template that is not already registered.</param>
    /// <param name="template">String to use as the template.</param>
    public void Register(string templateName, string template)
    {
        var parsed = m_Formatter !.Parser.ParseFormat(template);
        Templates.Add(templateName, parsed);
        m_Templates.Add(new Template { Format = parsed, name = templateName, text = template });
    }

    /// <summary>
    /// Removes a template by name.
    /// </summary>
    /// <param name="templateName">Name of the template to remove.</param>
    /// <returns><see langword="true"/> if the template was found and removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(string templateName)
    {
        m_Templates.RemoveAll(m => m.name == templateName);
        return Templates.Remove(templateName);
    }

    /// <summary>
    /// Removes all registered templates.
    /// </summary>
    public void Clear()
    {
        Templates.Clear();
        m_Templates.Clear();
    }

    ///<inheritdoc/>
    public void Initialize(SmartFormatter smartFormatter)
    {
        m_Formatter = smartFormatter;
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        m_TemplatesDict = null;
    }
}
