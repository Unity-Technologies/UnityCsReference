// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/*
 * Simple recursive descending JSON parser and
 * JSON string builder.
 *
 * Jonas Drewsen - (C) Unity3d.com - 2010
 *
 * JSONParser parser = new JSONParser(" { \"hello\" : 42.3 } ");
 * JSONValue value = parser.Parse();
 *
 * bool is_it_float = value.isFloat();
 * float the_float = value.asFloat();
 *
 */

using System.Collections.Generic;
using System;
using UnityEngine;

namespace UnityEditorInternal
{
    /*
     * JSON value structure
     *
     * Example:
     * JSONValue v = JSONValue.NewDict();
     * v["hello"] = JSONValue.NewString("world");
     * asset(v["hello"].AsString() == "world");
     *
     */
    internal struct JSONValue
    {
        public JSONValue(object o)
        {
            data = o;
        }

        public bool IsString() { return data is string; }
        public bool IsFloat() { return data is float; }
        public bool IsList() { return data is List<JSONValue>; }
        public bool IsDict() { return data is Dictionary<string, JSONValue>; }
        public bool IsBool() { return data is bool; }
        public bool IsNull() { return data == null; }

        public static implicit operator JSONValue(string s)
        {
            return new JSONValue(s);
        }

        public static implicit operator JSONValue(float s)
        {
            return new JSONValue(s);
        }

        public static implicit operator JSONValue(bool s)
        {
            return new JSONValue(s);
        }

        public static implicit operator JSONValue(int s)
        {
            return new JSONValue((float)s);
        }

        public object AsObject()
        {
            return data;
        }

        public string AsString(bool nothrow)
        {
            if (data is string)
                return (string)data;
            if (!nothrow)
                throw new JSONTypeException("Tried to read non-string json value as string");
            return "";
        }

        public string AsString()
        {
            return AsString(false);
        }

        public float AsFloat(bool nothrow)
        {
            if (data is float)
                return (float)data;
            if (!nothrow)
                throw new JSONTypeException("Tried to read non-float json value as float");
            return 0.0f;
        }

        public float AsFloat()
        {
            return AsFloat(false);
        }

        public bool AsBool(bool nothrow)
        {
            if (data is bool)
                return (bool)data;
            if (!nothrow)
                throw new JSONTypeException("Tried to read non-bool json value as bool");
            return false;
        }

        public bool AsBool()
        {
            return AsBool(false);
        }

        public List<JSONValue> AsList(bool nothrow)
        {
            if (data is List<JSONValue>)
                return (List<JSONValue>)data;
            if (!nothrow)
                throw new JSONTypeException("Tried to read " + data.GetType().Name + " json value as list");
            return null;
        }

        public List<JSONValue> AsList()
        {
            return AsList(false);
        }

        public Dictionary<string, JSONValue> AsDict(bool nothrow)
        {
            if (data is Dictionary<string, JSONValue>)
                return (Dictionary<string, JSONValue>)data;
            if (!nothrow)
                throw new JSONTypeException("Tried to read non-dictionary json value as dictionary");
            return null;
        }

        public Dictionary<string, JSONValue> AsDict()
        {
            return AsDict(false);
        }

        public static JSONValue NewString(string val)
        {
            return new JSONValue(val);
        }

        public static JSONValue NewFloat(float val)
        {
            return new JSONValue(val);
        }

        public static JSONValue NewDict()
        {
            return new JSONValue(new Dictionary<string, JSONValue>());
        }

        public static JSONValue NewList()
        {
            return new JSONValue(new List<JSONValue>());
        }

        public static JSONValue NewBool(bool val)
        {
            return new JSONValue(val);
        }

        public static JSONValue NewNull()
        {
            return new JSONValue(null);
        }

        public JSONValue this[string index]
        {
            get
            {
                Dictionary<string, JSONValue> dict = AsDict();
                return dict[index];
            }
            set
            {
                if (data == null)
                    data = new Dictionary<string, JSONValue>();
                Dictionary<string, JSONValue> dict = AsDict();
                dict[index] = value;
            }
        }

        public bool ContainsKey(string index)
        {
            if (!IsDict())
                return false;
            return AsDict().ContainsKey(index);
        }

        // Get the specified field in a dict or null json value if
        // no such field exists. The key can point to a nested structure
        // e.g. key1.key2 in  { key1 : { key2 : 32 } }
        public JSONValue Get(string key)
        {
            if (!IsDict())
                return new JSONValue(null);
            JSONValue value = this;
            foreach (string part in key.Split('.'))
            {
                if (!value.ContainsKey(part))
                    return new JSONValue(null);
                value = value[part];
            }
            return value;
        }

        // Convenience dict value setting
        public void Set(string key, string value)
        {
            if (value == null)
            {
                this[key] = NewNull();
                return;
            }
            this[key] = NewString(value);
        }

