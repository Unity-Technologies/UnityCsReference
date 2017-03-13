//-----------------------------------------------------------------------
// <copyright file="SimpleJson.cs" company="The Outercurve Foundation">
//    Copyright (c) 2011, The Outercurve Foundation.
//
//    Licensed under the MIT License (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.opensource.org/licenses/mit-license.php
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// <author>Nathan Totten (ntotten.com), Jim Zimmerman (jimzimmerman.com) and Prabir Shrestha (prabir.me)</author>
// <website>https://github.com/facebook-csharp-sdk/simple-json</website>
//-----------------------------------------------------------------------

// VERSION:

// NOTE: uncomment the following line to make SimpleJson class internal.

// NOTE: uncomment the following line to make JsonArray and JsonObject class internal.

// NOTE: uncomment the following line to enable dynamic support.
//#define SIMPLE_JSON_DYNAMIC

// NOTE: uncomment the following line to enable DataContract support.
//#define SIMPLE_JSON_DATACONTRACT

// NOTE: uncomment the following line to disable linq expressions/compiled lambda (better performance) instead of method.invoke().
// define if you are using .net framework <= 3.0 or < WP7.5

// NOTE: uncomment the following line if you are compiling under Window Metro style application/library.
// usually already defined in properties
//#define NETFX_CORE;

// original json parsing code from http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html


using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using SimpleJson.Reflection;

// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable SuggestUseVarKeywordEvident
namespace SimpleJson
{
/// <summary>
/// Represents the json array.
/// </summary>
[GeneratedCode ("simple-json", "1.0.0")]
[EditorBrowsable (EditorBrowsableState.Never)]
[SuppressMessage ("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
internal
class JsonArray : List<object>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JsonArray"/> class.
	/// </summary>
	public JsonArray () { }

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonArray"/> class.
	/// </summary>
	/// <param name="capacity">The capacity of the json array.</param>
	public JsonArray (int capacity) : base (capacity) { }

	/// <summary>
	/// The json representation of the array.
	/// </summary>
	/// <returns>The json representation of the array.</returns>
	public override string ToString ()
	{
		return SimpleJson.SerializeObject (this) ?? string.Empty;
	}
}

/// <summary>
/// Represents the json object.
/// </summary>
[GeneratedCode ("simple-json", "1.0.0")]
[EditorBrowsable (EditorBrowsableState.Never)]
[SuppressMessage ("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
internal
class JsonObject :
	IDictionary<string, object>
{
	/// <summary>
	/// The internal member dictionary.
	/// </summary>
	private readonly Dictionary<string, object> _members;

	/// <summary>
	/// Initializes a new instance of <see cref="JsonObject"/>.
	/// </summary>
	public JsonObject ()
	{
		_members = new Dictionary<string, object> ();
	}

	/// <summary>
	/// Initializes a new instance of <see cref="JsonObject"/>.
	/// </summary>
	/// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1"/> implementation to use when comparing keys, or null to use the default <see cref="T:System.Collections.Generic.EqualityComparer`1"/> for the type of the key.</param>
	public JsonObject (IEqualityComparer<string> comparer)
	{
		_members = new Dictionary<string, object> (comparer);
	}

	/// <summary>
	/// Gets the <see cref="System.Object"/> at the specified index.
	/// </summary>
	/// <value></value>
	public object this[int index]
	{
		get
		{
			return GetAtIndex (_members, index);
		}
	}

	internal static object GetAtIndex (IDictionary<string, object> obj, int index)
	{
		if (obj == null)
			throw new ArgumentNullException ("obj");
		if (index >= obj.Count)
			throw new ArgumentOutOfRangeException ("index");
		int i = 0;
		foreach (KeyValuePair<string, object> o in obj)
			if (i++ == index) return o.Value;
		return null;
	}

	/// <summary>
	/// Adds the specified key.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="value">The value.</param>
	public void Add (string key, object value)
	{
		_members.Add (key, value);
	}

	/// <summary>
	/// Determines whether the specified key contains key.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>
	///     <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
	/// </returns>
	public bool ContainsKey (string key)
	{
		return _members.ContainsKey (key);
	}

	/// <summary>
	/// Gets the keys.
	/// </summary>
	/// <value>The keys.</value>
	public ICollection<string> Keys
	{
		get
		{
			return _members.Keys;
		}
	}

	/// <summary>
	/// Removes the specified key.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns></returns>
	public bool Remove (string key)
	{
		return _members.Remove (key);
	}

	/// <summary>
	/// Tries the get value.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="value">The value.</param>
	/// <returns></returns>
	public bool TryGetValue (string key, out object value)
	{
		return _members.TryGetValue (key, out value);
	}

	/// <summary>
	/// Gets the values.
	/// </summary>
	/// <value>The values.</value>
	public ICollection<object> Values
	{
		get
		{
			return _members.Values;
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="System.Object"/> with the specified key.
	/// </summary>
	/// <value></value>
	public object this[string key]
	{
		get
		{
			return _members[key];
		}
		set
		{
			_members[key] = value;
		}
	}

	/// <summary>
	/// Adds the specified item.
	/// </summary>
	/// <param name="item">The item.</param>
	public void Add (KeyValuePair<string, object> item)
	{
		_members.Add (item.Key, item.Value);
	}

	/// <summary>
	/// Clears this instance.
	/// </summary>
	public void Clear ()
	{
		_members.Clear ();
	}

	/// <summary>
	/// Determines whether [contains] [the specified item].
	/// </summary>
	/// <param name="item">The item.</param>
	/// <returns>
	///		<c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.
	/// </returns>
	public bool Contains (KeyValuePair<string, object> item)
	{
		return _members.ContainsKey (item.Key) && _members[item.Key] == item.Value;
	}

	/// <summary>
	/// Copies to.
	/// </summary>
	/// <param name="array">The array.</param>
	/// <param name="arrayIndex">Index of the array.</param>
	public void CopyTo (KeyValuePair<string, object>[] array, int arrayIndex)
	{
		if (array == null) throw new ArgumentNullException ("array");
		int num = Count;
		foreach (KeyValuePair<string, object> kvp in this)
		{
			array[arrayIndex++] = kvp;
			if (--num <= 0)
				return;
		}
	}

	/// <summary>
	/// Gets the count.
	/// </summary>
	/// <value>The count.</value>
	public int Count
	{
		get
		{
			return _members.Count;
		}
	}

	/// <summary>
	/// Gets a value indicating whether this instance is read only.
	/// </summary>
	/// <value>
	///		<c>true</c> if this instance is read only; otherwise, <c>false</c>.
	/// </value>
	public bool IsReadOnly
	{
		get
		{
			return false;
		}
	}

	/// <summary>
	/// Removes the specified item.
	/// </summary>
	/// <param name="item">The item.</param>
	/// <returns></returns>
	public bool Remove (KeyValuePair<string, object> item)
	{
		return _members.Remove (item.Key);
	}

	/// <summary>
	/// Gets the enumerator.
	/// </summary>
	/// <returns></returns>
	public IEnumerator<KeyValuePair<string, object>> GetEnumerator ()
	{
		return _members.GetEnumerator ();
	}

	/// <summary>
	/// Returns an enumerator that iterates through a collection.
	/// </summary>
	/// <returns>
	/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
	/// </returns>
	IEnumerator IEnumerable.GetEnumerator ()
	{
		return _members.GetEnumerator ();
	}

	/// <summary>
	/// Returns a json <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
	/// </summary>
	/// <returns>
	/// A json <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
	/// </returns>
	public override string ToString ()
	{
		return SimpleJson.SerializeObject (this);
	}

}
}

namespace SimpleJson
{
/// <summary>
/// This class encodes and decodes JSON strings.
/// Spec. details, see http://www.json.org/
///
/// JSON uses Arrays and Objects. These correspond here to the datatypes JsonArray(IList&lt;object>) and JsonObject(IDictionary&lt;string,object>).
/// All numbers are parsed to doubles.
/// </summary>
[GeneratedCode ("simple-json", "1.0.0")]
internal
static class SimpleJson
{
	private const int TOKEN_NONE = 0;
	private const int TOKEN_CURLY_OPEN = 1;
	private const int TOKEN_CURLY_CLOSE = 2;
	private const int TOKEN_SQUARED_OPEN = 3;
	private const int TOKEN_SQUARED_CLOSE = 4;
	private const int TOKEN_COLON = 5;
	private const int TOKEN_COMMA = 6;
	private const int TOKEN_STRING = 7;
	private const int TOKEN_NUMBER = 8;
	private const int TOKEN_TRUE = 9;
	private const int TOKEN_FALSE = 10;
	private const int TOKEN_NULL = 11;
	private const int BUILDER_CAPACITY = 2000;

	/// <summary>
	/// Parses the string json into a value
	/// </summary>
	/// <param name="json">A JSON string.</param>
	/// <returns>An IList&lt;object>, a IDictionary&lt;string,object>, a double, a string, null, true, or false</returns>
	public static object DeserializeObject (string json)
	{
		object obj;
		if (TryDeserializeObject (json, out obj))
			return obj;
		throw new SerializationException ("Invalid JSON string");
	}

	/// <summary>
	/// Try parsing the json string into a value.
	/// </summary>
	/// <param name="json">
	/// A JSON string.
	/// </param>
	/// <param name="obj">
	/// The object.
	/// </param>
	/// <returns>
	/// Returns true if successfull otherwise false.
	/// </returns>
	[SuppressMessage ("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate", Justification="Need to support .NET 2")]
	public static bool TryDeserializeObject (string json, out object obj)
	{
		bool success = true;
		if (json != null)
		{
			char[] charArray = json.ToCharArray ();
			int index = 0;
			obj = ParseValue (charArray, ref index, ref success);
		}
		else
			obj = null;

		return success;
	}

	public static object DeserializeObject (string json, Type type, IJsonSerializerStrategy jsonSerializerStrategy)
	{
		object jsonObject = DeserializeObject (json);
		return type == null || jsonObject != null && ReflectionUtils.IsAssignableFrom (jsonObject.GetType (), type)
		       ? jsonObject
		       : (jsonSerializerStrategy ?? CurrentJsonSerializerStrategy).DeserializeObject (jsonObject, type);
	}

	public static object DeserializeObject (string json, Type type)
	{
		return DeserializeObject (json, type, null);
	}

	public static T DeserializeObject<T> (string json, IJsonSerializerStrategy jsonSerializerStrategy)
	{
		return (T)DeserializeObject (json, typeof (T), jsonSerializerStrategy);
	}

	public static T DeserializeObject<T> (string json)
	{
		return (T)DeserializeObject (json, typeof (T), null);
	}

	/// <summary>
	/// Converts a IDictionary&lt;string,object> / IList&lt;object> object into a JSON string
	/// </summary>
	/// <param name="json">A IDictionary&lt;string,object> / IList&lt;object></param>
	/// <param name="jsonSerializerStrategy">Serializer strategy to use</param>
	/// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
	public static string SerializeObject (object json, IJsonSerializerStrategy jsonSerializerStrategy)
	{
		StringBuilder builder = new StringBuilder (BUILDER_CAPACITY);
		bool success = SerializeValue (jsonSerializerStrategy, json, builder);
		return (success ? builder.ToString () : null);
	}

	public static string SerializeObject (object json)
	{
		return SerializeObject (json, CurrentJsonSerializerStrategy);
	}

	public static string EscapeToJavascriptString (string jsonString)
	{
		if (string.IsNullOrEmpty (jsonString))
			return jsonString;

		StringBuilder sb = new StringBuilder ();
		char c;

		for (int i = 0; i < jsonString.Length; )
		{
			c = jsonString[i++];

			if (c == '\\')
			{
				int remainingLength = jsonString.Length - i;
				if (remainingLength >= 2)
				{
					char lookahead = jsonString[i];
					if (lookahead == '\\')
					{
						sb.Append ('\\');
						++i;
					}
					else if (lookahead == '"')
					{
						sb.Append ("\"");
						++i;
					}
					else if (lookahead == 't')
					{
						sb.Append ('\t');
						++i;
					}
					else if (lookahead == 'b')
					{
						sb.Append ('\b');
						++i;
					}
					else if (lookahead == 'n')
					{
						sb.Append ('\n');
						++i;
					}
					else if (lookahead == 'r')
					{
						sb.Append ('\r');
						++i;
					}
				}
			}
			else
			{
				sb.Append (c);
			}
		}
		return sb.ToString ();
	}

	static IDictionary<string, object> ParseObject (char[] json, ref int index, ref bool success)
	{
		IDictionary<string, object> table = new JsonObject ();
		int token;

		// {
		NextToken (json, ref index);

		bool done = false;
		while (!done)
		{
			token = LookAhead (json, index);
			if (token == TOKEN_NONE)
			{
				success = false;
				return null;
			}
			else if (token == TOKEN_COMMA)
				NextToken (json, ref index);
			else if (token == TOKEN_CURLY_CLOSE)
			{
				NextToken (json, ref index);
				return table;
			}
			else
			{
				// name
				string name = ParseString (json, ref index, ref success);
				if (!success)
				{
					success = false;
					return null;
				}
				// :
				token = NextToken (json, ref index);
				if (token != TOKEN_COLON)
				{
					success = false;
					return null;
				}
				// value
				object value = ParseValue (json, ref index, ref success);
				if (!success)
				{
					success = false;
					return null;
				}
				table[name] = value;
			}
		}
		return table;
	}

	static JsonArray ParseArray (char[] json, ref int index, ref bool success)
	{
		JsonArray array = new JsonArray ();

		// [
		NextToken (json, ref index);

		bool done = false;
		while (!done)
		{
			int token = LookAhead (json, index);
			if (token == TOKEN_NONE)
			{
				success = false;
				return null;
			}
			else if (token == TOKEN_COMMA)
				NextToken (json, ref index);
			else if (token == TOKEN_SQUARED_CLOSE)
			{
				NextToken (json, ref index);
				break;
			}
			else
			{
				object value = ParseValue (json, ref index, ref success);
				if (!success)
					return null;
				array.Add (value);
			}
		}
		return array;
	}

	static object ParseValue (char[] json, ref int index, ref bool success)
	{
		switch (LookAhead (json, index))
		{
		case TOKEN_STRING:
			return ParseString (json, ref index, ref success);
		case TOKEN_NUMBER:
			return ParseNumber (json, ref index, ref success);
		case TOKEN_CURLY_OPEN:
			return ParseObject (json, ref index, ref success);
		case TOKEN_SQUARED_OPEN:
			return ParseArray (json, ref index, ref success);
		case TOKEN_TRUE:
			NextToken (json, ref index);
			return true;
		case TOKEN_FALSE:
			NextToken (json, ref index);
			return false;
		case TOKEN_NULL:
			NextToken (json, ref index);
			return null;
		case TOKEN_NONE:
			break;
		}
		success = false;
		return null;
	}

	static string ParseString (char[] json, ref int index, ref bool success)
	{
		StringBuilder s = new StringBuilder (BUILDER_CAPACITY);
		char c;

		EatWhitespace (json, ref index);

		// "
		c = json[index++];
		bool complete = false;
		while (!complete)
		{
			if (index == json.Length)
				break;

			c = json[index++];
			if (c == '"')
			{
				complete = true;
				break;
			}
			else if (c == '\\')
			{
				if (index == json.Length)
					break;
				c = json[index++];
				if (c == '"')
					s.Append ('"');
				else if (c == '\\')
					s.Append ('\\');
				else if (c == '/')
					s.Append ('/');
				else if (c == 'b')
					s.Append ('\b');
				else if (c == 'f')
					s.Append ('\f');
				else if (c == 'n')
					s.Append ('\n');
				else if (c == 'r')
					s.Append ('\r');
				else if (c == 't')
					s.Append ('\t');
				else if (c == 'u')
				{
					int remainingLength = json.Length - index;
					if (remainingLength >= 4)
					{
						// parse the 32 bit hex into an integer codepoint
						uint codePoint;
						if (! (success = UInt32.TryParse (new string (json, index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint)))
							return "";

						// convert the integer codepoint to a unicode char and add to string
						if (0xD800 <= codePoint && codePoint <= 0xDBFF)  // if high surrogate
						{
							index += 4; // skip 4 chars
							remainingLength = json.Length - index;
							if (remainingLength >= 6)
							{
								uint lowCodePoint;
								if (new string (json, index, 2) == "\\u" && UInt32.TryParse (new string (json, index + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out lowCodePoint))
								{
									if (0xDC00 <= lowCodePoint && lowCodePoint <= 0xDFFF)    // if low surrogate
									{
										s.Append ((char)codePoint);
										s.Append ((char)lowCodePoint);
										index += 6; // skip 6 chars
										continue;
									}
								}
							}
							success = false;    // invalid surrogate pair
							return "";
						}
						s.Append (ConvertFromUtf32 ((int)codePoint));
						// skip 4 chars
						index += 4;
					}
					else
						break;
				}
			}
			else
				s.Append (c);
		}
		if (!complete)
		{
			success = false;
			return null;
		}
		return s.ToString ();
	}

	private static string ConvertFromUtf32 (int utf32)
	{
		// http://www.java2s.com/Open-Source/CSharp/2.6.4-mono-.net-core/System/System/Char.cs.htm
		if (utf32 < 0 || utf32 > 0x10FFFF)
			throw new ArgumentOutOfRangeException ("utf32", "The argument must be from 0 to 0x10FFFF.");
		if (0xD800 <= utf32 && utf32 <= 0xDFFF)
			throw new ArgumentOutOfRangeException ("utf32", "The argument must not be in surrogate pair range.");
		if (utf32 < 0x10000)
			return new string ((char)utf32, 1);
		utf32 -= 0x10000;
		return new string (new char[] { (char) ((utf32 >> 10) + 0xD800), (char) (utf32 % 0x0400 + 0xDC00) });
	}

	static object ParseNumber (char[] json, ref int index, ref bool success)
	{
		EatWhitespace (json, ref index);
		int lastIndex = GetLastIndexOfNumber (json, index);
		int charLength = (lastIndex - index) + 1;
		object returnNumber;
		string str = new string (json, index, charLength);
		if (str.IndexOf (".", StringComparison.OrdinalIgnoreCase) != -1 || str.IndexOf ("e", StringComparison.OrdinalIgnoreCase) != -1)
		{
			double number;
			success = double.TryParse (new string (json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
			returnNumber = number;
		}
		else
		{
			long number;
			success = long.TryParse (new string (json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
			returnNumber = number;
		}
		index = lastIndex + 1;
		return returnNumber;
	}

	static int GetLastIndexOfNumber (char[] json, int index)
	{
		int lastIndex;
		for (lastIndex = index; lastIndex < json.Length; lastIndex++)
			if ("0123456789+-.eE".IndexOf (json[lastIndex]) == -1) break;
		return lastIndex - 1;
	}

	static void EatWhitespace (char[] json, ref int index)
	{
		for (; index < json.Length; index++)
			if (" \t\n\r\b\f".IndexOf (json[index]) == -1) break;
	}

	static int LookAhead (char[] json, int index)
	{
		int saveIndex = index;
		return NextToken (json, ref saveIndex);
	}

	[SuppressMessage ("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
	static int NextToken (char[] json, ref int index)
	{
		EatWhitespace (json, ref index);
		if (index == json.Length)
			return TOKEN_NONE;
		char c = json[index];
		index++;
		switch (c)
		{
		case '{':
			return TOKEN_CURLY_OPEN;
		case '}':
			return TOKEN_CURLY_CLOSE;
		case '[':
			return TOKEN_SQUARED_OPEN;
		case ']':
			return TOKEN_SQUARED_CLOSE;
		case ',':
			return TOKEN_COMMA;
		case '"':
			return TOKEN_STRING;
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
			return TOKEN_NUMBER;
		case ':':
			return TOKEN_COLON;
		}
		index--;
		int remainingLength = json.Length - index;
		// false
		if (remainingLength >= 5)
		{
			if (json[index] == 'f' && json[index + 1] == 'a' && json[index + 2] == 'l' && json[index + 3] == 's' && json[index + 4] == 'e')
			{
				index += 5;
				return TOKEN_FALSE;
			}
		}
		// true
		if (remainingLength >= 4)
		{
			if (json[index] == 't' && json[index + 1] == 'r' && json[index + 2] == 'u' && json[index + 3] == 'e')
			{
				index += 4;
				return TOKEN_TRUE;
			}
		}
		// null
		if (remainingLength >= 4)
		{
			if (json[index] == 'n' && json[index + 1] == 'u' && json[index + 2] == 'l' && json[index + 3] == 'l')
			{
				index += 4;
				return TOKEN_NULL;
			}
		}
		return TOKEN_NONE;
	}

	static bool SerializeValue (IJsonSerializerStrategy jsonSerializerStrategy, object value, StringBuilder builder)
	{
		bool success = true;
		string stringValue = value as string;
		if (stringValue != null)
			success = SerializeString (stringValue, builder);
		else
		{
			IDictionary<string, object> dict = value as IDictionary<string, object>;
			if (dict != null)
			{
				success = SerializeObject (jsonSerializerStrategy, dict.Keys, dict.Values, builder);
			}
			else
			{
				IDictionary<string, string> stringDictionary = value as IDictionary<string, string>;
				if (stringDictionary != null)
				{
					success = SerializeObject (jsonSerializerStrategy, stringDictionary.Keys, stringDictionary.Values, builder);
				}
				else
				{
					IEnumerable enumerableValue = value as IEnumerable;
					if (enumerableValue != null)
						success = SerializeArray (jsonSerializerStrategy, enumerableValue, builder);
					else if (IsNumeric (value))
						success = SerializeNumber (value, builder);
					else if (value is bool)
						builder.Append ((bool)value ? "true" : "false");
					else if (value == null)
						builder.Append ("null");
					else
					{
						object serializedObject;
						success = jsonSerializerStrategy.TrySerializeNonPrimitiveObject (value, out serializedObject);
						if (success)
							SerializeValue (jsonSerializerStrategy, serializedObject, builder);
					}
				}
			}
		}
		return success;
	}

	static bool SerializeObject (IJsonSerializerStrategy jsonSerializerStrategy, IEnumerable keys, IEnumerable values, StringBuilder builder)
	{
		builder.Append ("{");
		IEnumerator ke = keys.GetEnumerator ();
		IEnumerator ve = values.GetEnumerator ();
		bool first = true;
		while (ke.MoveNext () && ve.MoveNext ())
		{
			object key = ke.Current;
			object value = ve.Current;
			if (!first)
				builder.Append (",");
			string stringKey = key as string;
			if (stringKey != null)
				SerializeString (stringKey, builder);
			else if (!SerializeValue (jsonSerializerStrategy, value, builder)) return false;
			builder.Append (":");
			if (!SerializeValue (jsonSerializerStrategy, value, builder))
				return false;
			first = false;
		}
		builder.Append ("}");
		return true;
	}

	static bool SerializeArray (IJsonSerializerStrategy jsonSerializerStrategy, IEnumerable anArray, StringBuilder builder)
	{
		builder.Append ("[");
		bool first = true;
		foreach (object value in anArray)
		{
			if (!first)
				builder.Append (",");
			if (!SerializeValue (jsonSerializerStrategy, value, builder))
				return false;
			first = false;
		}
		builder.Append ("]");
		return true;
	}

	static bool SerializeString (string aString, StringBuilder builder)
	{
		builder.Append ("\"");
		char[] charArray = aString.ToCharArray ();
		for (int i = 0; i < charArray.Length; i++)
		{
			char c = charArray[i];
			if (c == '"')
				builder.Append ("\\\"");
			else if (c == '\\')
				builder.Append ("\\\\");
			else if (c == '\b')
				builder.Append ("\\b");
			else if (c == '\f')
				builder.Append ("\\f");
			else if (c == '\n')
				builder.Append ("\\n");
			else if (c == '\r')
				builder.Append ("\\r");
			else if (c == '\t')
				builder.Append ("\\t");
			else
				builder.Append (c);
		}
		builder.Append ("\"");
		return true;
	}

	static bool SerializeNumber (object number, StringBuilder builder)
	{
		if (number is long)
			builder.Append (((long)number).ToString (CultureInfo.InvariantCulture));
		else if (number is ulong)
			builder.Append (((ulong)number).ToString (CultureInfo.InvariantCulture));
		else if (number is int)
			builder.Append (((int)number).ToString (CultureInfo.InvariantCulture));
		else if (number is uint)
			builder.Append (((uint)number).ToString (CultureInfo.InvariantCulture));
		else if (number is decimal)
			builder.Append (((decimal)number).ToString (CultureInfo.InvariantCulture));
		else if (number is float)
			builder.Append (((float)number).ToString (CultureInfo.InvariantCulture));
		else
			builder.Append (Convert.ToDouble (number, CultureInfo.InvariantCulture).ToString ("r", CultureInfo.InvariantCulture));
		return true;
	}

	/// <summary>
	/// Determines if a given object is numeric in any way
	/// (can be integer, double, null, etc).
	/// </summary>
	static bool IsNumeric (object value)
	{
		if (value is sbyte) return true;
		if (value is byte) return true;
		if (value is short) return true;
		if (value is ushort) return true;
		if (value is int) return true;
		if (value is uint) return true;
		if (value is long) return true;
		if (value is ulong) return true;
		if (value is float) return true;
		if (value is double) return true;
		if (value is decimal) return true;
		return false;
	}

	private static IJsonSerializerStrategy _currentJsonSerializerStrategy;
	public static IJsonSerializerStrategy CurrentJsonSerializerStrategy
	{
		get
		{
			return _currentJsonSerializerStrategy ??
			       (_currentJsonSerializerStrategy =
				        PocoJsonSerializerStrategy
				   );
		}
		set
		{
			_currentJsonSerializerStrategy = value;
		}
	}

	private static PocoJsonSerializerStrategy _pocoJsonSerializerStrategy;
	[EditorBrowsable (EditorBrowsableState.Advanced)]
	public static PocoJsonSerializerStrategy PocoJsonSerializerStrategy
	{
		get
		{
			return _pocoJsonSerializerStrategy ?? (_pocoJsonSerializerStrategy = new PocoJsonSerializerStrategy ());
		}
	}

}

[GeneratedCode ("simple-json", "1.0.0")]
internal
interface IJsonSerializerStrategy
{
	[SuppressMessage ("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate", Justification="Need to support .NET 2")]
	bool TrySerializeNonPrimitiveObject (object input, out object output);
	object DeserializeObject (object value, Type type);
}

[GeneratedCode ("simple-json", "1.0.0")]
internal
class PocoJsonSerializerStrategy : IJsonSerializerStrategy
{
	internal IDictionary<Type, ReflectionUtils.ConstructorDelegate> ConstructorCache;
	internal IDictionary<Type, IDictionary<string, ReflectionUtils.GetDelegate>> GetCache;
	internal IDictionary<Type, IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>>> SetCache;

	internal static readonly Type[] EmptyTypes = new Type[0];
	internal static readonly Type[] ArrayConstructorParameterTypes = new Type[] { typeof (int) };

	private static readonly string[] Iso8601Format = new string[]
	{
		@"yyyy-MM-dd\THH:mm:ss.FFFFFFF\Z",
		@"yyyy-MM-dd\THH:mm:ss\Z",
		@"yyyy-MM-dd\THH:mm:ssK"
	};

	public PocoJsonSerializerStrategy ()
	{
		ConstructorCache = new ReflectionUtils.ThreadSafeDictionary<Type, ReflectionUtils.ConstructorDelegate> (ContructorDelegateFactory);
		GetCache = new ReflectionUtils.ThreadSafeDictionary<Type, IDictionary<string, ReflectionUtils.GetDelegate>> (GetterValueFactory);
		SetCache = new ReflectionUtils.ThreadSafeDictionary<Type, IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>>> (SetterValueFactory);
	}

	protected virtual string MapClrMemberNameToJsonFieldName (string clrPropertyName)
	{
		return clrPropertyName;
	}

	internal virtual ReflectionUtils.ConstructorDelegate ContructorDelegateFactory (Type key)
	{
		return ReflectionUtils.GetContructor (key, key.IsArray ? ArrayConstructorParameterTypes : EmptyTypes);
	}

	internal virtual IDictionary<string, ReflectionUtils.GetDelegate> GetterValueFactory (Type type)
	{
		IDictionary<string, ReflectionUtils.GetDelegate> result = new Dictionary<string, ReflectionUtils.GetDelegate> ();
		foreach (PropertyInfo propertyInfo in ReflectionUtils.GetProperties (type))
		{
			if (propertyInfo.CanRead)
			{
				MethodInfo getMethod = ReflectionUtils.GetGetterMethodInfo (propertyInfo);
				if (getMethod.IsStatic || !getMethod.IsPublic)
					continue;
				result[MapClrMemberNameToJsonFieldName (propertyInfo.Name)] = ReflectionUtils.GetGetMethod (propertyInfo);
			}
		}
		foreach (FieldInfo fieldInfo in ReflectionUtils.GetFields (type))
		{
			if (fieldInfo.IsStatic || !fieldInfo.IsPublic)
				continue;
			result[MapClrMemberNameToJsonFieldName (fieldInfo.Name)] = ReflectionUtils.GetGetMethod (fieldInfo);
		}
		return result;
	}

	internal virtual IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>> SetterValueFactory (Type type)
	{
		IDictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>> result = new Dictionary<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>> ();
		foreach (PropertyInfo propertyInfo in ReflectionUtils.GetProperties (type))
		{
			if (propertyInfo.CanWrite)
			{
				MethodInfo setMethod = ReflectionUtils.GetSetterMethodInfo (propertyInfo);
				if (setMethod.IsStatic || !setMethod.IsPublic)
					continue;
				result[MapClrMemberNameToJsonFieldName (propertyInfo.Name)] = new KeyValuePair<Type, ReflectionUtils.SetDelegate> (propertyInfo.PropertyType, ReflectionUtils.GetSetMethod (propertyInfo));
			}
		}
		foreach (FieldInfo fieldInfo in ReflectionUtils.GetFields (type))
		{
			if (fieldInfo.IsInitOnly || fieldInfo.IsStatic || !fieldInfo.IsPublic)
				continue;
			result[MapClrMemberNameToJsonFieldName (fieldInfo.Name)] = new KeyValuePair<Type, ReflectionUtils.SetDelegate> (fieldInfo.FieldType, ReflectionUtils.GetSetMethod (fieldInfo));
		}
		return result;
	}

	public virtual bool TrySerializeNonPrimitiveObject (object input, out object output)
	{
		return TrySerializeKnownTypes (input, out output) || TrySerializeUnknownTypes (input, out output);
	}

	[SuppressMessage ("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
	public virtual object DeserializeObject (object value, Type type)
	{
		if (type == null) throw new ArgumentNullException ("type");
		string str = value as string;

		if (type == typeof (Guid) && string.IsNullOrEmpty (str))
			return default (Guid);

		if (value == null)
			return null;

		object obj = null;

		if (str != null)
		{
			if (str.Length != 0) // We know it can't be null now.
			{
				if (type == typeof (DateTime) || (ReflectionUtils.IsNullableType (type) && Nullable.GetUnderlyingType (type) == typeof (DateTime)))
					return DateTime.ParseExact (str, Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
				if (type == typeof (DateTimeOffset) || (ReflectionUtils.IsNullableType (type) && Nullable.GetUnderlyingType (type) == typeof (DateTimeOffset)))
					return DateTimeOffset.ParseExact (str, Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
				if (type == typeof (Guid) || (ReflectionUtils.IsNullableType (type) && Nullable.GetUnderlyingType (type) == typeof (Guid)))
					return new Guid (str);
				return str;
			}
			else
			{
				if (type == typeof (Guid))
					obj = default (Guid);
				else if (ReflectionUtils.IsNullableType (type) && Nullable.GetUnderlyingType (type) == typeof (Guid))
					obj = null;
				else
					obj = str;
			}
			// Empty string case
			if (!ReflectionUtils.IsNullableType (type) && Nullable.GetUnderlyingType (type) == typeof (Guid))
				return str;
		}
		else if (value is bool)
			return value;

		bool valueIsLong = value is long;
		bool valueIsDouble = value is double;
		if ((valueIsLong && type == typeof (long)) || (valueIsDouble && type == typeof (double)))
			return value;
		if ((valueIsDouble && type != typeof (double)) || (valueIsLong && type != typeof (long)))
		{
			obj =
				typeof (IConvertible).IsAssignableFrom (type)
				? Convert.ChangeType (value, type, CultureInfo.InvariantCulture) : value;
		}
		else
		{
			IDictionary<string, object> objects = value as IDictionary<string, object>;
			if (objects != null)
			{
				IDictionary<string, object> jsonObject = objects;

				if (ReflectionUtils.IsTypeDictionary (type))
				{
					// if dictionary then
					Type[] types = ReflectionUtils.GetGenericTypeArguments (type);
					Type keyType = types[0];
					Type valueType = types[1];

					Type genericType = typeof (Dictionary<,>).MakeGenericType (keyType, valueType);

					IDictionary dict = (IDictionary)ConstructorCache[genericType] (null);

					foreach (KeyValuePair<string, object> kvp in jsonObject)
						dict.Add (kvp.Key, DeserializeObject (kvp.Value, valueType));

					obj = dict;
				}
				else
				{
					if (type == typeof (object))
						obj = value;
					else
					{
						obj = ConstructorCache[type] (null);
						foreach (KeyValuePair<string, KeyValuePair<Type, ReflectionUtils.SetDelegate>> setter in SetCache[type])
						{
							object jsonValue;
							if (jsonObject.TryGetValue (setter.Key, out jsonValue))
							{
								jsonValue = DeserializeObject (jsonValue, setter.Value.Key);
								setter.Value.Value (obj, jsonValue);
							}
						}
					}
				}
			}
			else
			{
				IList<object> valueAsList = value as IList<object>;
				if (valueAsList != null)
				{
					IList<object> jsonObject = valueAsList;
					IList list = null;

					if (type.IsArray)
					{
						list = (IList)ConstructorCache[type] (jsonObject.Count);
						int i = 0;
						foreach (object o in jsonObject)
							list[i++] = DeserializeObject (o, type.GetElementType ());
					}
					else if (ReflectionUtils.IsTypeGenericeCollectionInterface (type) || ReflectionUtils.IsAssignableFrom (typeof (IList), type))
					{
						Type innerType = ReflectionUtils.GetGenericTypeArguments (type)[0];
						Type genericType = typeof (List<>).MakeGenericType (innerType);
						list = (IList)ConstructorCache[genericType] (jsonObject.Count);
						foreach (object o in jsonObject)
							list.Add (DeserializeObject (o, innerType));
					}
					obj = list;
				}
			}
			return obj;
		}
		if (ReflectionUtils.IsNullableType (type))
			return ReflectionUtils.ToNullableType (obj, type);
		return obj;
	}

	protected virtual object SerializeEnum (Enum p)
	{
		return Convert.ToDouble (p, CultureInfo.InvariantCulture);
	}

	[SuppressMessage ("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate", Justification="Need to support .NET 2")]
	protected virtual bool TrySerializeKnownTypes (object input, out object output)
	{
		bool returnValue = true;
		if (input is DateTime)
			output = ((DateTime)input).ToUniversalTime ().ToString (Iso8601Format[0], CultureInfo.InvariantCulture);
		else if (input is DateTimeOffset)
			output = ((DateTimeOffset)input).ToUniversalTime ().ToString (Iso8601Format[0], CultureInfo.InvariantCulture);
		else if (input is Guid)
			output = ((Guid)input).ToString ("D");
		else if (input is Uri)
			output = input.ToString ();
		else
		{
			Enum inputEnum = input as Enum;
			if (inputEnum != null)
				output = SerializeEnum (inputEnum);
			else
			{
				returnValue = false;
				output = null;
			}
		}
		return returnValue;
	}
	[SuppressMessage ("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate", Justification="Need to support .NET 2")]
	protected virtual bool TrySerializeUnknownTypes (object input, out object output)
	{
		if (input == null) throw new ArgumentNullException ("input");
		output = null;
		Type type = input.GetType ();
		if (type.FullName == null)
			return false;
		IDictionary<string, object> obj = new JsonObject ();
		IDictionary<string, ReflectionUtils.GetDelegate> getters = GetCache[type];
		foreach (KeyValuePair<string, ReflectionUtils.GetDelegate> getter in getters)
		{
			if (getter.Value != null)
				obj.Add (MapClrMemberNameToJsonFieldName (getter.Key), getter.Value (input));
		}
		output = obj;
		return true;
	}
}


namespace Reflection
{
// This class is meant to be copied into other libraries. So we want to exclude it from Code Analysis rules
// that might be in place in the target project.
[GeneratedCode ("reflection-utils", "1.0.0")]
internal
class ReflectionUtils
{
	private static readonly object[] EmptyObjects = new object[] { };

	public delegate object GetDelegate (object source);
	public delegate void SetDelegate (object source, object value);
	public delegate object ConstructorDelegate (params object[] args);

	public delegate TValue ThreadSafeDictionaryValueFactory<TKey, TValue> (TKey key);

	public static Attribute GetAttribute (MemberInfo info, Type type)
	{
		if (info == null || type == null || !Attribute.IsDefined (info, type))
			return null;
		return Attribute.GetCustomAttribute (info, type);
	}

	public static Attribute GetAttribute (Type objectType, Type attributeType)
	{

		if (objectType == null || attributeType == null || !Attribute.IsDefined (objectType, attributeType))
			return null;
		return Attribute.GetCustomAttribute (objectType, attributeType);
	}

	public static Type[] GetGenericTypeArguments (Type type)
	{
		return type.GetGenericArguments ();
	}

	public static bool IsTypeGenericeCollectionInterface (Type type)
	{
		if (!type.IsGenericType)
			return false;

		Type genericDefinition = type.GetGenericTypeDefinition ();

		return (genericDefinition == typeof (IList<>) || genericDefinition == typeof (ICollection<>) || genericDefinition == typeof (IEnumerable<>));
	}

	public static bool IsAssignableFrom (Type type1, Type type2)
	{
		return type1.IsAssignableFrom (type2);
	}

	public static bool IsTypeDictionary (Type type)
	{
		if (typeof (System.Collections.IDictionary).IsAssignableFrom (type))
			return true;

		if (!type.IsGenericType)
			return false;
		Type genericDefinition = type.GetGenericTypeDefinition ();
		return genericDefinition == typeof (IDictionary<,>);
	}

	public static bool IsNullableType (Type type)
	{
		return
			type.IsGenericType
			&& type.GetGenericTypeDefinition () == typeof (Nullable<>);
	}

	public static object ToNullableType (object obj, Type nullableType)
	{
		return obj == null ? null : Convert.ChangeType (obj, Nullable.GetUnderlyingType (nullableType), CultureInfo.InvariantCulture);
	}

	public static bool IsValueType (Type type)
	{
		return type.IsValueType;
	}

	public static IEnumerable<ConstructorInfo> GetConstructors (Type type)
	{
		return type.GetConstructors ();
	}

	public static ConstructorInfo GetConstructorInfo (Type type, params Type[] argsType)
	{
		IEnumerable<ConstructorInfo> constructorInfos = GetConstructors (type);
		int i;
		bool matches;
		foreach (ConstructorInfo constructorInfo in constructorInfos)
		{
			ParameterInfo[] parameters = constructorInfo.GetParameters ();
			if (argsType.Length != parameters.Length)
				continue;

			i = 0;
			matches = true;
			foreach (ParameterInfo parameterInfo in constructorInfo.GetParameters ())
			{
				if (parameterInfo.ParameterType != argsType[i])
				{
					matches = false;
					break;
				}
			}

			if (matches)
				return constructorInfo;
		}

		return null;
	}

	public static IEnumerable<PropertyInfo> GetProperties (Type type)
	{
		return type.GetProperties (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
	}

	public static IEnumerable<FieldInfo> GetFields (Type type)
	{
		return type.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
	}

	public static MethodInfo GetGetterMethodInfo (PropertyInfo propertyInfo)
	{
		return propertyInfo.GetGetMethod (true);
	}

	public static MethodInfo GetSetterMethodInfo (PropertyInfo propertyInfo)
	{
		return propertyInfo.GetSetMethod (true);
	}

	public static ConstructorDelegate GetContructor (ConstructorInfo constructorInfo)
	{
		return GetConstructorByReflection (constructorInfo);
	}

	public static ConstructorDelegate GetContructor (Type type, params Type[] argsType)
	{
		return GetConstructorByReflection (type, argsType);
	}

	public static ConstructorDelegate GetConstructorByReflection (ConstructorInfo constructorInfo)
	{
		return delegate (object[] args)
		{
			return constructorInfo.Invoke (args);
		};
	}

	public static ConstructorDelegate GetConstructorByReflection (Type type, params Type[] argsType)
	{
		ConstructorInfo constructorInfo = GetConstructorInfo (type, argsType);
		return constructorInfo == null ? null : GetConstructorByReflection (constructorInfo);
	}


	public static GetDelegate GetGetMethod (PropertyInfo propertyInfo)
	{
		return GetGetMethodByReflection (propertyInfo);
	}

	public static GetDelegate GetGetMethod (FieldInfo fieldInfo)
	{
		return GetGetMethodByReflection (fieldInfo);
	}

	public static GetDelegate GetGetMethodByReflection (PropertyInfo propertyInfo)
	{
		MethodInfo methodInfo = GetGetterMethodInfo (propertyInfo);
		return delegate (object source)
		{
			return methodInfo.Invoke (source, EmptyObjects);
		};
	}

	public static GetDelegate GetGetMethodByReflection (FieldInfo fieldInfo)
	{
		return delegate (object source)
		{
			return fieldInfo.GetValue (source);
		};
	}


	public static SetDelegate GetSetMethod (PropertyInfo propertyInfo)
	{
		return GetSetMethodByReflection (propertyInfo);
	}

	public static SetDelegate GetSetMethod (FieldInfo fieldInfo)
	{
		return GetSetMethodByReflection (fieldInfo);
	}

	public static SetDelegate GetSetMethodByReflection (PropertyInfo propertyInfo)
	{
		MethodInfo methodInfo = GetSetterMethodInfo (propertyInfo);
		return delegate (object source, object value)
		{
			methodInfo.Invoke (source, new object[]
			{
				value
			});
		};
	}

	public static SetDelegate GetSetMethodByReflection (FieldInfo fieldInfo)
	{
		return delegate (object source, object value)
		{
			fieldInfo.SetValue (source, value);
		};
	}


	public sealed class ThreadSafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		private readonly object _lock = new object ();
		private readonly ThreadSafeDictionaryValueFactory<TKey, TValue> _valueFactory;
		private Dictionary<TKey, TValue> _dictionary;

		public ThreadSafeDictionary (ThreadSafeDictionaryValueFactory<TKey, TValue> valueFactory)
		{
			_valueFactory = valueFactory;
		}

		private TValue Get (TKey key)
		{
			if (_dictionary == null)
				return AddValue (key);
			TValue value;
			if (!_dictionary.TryGetValue (key, out value))
				return AddValue (key);
			return value;
		}

		private TValue AddValue (TKey key)
		{
			TValue value = _valueFactory (key);
			lock (_lock)
			{
				if (_dictionary == null)
				{
					_dictionary = new Dictionary<TKey, TValue> ();
					_dictionary[key] = value;
				}
				else
				{
					TValue val;
					if (_dictionary.TryGetValue (key, out val))
						return val;
					Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue> (_dictionary);
					dict[key] = value;
					_dictionary = dict;
				}
			}
			return value;
		}

		public void Add (TKey key, TValue value)
		{
			throw new NotImplementedException ();
		}

		public bool ContainsKey (TKey key)
		{
			return _dictionary.ContainsKey (key);
		}

		public ICollection<TKey> Keys
		{
			get
			{
				return _dictionary.Keys;
			}
		}

		public bool Remove (TKey key)
		{
			throw new NotImplementedException ();
		}

		public bool TryGetValue (TKey key, out TValue value)
		{
			value = this[key];
			return true;
		}

		public ICollection<TValue> Values
		{
			get
			{
				return _dictionary.Values;
			}
		}

		public TValue this[TKey key]
		{
			get
			{
				return Get (key);
			}
			set
			{
				throw new NotImplementedException ();
			}
		}

		public void Add (KeyValuePair<TKey, TValue> item)
		{
			throw new NotImplementedException ();
		}

		public void Clear ()
		{
			throw new NotImplementedException ();
		}

		public bool Contains (KeyValuePair<TKey, TValue> item)
		{
			throw new NotImplementedException ();
		}

		public void CopyTo (KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			throw new NotImplementedException ();
		}

		public int Count
		{
			get
			{
				return _dictionary.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				throw new NotImplementedException ();
			}
		}

		public bool Remove (KeyValuePair<TKey, TValue> item)
		{
			throw new NotImplementedException ();
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator ()
		{
			return _dictionary.GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return _dictionary.GetEnumerator ();
		}
	}

}
}
}
// ReSharper restore LoopCanBeConvertedToQuery
// ReSharper restore RedundantExplicitArrayCreation
// ReSharper restore SuggestUseVarKeywordEvident
