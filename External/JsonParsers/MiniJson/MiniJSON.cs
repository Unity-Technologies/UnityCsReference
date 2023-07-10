/*
 * Copyright (c) 2013 Calvin Rien
 *
 * Based on the JSON parser by Patrick van Bergen
 * http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html
 *
 * Simplified it so that it doesn't throw exceptions
 * and can be used in Unity iPhone with maximum code stripping.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;

namespace UnityEditor
{
	// Example usage:
	//
	//  using UnityEngine;
	//  using UnityEditor;
	//  using System.Collections;
	//  using System.Collections.Generic;
	//
	//  public class MiniJSONTest : MonoBehaviour {
	//      void Start () {
	//          var jsonString = "{ \"array\": [1.44,2,3], " +
	//                          "\"object\": {\"key1\":\"value1\", \"key2\":256}, " +
	//                          "\"string\": \"The quick brown fox \\\"jumps\\\" over the lazy dog \", " +
	//                          "\"unicode\": \"\\u3041 Men\u00fa sesi\u00f3n\", " +
	//                          "\"int\": 65536, " +
	//                          "\"float\": 3.1415926, " +
	//                          "\"bool\": true, " +
	//                          "\"null\": null }";
	//
	//          var dict = Json.Deserialize (jsonString) as Dictionary<string,object>;
	//
	//          Debug.Log ("deserialized: " + dict.GetType ());
	//          Debug.Log ("dict['array'][0]: " + ((List<object>) dict["array"])[0]);
	//          Debug.Log ("dict['string']: " + (string) dict["string"]);
	//          Debug.Log ("dict['float']: " + (double) dict["float"]); // floats come out as doubles
	//          Debug.Log ("dict['int']: " + (long) dict["int"]); // ints come out as longs
	//          Debug.Log ("dict['unicode']: " + (string) dict["unicode"]);
	//
	//          var str = Json.Serialize (dict);
	//
	//          Debug.Log ("serialized: " + str);
	//      }
	//  }

	/// <summary>
	/// This class encodes and decodes JSON strings.
	/// Spec. details, see http://www.json.org/
	///
	/// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
	/// All numbers are parsed to doubles.
	/// </summary>
	internal static class Json
	{
		/// <summary>
		/// Parses the string json into a value
		/// </summary>
		/// <param name="json">A JSON string.</param>
		/// <returns>An List&lt;object&gt;, a Dictionary&lt;string, object&gt;, a double, an integer,a string, null, true, or false</returns>
		public static object Deserialize (string json)
		{
			// save the string for debug information
			if (json == null)
			{
				return null;
			}

			return Parser.Parse (json);
		}

		sealed class Parser : IDisposable
		{
			const string WORD_BREAK = "{}[],:\"";

			public static bool IsWordBreak (char c)
			{
				return Char.IsWhiteSpace (c) || WORD_BREAK.IndexOf (c) != -1;
			}

			enum TOKEN
			{
				NONE,
				CURLY_OPEN,
				CURLY_CLOSE,
				SQUARED_OPEN,
				SQUARED_CLOSE,
				COLON,
				COMMA,
				STRING,
				NUMBER,
				TRUE,
				FALSE,
				NULL
			}

			StringReader json;

			Parser (string jsonString)
			{
				json = new StringReader (jsonString);
			}

			public static object Parse (string jsonString)
			{
				using (var instance = new Parser (jsonString))
				{
					return instance.ParseValue ();
				}
			}

			public void Dispose ()
			{
				json.Dispose ();
				json = null;
			}

			Dictionary<string, object> ParseObject ()
			{
				Dictionary<string, object> table = new Dictionary<string, object> ();

				// ditch opening brace
				json.Read ();

				// {
				while (true)
				{
					switch (NextToken)
					{
						case TOKEN.NONE:
							return null;
						case TOKEN.COMMA:
							continue;
						case TOKEN.CURLY_CLOSE:
							return table;
						default:
							// name
							string name = ParseString ();
							if (name == null)
							{
								return null;
							}

							// :
							if (NextToken != TOKEN.COLON)
							{
								return null;
							}
							// ditch the colon
							json.Read ();

							// value
							table[name] = ParseValue ();
							break;
					}
				}
			}

			List<object> ParseArray ()
			{
				List<object> array = new List<object> ();

				// ditch opening bracket
				json.Read ();

				// [
				var parsing = true;
				while (parsing)
				{
					TOKEN nextToken = NextToken;

					switch (nextToken)
					{
						case TOKEN.NONE:
							return null;
						case TOKEN.COMMA:
							continue;
						case TOKEN.SQUARED_CLOSE:
							parsing = false;
							break;
						default:
							object value = ParseByToken (nextToken);

							array.Add (value);
							break;
					}
				}

				return array;
			}

			object ParseValue ()
			{
				TOKEN nextToken = NextToken;
				return ParseByToken (nextToken);
			}

			object ParseByToken (TOKEN token)
			{
				switch (token)
				{
					case TOKEN.STRING:
						return ParseString ();
					case TOKEN.NUMBER:
						return ParseNumber ();
					case TOKEN.CURLY_OPEN:
						return ParseObject ();
					case TOKEN.SQUARED_OPEN:
						return ParseArray ();
					case TOKEN.TRUE:
						return true;
					case TOKEN.FALSE:
						return false;
					case TOKEN.NULL:
						return null;
					default:
						return null;
				}
			}

			string ParseString ()
			{
				StringBuilder s = new StringBuilder ();
				char c;

				// ditch opening quote
				json.Read ();

				bool parsing = true;
				while (parsing)
				{

					if (json.Peek () == -1)
					{
						parsing = false;
						break;
					}

					c = NextChar;
					switch (c)
					{
						case '"':
							parsing = false;
							break;
						case '\\':
							if (json.Peek () == -1)
							{
								parsing = false;
								break;
							}

							c = NextChar;
							switch (c)
							{
								case '"':
								case '\\':
								case '/':
									s.Append (c);
									break;
								case 'b':
									s.Append ('\b');
									break;
								case 'f':
									s.Append ('\f');
									break;
								case 'n':
									s.Append ('\n');
									break;
								case 'r':
									s.Append ('\r');
									break;
								case 't':
									s.Append ('\t');
									break;
								case 'u':
									var hex = new char[4];

									for (int i=0; i< 4; i++)
									{
										hex[i] = NextChar;
									}

									s.Append ((char) Convert.ToInt32 (new string (hex), 16));
									break;
							}
							break;
						default:
							s.Append (c);
							break;
					}
				}

				return s.ToString ();
			}

			object ParseNumber ()
			{
				string number = NextWord;

                if (Int64.TryParse(number, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedInt))
				{
					return parsedInt;
				}

                Double.TryParse(number, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedDouble);
                return parsedDouble;
            }

            void EatWhitespace ()
			{
				while (Char.IsWhiteSpace (PeekChar))
				{
					json.Read ();

					if (json.Peek () == -1)
					{
						break;
					}
				}
			}

			char PeekChar
			{
				get
				{
					return Convert.ToChar (json.Peek ());
				}
			}

			char NextChar
			{
				get
				{
					return Convert.ToChar (json.Read ());
				}
			}

			string NextWord
			{
				get
				{
					StringBuilder word = new StringBuilder ();

					while (!IsWordBreak (PeekChar))
					{
						word.Append (NextChar);

						if (json.Peek () == -1)
						{
							break;
						}
					}

					return word.ToString ();
				}
			}

			TOKEN NextToken
			{
				get
				{
					EatWhitespace ();

					if (json.Peek () == -1)
					{
						return TOKEN.NONE;
					}

					switch (PeekChar)
					{
						case '{':
							return TOKEN.CURLY_OPEN;
						case '}':
							json.Read ();
							return TOKEN.CURLY_CLOSE;
						case '[':
							return TOKEN.SQUARED_OPEN;
						case ']':
							json.Read ();
							return TOKEN.SQUARED_CLOSE;
						case ',':
							json.Read ();
							return TOKEN.COMMA;
						case '"':
							return TOKEN.STRING;
						case ':':
							return TOKEN.COLON;
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
						case '-':
							return TOKEN.NUMBER;
					}

					switch (NextWord)
					{
						case "false":
							return TOKEN.FALSE;
						case "true":
							return TOKEN.TRUE;
						case "null":
							return TOKEN.NULL;
					}

					return TOKEN.NONE;
				}
			}
		}

		/// <summary>
		/// Converts a IDictionary / IList object or a simple type (string, int, etc.) into a JSON string
		/// </summary>
		/// <param name="json">A Dictionary&lt;string, object&gt; / List&lt;object&gt;</param>
		/// <param name="pretty">A boolean to indicate whether or not JSON should be prettified, default is false.</param>
		/// <param name="indentText">A string to ibe used as indentText, default is 2 spaces.</param>
		/// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
		public static string Serialize (object obj, bool pretty = false, string indentText = "  ")
		{
			return Serializer.Serialize (obj, pretty, indentText);
		}

		sealed class Serializer
		{
			readonly string indentText;
			bool pretty;
			StringBuilder builder;

			Serializer (bool pretty, string indentText)
			{
				builder = new StringBuilder ();
				this.pretty = pretty;
				this.indentText = indentText;
			}

			public static string Serialize (object obj, bool pretty, string indentText)
			{
				var instance = new Serializer (pretty, indentText);

				instance.SerializeValue (obj, 0);

				return instance.builder.ToString ();
			}

			void SerializeValue (object value, int indent)
			{
				IList asList;
				IDictionary asDict;
				string asStr;

				if (value == null || value is Delegate)
				{
					builder.Append ("null");
				}
				else if ((asStr = value as string) != null)
				{
					SerializeString (asStr);
				}
				else if (value is bool)
				{
					builder.Append ((bool) value ? "true" : "false");
				}
				else if ((asList = value as IList) != null)
				{
					SerializeArray (asList, indent);
				}
				else if ((asDict = value as IDictionary) != null)
				{
					SerializeObject (asDict, indent);
				}
				else if (value is char)
				{
					SerializeString (new string ((char) value, 1));
				}
				else
				{
					SerializeOther (value, indent);
				}
			}

			void SerializeObject (IDictionary obj, int indent)
			{
				if (pretty && obj.Keys.Count == 0)
				{
						builder.Append("{}");
						return;
				}

				bool first = true;
				string indentLine = null;

				builder.Append ('{');
				if (pretty)
				{
					builder.Append('\n');
					indentLine = string.Concat(Enumerable.Repeat(indentText, indent).ToArray());
				}

				foreach (object e in obj.Keys)
				{
					if (!first)
					{
						builder.Append (',');
						if (pretty)
							builder.Append ('\n');
					}

					if (pretty)
					{
						builder.Append(indentLine);
						builder.Append(indentText);
					}

					SerializeString (e.ToString ());
					builder.Append (':');
					if (pretty)
						builder.Append (' ');

					SerializeValue (obj[e], indent + 1);

					first = false;
				}

				if (pretty)
				{
					builder.Append('\n');
					builder.Append(indentLine);
				}
				builder.Append ('}');
			}

			void SerializeArray (IList anArray, int indent)
			{
				if (pretty && anArray.Count == 0)
				{
						builder.Append("[]");
						return;
				}

				bool first = true;
				string indentLine = null;

				builder.Append ('[');
				if (pretty)
				{
					builder.Append('\n');
					indentLine = string.Concat(Enumerable.Repeat(indentText, indent).ToArray());
				}

				foreach (object obj in anArray)
				{
					if (!first)
					{
						builder.Append (',');
						if (pretty)
							builder.Append ('\n');
					}

					if (pretty)
					{
						builder.Append(indentLine);
						builder.Append(indentText);
					}

					SerializeValue (obj, indent + 1);

					first = false;
				}

				if (pretty)
				{
					builder.Append('\n');
					builder.Append(indentLine);
				}
				builder.Append (']');
			}

			void SerializeString (string str)
			{
				builder.Append ('\"');

				char[] charArray = str.ToCharArray ();
				foreach (var c in charArray)
				{
					switch (c)
					{
						case '"':
							builder.Append ("\\\"");
							break;
						case '\\':
							builder.Append ("\\\\");
							break;
						case '\b':
							builder.Append ("\\b");
							break;
						case '\f':
							builder.Append ("\\f");
							break;
						case '\n':
							builder.Append ("\\n");
							break;
						case '\r':
							builder.Append ("\\r");
							break;
						case '\t':
							builder.Append ("\\t");
							break;
						default:
							int codepoint = Convert.ToInt32 (c);
							if ((codepoint >= 32) && (codepoint <= 126))
							{
								builder.Append (c);
							}
							else
							{
								builder.Append ("\\u");
								builder.Append (codepoint.ToString ("x4"));
							}
							break;
					}
				}

				builder.Append ('\"');
			}

			void SerializeOther (object value, int indent)
			{
				// NOTE: decimals lose precision during serialization.
				// They always have, I'm just letting you know.
				// Previously floats and doubles lost precision too.
				if (value is float)
				{
					builder.Append (((float) value).ToString ("R", System.Globalization.CultureInfo.InvariantCulture));
				}
				else if (value is int || value is uint || value is long || value is sbyte || value is byte || value is short || value is ushort || value is ulong)
				{
					builder.Append (value);
				}
				else if (value is double || value is decimal)
				{
					builder.Append (Convert.ToDouble (value).ToString ("R", System.Globalization.CultureInfo.InvariantCulture));
				}
				else
				{
					Dictionary<string, object> map = new Dictionary<string, object> ();
					List<FieldInfo> fields = value.GetType ().GetFields (BindingFlags.Instance | BindingFlags.Public).ToList ();
					foreach (FieldInfo field in fields)
					{
						map.Add (field.Name, field.GetValue (value));
					}
					List<PropertyInfo> properties = value.GetType ().GetProperties (BindingFlags.Instance | BindingFlags.Public).ToList ();
					foreach (PropertyInfo property in properties)
					{
						map.Add (property.Name, property.GetValue (value, null));
					}
					SerializeObject (map, indent);
				}
			}
		}
	}
}
