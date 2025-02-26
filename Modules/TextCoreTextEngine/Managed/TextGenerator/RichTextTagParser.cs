// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine.Bindings;

#nullable enable

namespace UnityEngine.TextCore
{

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal static class RichTextTagParser
    {
        public enum TagType
        {
            Hyperlink,
            Align,
            AllCaps,
            Alpha,
            Bold,
            Br,
            Color,
            CSpace,
            Font,
            FontWeight,
            Italic,
            Indent,
            LineHeight,
            LineIndent,
            Link,
            Lowercase,
            Mark,
            Mspace,
            NoBr,
            NoParse,
            Strikethrough,
            Size,
            SmallCaps,
            Space,
            Sprite,
            Style,
            Subscript,
            Superscript,
            Underline,
            Uppercase,
            Unknown // Not a real tag, used to indicate an error

            //gradient: margin, pos, rotate , width, voffset will not be supported
        }

        internal record TagTypeInfo
        {
            internal TagTypeInfo(TagType tagType, string name, TagValueType valueType = TagValueType.None, TagUnitType unitType = TagUnitType.Pixels)
            {
                TagType = tagType;
                this.name = name;
                this.valueType = valueType;
                this.unitType = unitType;
            }

            public TagType TagType;
            public string name;
            public TagValueType valueType;
            public TagUnitType unitType;
        }

        internal readonly static TagTypeInfo[] TagsInfo =
            {
            new TagTypeInfo(TagType.Hyperlink, "a"),
            new TagTypeInfo(TagType.Align,"align"), //"left", "center", "right", "justified", "flush"
            new TagTypeInfo(TagType.AllCaps,  "allcaps"), //none
            new TagTypeInfo(TagType.Alpha, "alpha"), //<alpha=#FF>FF <alpha=#CC>CC <alpha=#AA>AA <alpha=#88>88 <alpha=#66>66 <alpha=#44>44 <alpha=#22>22 <alpha=#00>00
            new TagTypeInfo(TagType.Bold,"b" ),
            new TagTypeInfo(TagType.Br,"br"),
            new TagTypeInfo(TagType.Color,"color",TagValueType.ColorValue), //<color="red">Red <color=#005500>Dark Green <#0000FF>Blue <color=#FF000088>Semitransparent Red
            new TagTypeInfo(TagType.CSpace,"cspace" ), //<cspace=1em>Spacing</cspace> is just as important as <cspace=-0.5em>timing.
            new TagTypeInfo(TagType.Font,"font"), //<font="Impact SDF">a different font?</font> or just <font="NotoSans" material="NotoSans Outline">a different material?
            new TagTypeInfo(TagType.FontWeight,"font-weight"), //<font-weight="100">Thin</font-weight>
            new TagTypeInfo(TagType.Italic,"i"),
            new TagTypeInfo(TagType.Indent,"indent"), //<indent=15%> pixels, font units, or percentages.
            new TagTypeInfo(TagType.LineHeight,"line-height"), //pixels, font units, or percentages. <line-height=100%>Rather cozy.
            new TagTypeInfo(TagType.LineIndent,"line-indent" ), //pixels, font units, or percentages.
            new TagTypeInfo(TagType.Link, "link"), //<link="ID">my link</link>
            new TagTypeInfo(TagType.Lowercase,"lowercase"),//none
            new TagTypeInfo(TagType.Mark,"mark" ), //<mark=#ffff00aa>
            new TagTypeInfo(TagType.Mspace,"mspace" ), // monospace : pixels or font units.
            new TagTypeInfo(TagType.NoBr,"nobr"), // none
            new TagTypeInfo(TagType.NoParse,"noparse"), // none
            new TagTypeInfo(TagType.Strikethrough,"s"), //striketrhough
            new TagTypeInfo(TagType.Size,"size"), // <size=20%>  // pixels, font units, or percentage. Pixel adjustments can be absolute (5px, 10px, and so on) or relative (+1 or -1, for example). Relative sizes are based on the original font size, so they're not cumulative.
            new TagTypeInfo(TagType.SmallCaps,"smallcaps" ),//none
            new TagTypeInfo(TagType.Space,"space" ), //pixels or font units.
            new TagTypeInfo(TagType.Sprite,"sprite"),
            // <sprite index=1>  <sprite name="spriteName">  <sprite=1>, <sprite="assetName" index=1> or by name <sprite="assetName" name="spriteName">.
            //tint=1 attribute to the tag tints the sprite with the TextMesh Pro object's Vertex Color. You can choose a different color by adding a color attribute to the tag (color=#FFFFFF).
            new TagTypeInfo ( TagType.Style,"style"), //<style="Title">Styles</style>
            new TagTypeInfo ( TagType.Subscript,"sub" ),
            new TagTypeInfo ( TagType.Superscript,"sup" ),
            new TagTypeInfo ( TagType.Underline,"u"),
            new TagTypeInfo ( TagType.Uppercase,"uppercase"),//none
            // page

            };

        internal enum TagValueType
        {
            None = 0x0,
            NumericalValue = 0x1,
            StringValue = 0x2,
            ColorValue = 0x4,
        }

        internal enum TagUnitType
        {
            Pixels = 0x0,
            FontUnits = 0x1,
            Percentage = 0x2,
        }

        //TODO : change this for an union when development is over to save memory
        // we possibly could remove the type check on getter unless in debug mode
        //[StructLayout(LayoutKind.Explicit)]
        internal record TagValue
        {
            internal TagValue(float value)
            {
                type = TagValueType.NumericalValue;
                m_numericalValue = value;
            }

            internal TagValue(Color value)
            {
                type = TagValueType.ColorValue;
                m_colorValue = value;
            }

            internal TagValue(string value)
            {
                type = TagValueType.StringValue;
                m_stringValue = value;
            }

            //[FieldOffset(0)]
            internal TagValueType type;

            //[FieldOffset(4)]
            //private TagUnitType unit;

            //[FieldOffset(8)]
            private string? m_stringValue;

            //[FieldOffset(8)]
            private float m_numericalValue;

            //[FieldOffset(8)]
            private Color m_colorValue;


            internal string? StringValue
            {
                get
                {
                    if (type != TagValueType.StringValue)
                        throw new InvalidOperationException("Not a string value");
                    return m_stringValue;
                }
            }

            internal float NumericalValue
            {
                get
                {
                    if (type != TagValueType.NumericalValue)
                        throw new InvalidOperationException("Not a numerical value");
                    return m_numericalValue;
                }
            }

            internal Color ColorValue
            {
                get
                {
                    if (type != TagValueType.ColorValue)
                        throw new InvalidOperationException("Not a color value");
                    return m_colorValue;
                }
            }


        }


        internal struct Tag
        {
            public TagType tagType;
            public bool isClosing;
            public int start; //position of the '<' character
            public int end; //position of the '>' character
            public TagValue? value; //could be replaced by a nullable struct?
        }

        public struct Segment
        {
            public List<Tag>? tags;
            public int start;
            public int end;
        }

        internal record ParseError
        {
            internal ParseError(string message, int position)
            {
                this.message = message;
                this.position = position;
            }
            public readonly int position;
            public readonly string message;
        }

        static bool tagMatch(ReadOnlySpan<char> tagCandidate, string tagName)
        {
            return tagCandidate.StartsWith(tagName.AsSpan()) && (tagCandidate.Length == tagName.Length || (!char.IsLetter(tagCandidate[tagName.Length]) && tagCandidate[tagName.Length] != '-'));
        }

        //Return true if there is a match
        static bool SpanToEnum(ReadOnlySpan<char> tagCandidate, out TagType tagType, out string? error, out ReadOnlySpan<char> attribute)
        {
            for (int i = 0; i < TagsInfo.Length; i++)
            {
                string tagName = TagsInfo[i].name;
                if (tagMatch(tagCandidate, tagName))
                {
                    tagType = TagsInfo[i].TagType;
                    error = null;
                    attribute = tagCandidate.Slice(tagName.Length);//Support only one attribute for now
                    return true;
                }
            }

            //Special case for color where there is no tag, just the attribute.
            if(tagCandidate.Length > 4 &&tagCandidate[0] == '#')
            {
                tagType = TagType.Color;
                error = null;
                attribute = tagCandidate;
                return true;
            }

            error = "Unknown tag: " + tagCandidate.ToString();
            tagType = TagType.Unknown;
            attribute = null;
            return false;
        }


        internal static List<Tag> FindTags(string inputStr, List<ParseError>? errors = null)
        {
            var input = inputStr.ToCharArray();
            var result = new List<Tag>();
            int pos = 0;

            while (true)
            {
                var start = Array.IndexOf(input, '<', pos);
                if (start == -1) // no tag
                    break;

                var end = Array.IndexOf(input, '>', start);
                if (end == -1)
                    break;

                bool isClosing = (input.Length > start + 1 && input[start + 1] == '/');

                if (end == start + 1)
                {
                    errors?.Add(new("Empty tag", start));
                    pos = end + 1;
                    continue;
                }

                pos = end + 1;

                if (!isClosing)
                {
                    var span = input.AsSpan(start + 1, end - start - 1);
                    if (SpanToEnum(span, out TagType tagType, out string? error, out var atributeSection))
                    {
                        // TODO Manual parsing of color need to be moved elsewhere
                        TagValue? value = null;
                        if (tagType == TagType.Color)
                        {
                            if (atributeSection.Length >= 2 && atributeSection[0] == '=')
                                atributeSection = atributeSection.Slice(1); // we should probably have a better way to do this

                            if (atributeSection.Length >= 4 && atributeSection[0] == '"' && atributeSection[atributeSection.Length - 1] == '"')
                            {
                                ColorUtility.TryParseHtmlString(atributeSection.Slice(1, atributeSection.Length - 2).ToString(), out Color color);
                                value = new TagValue(color);
                            }
                            else
                            {
                                ColorUtility.TryParseHtmlString(atributeSection.ToString(), out Color color);
                                value = new TagValue(color);
                            }

                            if (value is null)
                            {
                                errors?.Add(new("Invalid color value", start));
                                pos = start + 1; //malformed tag, skip the '<' character
                                continue;
                            }
                        }

                        if (tagType == TagType.Link || tagType == TagType.Hyperlink)
                        {
                            if (tagType == TagType.Hyperlink && atributeSection.StartsWith(" href="))
                                atributeSection = atributeSection.Slice(" href=".Length);

                            // strip the = for <link=xxxx>. The lenght need to be checked so that it is greater than 0
                            if (atributeSection.Length >= 1 && atributeSection[0] == '=')
                                atributeSection = atributeSection.Slice(1); // we should probably have a better way to do this

                            // strip the quotes for both  <link="xxxx"> and <a href="...">
                            // The length need to be checked so that it is greater than 0
                            // Quotes are not mandatory for link tag and for url it isn't problematic if they aren't there unless there is a <> in the url.
                            // We would need to stop parsing from the beginning of the quote until the second one (a bit like for the noparse tags) for supporting <> character in url
                            if (atributeSection.Length >= 2 && atributeSection[0] == '"' && atributeSection[atributeSection.Length - 1] == '"')
                            {
                                value = new TagValue(atributeSection.Slice(1, atributeSection.Length - 2).ToString());
                            }
                            else
                            {
                                value = new TagValue(atributeSection.ToString());
                            }
                        }

                        result.Add(new Tag { tagType = tagType, start = start, end = end, isClosing = isClosing, value = value });

                        if (tagType == TagType.NoParse)
                        {
                            //Not uisng the real loop to skip all "malformed tags" errors.
                            if ((start = input.AsSpan(pos).IndexOf("</noparse>")) == -1)
                            {
                                break; // no closing noparse tag, no need to cleanup
                            }
                            start += pos; //The start index was relative to the span, we need to make it relative to the input
                            end = start + "</noparse>".Length;
                            result.Add(new Tag { tagType = TagType.NoParse, start = start, end = end, isClosing = true });
                            pos = end + 1;
                        }
                    }
                    else
                    {
                        if (error is not null)
                            errors?.Add(new(error, start));

                        pos = start + 1; //malformed tag, skip the '<' character
                    }
                }
                else
                {
                    if (SpanToEnum(input.AsSpan(start + 2, end - start - 2), out TagType tagType, out string? error, out var _))
                    {
                        result.Add(new Tag { tagType = tagType, start = start, end = end, isClosing = isClosing });
                    }
                    else
                    {
                        if (error is not null)
                            errors?.Add(new(error, start));

                        pos = start + 1; //malformed tag, skip the '<' character
                    }
                }


            }

            return result;
        }


        // Return a list of tags that will be applied to the text
        // previousTags can be empty to skip the allocation of a new list

        internal static List<Tag> PickResultingTags(List<Tag> allTags, string input, int atPosition, List<Tag>? applicableTags = null)
        {
            if (applicableTags == null)
            {
                applicableTags = new List<Tag>();
            }
            else
            {
                applicableTags.Clear();
            }

            int startingPos = 0; //Assument the starting position is always 0 as we do not backup the stack infos..
            // TODO incremental procesing, will have to review methods parameter and structure...
            // TOOD clean the checks belos
            
            Debug.Assert(string.IsNullOrEmpty(input) || (atPosition < input.Length && atPosition >= 0), "Invalid position");
            Debug.Assert(startingPos <= atPosition && startingPos >= 0, "Invalid starting position");

            int previousTagPosition = 0;
            foreach(var tag in allTags)
            {
                Debug.Assert(tag.start >= previousTagPosition, "Tags are not sorted");
                previousTagPosition = tag.end+1;
            }

            foreach(var tag in applicableTags)
            {
                Debug.Assert(tag.end <= startingPos, "Tag end pass the point where we should start parsing");
                Debug.Assert(allTags.Contains(tag));
            }
            Span<int?> parents = stackalloc int?[allTags.Count];
            Span<int?> lastTagOfType = stackalloc int?[TagsInfo.Length];

            int i = -1;
            foreach (var tag in allTags)
            {
                i++;
                if (tag.end < startingPos)
                {
                    continue;
                }

                if (tag.tagType == TagType.NoParse)
                {
                    continue;
                }

                if (tag.start > atPosition)
                {
                    break; // we are done.
                }


                if (tag.isClosing)
                {
                    if (lastTagOfType[(int)tag.tagType].HasValue)
                    {
                        if (parents[i].HasValue)
                        {
                            lastTagOfType[(int)tag.tagType] = parents[i];
                        }
                        else
                            lastTagOfType[(int)tag.tagType] = null;
                    }
                }
                else
                {
                    //New tag, set as last tag open and nest under parent if there was one already

                    var currentLastTagIndex = lastTagOfType[(int)tag.tagType];
                    if (currentLastTagIndex.HasValue)
                    {
                        parents[i] = currentLastTagIndex;
                    }

                    lastTagOfType[(int)tag.tagType] = i;

                }

            }

            //The order in the resulting list is important: we cannot iterate only adding lastTagOfType
            int currentTagIndex = 0;
            foreach (var tag in allTags)
            {
                var lastTag = lastTagOfType[(int)tag.tagType];
                if (lastTag.HasValue && currentTagIndex == lastTag.Value)
                    applicableTags.Add(tag);

                currentTagIndex++;
            }

            return applicableTags;
        }


        //Return a list of text setgment that will share the same text generation settings between two tags
        internal static Segment[] GenerateSegments(string input, List<Tag> tags)
        {
            List<Segment> segments = new List<Segment>();
            int afterPreviousTagEnd = 0;
            for (int i = 0; i < tags.Count; i++)
            {
                Debug.Assert(tags[i].start >= afterPreviousTagEnd);
                //If the tag is consecutive, no segment is generated
                if (tags[i].start > afterPreviousTagEnd)
                {
                    segments.Add(new Segment { start = afterPreviousTagEnd, end = tags[i].start - 1 }); // tags[i].start-1 wont be negative because afterPreviousTagEnd start at 0 and we are greater
                }
                afterPreviousTagEnd = tags[i].end + 1;
            }

            //Check if there is a segment after the last tag
            if (afterPreviousTagEnd < input.Length)
            {
                segments.Add(new Segment { start = afterPreviousTagEnd, end = input.Length - 1 });
            }

            //This is ugly, need to be able to modify a reference in a loop later
            return segments.ToArray();
        }

        internal static void ApplyStateToSegment(string input, List<Tag> tags, Segment[] segments)
        {

            //tmp list = new List<Tag>();
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].tags = PickResultingTags(tags, input, segments[i].start);
                // TODO change to the non alloc version
                //segments[i].state.tags = PickResultingTags(tags, input, segments[i].start, i>0?segments[i-1].start :0, list).Copy? ;
                //
            }

        }

        static private int AddLink(TagType type, string value, List<(int, TagType, string)> links)
        {
            foreach (var (index, listType, listValue) in links)
            {
                if (type == listType && value == listValue)
                    return index;
            }

            int nextIndex = links.Count;
            links.Add((nextIndex, type, value));

            return nextIndex;
        }

        static TextSpan CreateTextSpan(Segment segment, ref NativeTextGenerationSettings tgs, List<(int, TagType, string)> links, Color hyperlinkColor )
        {
            var textSpan = tgs.CreateTextSpan();

            if (segment.tags is null)
                return textSpan;

            for (int i = 0; i < segment.tags.Count; i++)
            {
                switch (segment.tags[i].tagType)
                {
                    //Font Style
                    case TagType.Bold:
                        textSpan.fontWeight = TextCore.Text.TextFontWeight.Bold;
                        break;
                    case TagType.Italic:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Italic;
                        break;
                    case TagType.Underline:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Underline;
                        break;
                    case TagType.Strikethrough:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Strikethrough;
                        break;
                    case TagType.Subscript:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Subscript;
                        break;
                    case TagType.Superscript:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Superscript;
                        break;
                    case TagType.AllCaps:
                    case TagType.Uppercase:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.UpperCase;
                        break;
                    case TagType.SmallCaps:
                    case TagType.Lowercase:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.LowerCase;
                        break;

                    //Color and appeareance
                    case TagType.Color:
                        textSpan.color = segment.tags[i].value!.ColorValue;
                        break;
                    case TagType.Alpha:
                        //TODO tgs.color.a = segment.state.tags[i].value.NumericalValue;
                        break;
                    case TagType.Mark:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Highlight;
                        break;

                    //Asset required
                    case TagType.Style:
                        //TODO : Add support for style
                        break;
                    case TagType.Font:
                        //TODO : Add support for font
                        break;
                    case TagType.Hyperlink:
                        textSpan.linkID = AddLink(TagType.Hyperlink, segment.tags[i].value?.StringValue ?? "", links);
                        textSpan.color = hyperlinkColor;
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Underline;
                        break;
                    case TagType.Link:
                        textSpan.linkID = AddLink(TagType.Link, segment.tags[i].value?.StringValue ?? "", links);
                        break;
                    case TagType.Sprite:
                        //TODO : Add support for sprite
                        break;

                    //Layout/Positioning
                    case TagType.Size:
                        // TODO: Add support for size
                        //textSpan.fontSize = (int)(segment.tags[i].value!.NumericalValue/64f);
                        break;
                    case TagType.CSpace:
                        //TODO : Add support for cspace
                        break;
                    case TagType.Br:
                        //TODO : Add support for br
                        break;
                    case TagType.Mspace:
                        //TODO : Add support for mspace
                        break;
                    case TagType.LineIndent:
                        //TODO : Add support for lineindent
                        break;
                    case TagType.Space:
                        //TODO : Add support for space
                        break;
                    case TagType.NoBr:
                        //TODO : Add support for nobr
                        break;
                    case TagType.Align:
                        //TODO : Add support for align
                        break;
                    case TagType.LineHeight:
                        //TODO : Add support for lineheight
                        break;


                    case TagType.NoParse://Noparse should not be reach here/Should be trimmed
                    case TagType.Unknown:
                        throw new InvalidOperationException("Invalid tag type" + segment.tags[i].tagType);

                }
            }


            return textSpan;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void CreateTextGenerationSettingsArray(ref NativeTextGenerationSettings tgs, List<(int, TagType, string)> links, Color hyperlinkColor)
        {
			links.Clear();


            var tags = FindTags(tgs.text);
            var segments = GenerateSegments(tgs.text, tags);
            ApplyStateToSegment(tgs.text, tags, segments);

            var parsedTextBuilder = new StringBuilder(tgs.text.Length);
            tgs.textSpans = new TextSpan[segments.Length];
            int parsedIndex = 0;

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                string segmentText = tgs.text.Substring(segment.start, segment.end + 1 - segment.start);

                var textSpan = CreateTextSpan(segment, ref tgs,links, hyperlinkColor );
                textSpan.startIndex = parsedIndex;
                textSpan.length = segmentText.Length;
                tgs.textSpans[i] = textSpan;
                parsedTextBuilder.Append(segmentText);
                parsedIndex += segmentText.Length;
            }

            tgs.text = parsedTextBuilder.ToString();
        }
    }
}