        // Convenience dict value setting
        public void Set(string key, float value)
        {
            this[key] = NewFloat(value);
        }

        // Convenience dict value setting
        public void Set(string key, bool value)
        {
            this[key] = NewBool(value);
        }

        // Convenience list value add
        public void Add(string value)
        {
            List<JSONValue> list = AsList();
            if (value == null)
            {
                list.Add(NewNull());
                return;
            }
            list.Add(NewString(value));
        }

        // Convenience list value add
        public void Add(float value)
        {
            List<JSONValue> list = AsList();
            list.Add(NewFloat(value));
        }

        // Convenience list value add
        public void Add(bool value)
        {
            List<JSONValue> list = AsList();
            list.Add(NewBool(value));
        }

        /*
         * Serialize a JSON value to string.
         * This will recurse down through dicts and list type JSONValues.
         */
        public override string ToString()
        {
            if (IsString())
            {
                return "\"" + EncodeString(AsString()) + "\"";
            }
            else if (IsFloat())
            {
                return AsFloat().ToString();
            }
            else if (IsList())
            {
                string res = "[";
                string delim = "";
                foreach (JSONValue i in AsList())
                {
                    res += delim + i.ToString();
                    delim = ", ";
                }
                return res + "]";
            }
            else if (IsDict())
            {
                string res = "{";
                string delim = "";
                foreach (KeyValuePair<string, JSONValue> kv in AsDict())
                {
                    res += delim + '"' + EncodeString(kv.Key) + "\" : " + kv.Value.ToString();
                    delim = ", ";
                }
                return res + "}";
            }
            else if (IsBool())
            {
                return AsBool() ? "true" : "false";
            }
            else if (IsNull())
            {
                return "null";
            }
            else
            {
                throw new JSONTypeException("Cannot serialize json value of unknown type");
            }
        }

        // Encode a string into a json string
        private static string EncodeString(string str)
        {
            str = str.Replace("\"", "\\\"");
            str = str.Replace("\\", "\\\\");
            str = str.Replace("\b", "\\b");
            str = str.Replace("\f", "\\f");
            str = str.Replace("\n", "\\n");
            str = str.Replace("\r", "\\r");
            str = str.Replace("\t", "\\t");
            // We do not use \uXXXX specifier but direct unicode in the string.
            return str;
        }

        object data;
    }

    class JSONParseException : Exception
    {
        public JSONParseException(string msg) : base(msg)
        {
        }
    }

    class JSONTypeException : Exception
    {
        public JSONTypeException(string msg) : base(msg)
        {
        }
    }

    /*
     * Top down recursive JSON parser
     *
     * Example:
     * string json = "{ \"hello\" : \"world\", \"age\" : 100000, "sister" : null }";
     * JSONValue val = JSONParser.SimpleParse(json);
     * asset( val["hello"].AsString() == "world" );
     *
     */
    class JSONParser
    {
        private string json;
        private int line;
        private int linechar;
        private int len;
        private int idx;
        private int pctParsed;
        private char cur;

        public static JSONValue SimpleParse(string jsondata)
        {
            var parser = new JSONParser(jsondata);
            try
            {
                return parser.Parse();
            }
            catch (JSONParseException ex)
            {
                Debug.LogError(ex.Message);
            }
            return new JSONValue(null);
        }

        /*
         * Setup a parse to be ready for parsing the given string
         */
        public JSONParser(string jsondata)
        {
            // TODO: fix that parser needs trailing spaces;
            json = jsondata + "    ";
            line = 1;
            linechar = 1;
            len = json.Length;
            idx = 0;
            pctParsed = 0;
        }

        /*
         * Parse the entire json data string into a JSONValue structure hierarchy
         */
        public JSONValue Parse()
        {
            cur = json[idx];
            return ParseValue();
        }

        private char Next()
        {
            if (cur == '\n')
            {
                line++;
                linechar = 0;
            }
            idx++;
            if (idx >= len)
                throw new JSONParseException("End of json while parsing at " + PosMsg());

            linechar++;

            int newPct = (int)((float)idx * 100f / (float)len);
            if (newPct != pctParsed)
            {
                pctParsed = newPct;
            }
            cur = json[idx];
            return cur;
        }

        private void SkipWs()
        {
            string ws = " \n\t\r";
            while (ws.IndexOf(cur) != -1) Next();
        }

        private string PosMsg()
        {
            return "line " + line.ToString() + ", column " + linechar.ToString();
        }

        private JSONValue ParseValue()
        {
            // Skip spaces
            SkipWs();

            switch (cur)
            {
                case '[':
                    return ParseArray();
                case '{':
                    return ParseDict();
                case '"':
                    return ParseString();
                case '-':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return ParseNumber();
                case 't':
                case 'f':
                case 'n':
                    return ParseConstant();
                default:
                    throw new JSONParseException("Cannot parse json value starting with '" + json.Substring(idx, 5) + "' at " + PosMsg());
            }
        }

