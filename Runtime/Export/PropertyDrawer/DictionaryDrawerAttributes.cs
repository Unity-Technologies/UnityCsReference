// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    // Selects how a Dictionary field lays out its key/value pairs in the inspector.
    public enum DictionaryLayout
    {
        // Key and value rendered side by side in two resizable columns.
        TwoColumns,
        // Key stacked on top of the value; each value stays collapsible behind a "Value" foldout.
        OneColumnWithValueFoldout,
        // Key stacked on top of the value; every value field is always shown inline, no per-row foldout.
        OneColumnWithValueVisible,
    }

    // Configures how the Dictionary property drawer presents a dictionary field: its layout
    // (one/two columns), the key/value header labels, and the default key column width.
    //
    //     [DictionaryDisplay(layout = DictionaryLayout.OneColumnWithValueVisible,
    //                        keyLabel = "Name", valueLabel = "Item")]
    //     public Dictionary<int, GameObject> data;
    //
    // Every setting is optional: layout defaults to TwoColumns, so a field can set only
    // labels/fraction, e.g. [DictionaryDisplay(keyLabel = "Id")].
    //
    // To configure a dictionary you can't decorate directly — a nested inner dictionary, or a
    // type you don't own — use the assembly-level DictionaryDisplayForTypeAttribute instead.
    //
    // The user can still override the layout (from the header context menu) and the key column
    // width (by dragging the splitter); their choices persist and win over the defaults declared
    // here. The context menu's 'Reset to Defaults' returns to the values defined by this attribute.
    // A field-level attribute takes precedence over any assembly-level one.
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DictionaryDisplayAttribute : Attribute
    {
        public DictionaryLayout layout { get; set; } = DictionaryLayout.TwoColumns;
        public string keyLabel { get; set; } = string.Empty;
        public string valueLabel { get; set; } = string.Empty;
        public float keyColumnFraction { get; set; } = 0.5f;
    }

    // Assembly-level form of DictionaryDisplayAttribute that names an exact closed Dictionary<K,V>
    // to match via typeof. This is the only way to reach a dictionary with no field to decorate —
    // the inner Dictionary<string,float> of a Dictionary<int, Dictionary<string,float>>, or a type
    // you don't own — because a closed generic type can still be referenced through typeof:
    //
    //     [assembly: DictionaryDisplayForType(typeof(Dictionary<string, float>),
    //                                     layout = DictionaryLayout.OneColumnWithValueVisible,
    //                                     keyLabel = "Bone", valueLabel = "Weight")]
    //
    // The target must be a closed Dictionary<TKey,TValue>; any other type is ignored (with a
    // warning). It configures that exact dictionary's layout, header labels, and keyColumnFraction.
    //
    // A rule matches globally — it configures that dictionary shape wherever it is used, across
    // assemblies — but you may only declare a rule for a Dictionary<K,V> whose shape involves a type
    // your assembly *defines*: its key, its value, or a type nested within either (so Dictionary<int,
    // YourType>, Dictionary<int, YourType[]>, and Dictionary<int, List<YourType>> all qualify). This
    // lets an extension style dictionaries over its own types everywhere they appear, while preventing
    // a rule for a shape it has no stake in — e.g. Dictionary<int,int> — from taking over every such
    // dictionary in the project. A rule whose target involves no type from its own assembly is ignored
    // (with a warning).
    //
    // Matching is by type, not by field: a rule configures every dictionary of that shape across the
    // project, not one field. Use a field-level [DictionaryDisplay] to configure or override a single
    // field — it always wins over any assembly-level rule.
    //
    // Because it derives from DictionaryDisplayAttribute it shares every display setting; the
    // AttributeUsage below is declared explicitly so it overrides the base's Field target (an
    // attribute subclass otherwise inherits its base's AttributeUsage), keeping the field form and
    // the assembly form mutually exclusive at compile time.
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class DictionaryDisplayForTypeAttribute : DictionaryDisplayAttribute
    {
        public Type targetType { get; }

        public DictionaryDisplayForTypeAttribute(Type targetType)
        {
            this.targetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        }
    }
}