        private JSONValue ParseArray()
        {
            Next();
            SkipWs();
            List<JSONValue> arr = new List<JSONValue>();
            while (cur != ']')
            {
                arr.Add(ParseValue());
                SkipWs();
                if (cur == ',')
                {
                    Next();
                    SkipWs();
                }
            }
            Next();
            return new JSONValue(arr);
        }

        private JSONValue ParseDict()
        {
            Next();
            SkipWs();
            Dictionary<string, JSONValue> dict = new Dictionary<string, JSONValue>();
            while (cur != '}')
            {
                JSONValue key = ParseValue();
                if (!key.IsString())
                    throw new JSONParseException("Key not string type at " + PosMsg());
                SkipWs();
                if (cur != ':')
                    throw new JSONParseException("Missing dict entry delimiter ':' at " + PosMsg());
                Next();
                dict.Add(key.AsString(), ParseValue());
                SkipWs();
                if (cur == ',')
                {
                    Next();
                    SkipWs();
                }
            }
            Next();
            return new JSONValue(dict);
        }

        static char[] endcodes = new char[] { '\\', '"' };

        private JSONValue ParseString()
        {
            string res = "";

            Next();

            while (idx < len)
            {
                int endidx = json.IndexOfAny(endcodes, idx);
                if (endidx < 0)
                    throw new JSONParseException("missing '\"' to end string at " + PosMsg());

                res += json.Substring(idx, endidx - idx);

                if (json[endidx] == '"')
                {
                    cur = json[endidx];
                    idx = endidx;
                    break;
                }

                endidx++; // get escape code
                if (endidx >= len)
                    throw new JSONParseException("End of json while parsing while parsing string at " + PosMsg());

                // char at endidx is \
                char ncur = json[endidx];
                switch (ncur)
                {
                    case '"':
                        goto case '/';
                    case '\\':
                        goto case '/';
                    case '/':
                        res += ncur;
                        break;
                    case 'b':
                        res += '\b';
                        break;
                    case 'f':
                        res += '\f';
                        break;
                    case 'n':
                        res += '\n';
                        break;
                    case 'r':
                        res += '\r';
                        break;
                    case 't':
                        res += '\t';
                        break;
                    case 'u':
                        // Unicode char specified by 4 hex digits
                        string digit = "";
                        if (endidx + 4 >= len)
                            throw new JSONParseException("End of json while parsing while parsing unicode char near " + PosMsg());
                        digit += json[endidx + 1];
                        digit += json[endidx + 2];
                        digit += json[endidx + 3];
                        digit += json[endidx + 4];
                        try
                        {
                            int d = System.Int32.Parse(digit, System.Globalization.NumberStyles.AllowHexSpecifier);
                            res += (char)d;
                        }
                        catch (FormatException)
                        {
                            throw new JSONParseException("Invalid unicode escape char near " + PosMsg());
                        }
                        endidx += 4;
                        break;
                    default:
                        throw new JSONParseException("Invalid escape char '" + ncur + "' near " + PosMsg());
                }
                idx = endidx + 1;
            }
            if (idx >= len)
                throw new JSONParseException("End of json while parsing while parsing string near " + PosMsg());

            cur = json[idx];

            Next();
            return new JSONValue(res);
        }

        private JSONValue ParseNumber()
        {
            string resstr = "";

            if (cur == '-')
            {
                resstr = "-";
                Next();
            }

            while (cur >= '0' && cur <= '9')
            {
                resstr += cur;
                Next();
            }
            if (cur == '.')
            {
                Next();
                resstr += '.';
                while (cur >= '0' && cur <= '9')
                {
                    resstr += cur;
                    Next();
                }
            }

            if (cur == 'e' || cur == 'E')
            {
                resstr += "e";
                Next();
                if (cur != '-' && cur != '+')
                {
                    // throw new JSONParseException("Missing - or + in 'e' potent specifier at " + PosMsg());
                    resstr += cur;
                    Next();
                }
                while (cur >= '0' && cur <= '9')
                {
                    resstr += cur;
                    Next();
                }
            }

            try
            {
                float f = System.Convert.ToSingle(resstr);
                return new JSONValue(f);
            }
            catch (Exception)
            {
                throw new JSONParseException("Cannot convert string to float : '" + resstr + "' at " + PosMsg());
            }
        }

        private JSONValue ParseConstant()
        {
            string c = "";
            c = "" + cur + Next() + Next() + Next();
            Next();
            if (c == "true")
            {
                return new JSONValue(true);
            }
            else if (c == "fals")
            {
                if (cur == 'e')
                {
                    Next();
                    return new JSONValue(false);
                }
            }
            else if (c == "null")
            {
                return new JSONValue(null);
            }
            throw new JSONParseException("Invalid token at " + PosMsg());
        }
    }
}
