// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>Provides functions for converting between objects and JSON data.</summary>
    ///<remarks>
    ///  <para>You can use this class to generate a JSON representation of an object, or to populate an object from a JSON string. This can be useful when interacting with web services that send and receive JSON data, or when you need to convert objects into a serializable format, such as when saving game state.
    ///
    ///The functions use the standard Unity serializer, which means they only serialize or deserialize the fields on an object, and only when the fields are of supported types. For more information about the Unity serializer, refer to [Serialization rules](xref:script-serialization-rules) in the Unity manual.
    ///
    ///The following example shows use of <c>JsonUtility</c> to save and load a game's state to the <see cref="PlayerPrefs" />.</para>
    ///  <para>The object or type you pass to the functions must be a custom C# type you have defined. It must not be a primitive type such as <c>bool</c> or <c>string</c> or a collection type such as <c>List&lt;T&gt;</c> or an array. If you want to serialize a collection of objects to JSON, you must create a <c>class</c> or <c>struct</c> which has the collection as a member. Similarly, if you want to deserialize a JSON string, that JSON string must always have an object at the top level, not an array.
    ///
    ///</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[using UnityEngine;
    ///using System;
    ///using System.Collections.Generic;
    ///
    ///[Serializable]
    ///public class GameState
    ///{
    ///    public int Lives;
    ///    public int Level;
    ///    public string CharacterName;
    ///    public List<string> ItemsCarried;
    /// 
    ///    public const string PlayerPrefsKeyName = "SavedGameState";
    ///
    ///    public void SaveToPlayerPrefs()
    ///    {
    ///        // Convert this GameState instance to a JSON string
    ///        string json = JsonUtility.ToJson(this);
    ///
    ///        // Save the converted JSON into the PlayerPrefs
    ///        PlayerPrefs.SetString(PlayerPrefsKeyName, json);
    ///        PlayerPrefs.Save();
    ///    }
    ///
    ///    public static GameState CreateFromPlayerPrefs()
    ///    {
    ///        // If the game was never saved before, the key will not exist; in this case return null
    ///        if(!PlayerPrefs.HasKey(PlayerPrefsKeyName))
    ///            return null;
    ///
    ///        // Retrieve the saved JSON string from the player prefs
    ///        string json = PlayerPrefs.GetString(PlayerPrefsKeyName);
    ///
    ///        // Deserialize the JSON string into a new GameState object and return it
    ///        return JsonUtility.FromJson<GameState>(json);           
    ///    }
    ///}]]></code>
    ///</example>
    ///<seealso cref="T:UnityEditor.EditorJsonUtility" />
    ///<seealso href="xref:json-serialization" />
    [NativeHeader("Modules/JSONSerialize/Public/JsonUtility.bindings.h")]
    public static class JsonUtility
    {
        [FreeFunction("ToJsonInternal", IsThreadSafe = true)]
        private static extern string ToJsonInternal([NotNull] object obj, bool prettyPrint);

        [FreeFunction("FromJsonInternal", true, ThrowsException = true, IsThreadSafe = true)]
        private static extern object FromJsonInternal(string json, object objectToOverwrite, Type type);

        // False means the walker diverged or never engaged; test link.xml keeps this in stripped players
        [FreeFunction("CheckJsonFromBinaryByteIdenticalInternal")]
        internal static extern bool CheckJsonFromBinaryByteIdentical([NotNull] object obj, bool prettyPrint);

        ///<summary>Generate a JSON representation of the public fields of an object.</summary>
        ///<remarks>Internally, this method uses the Unity serializer. The object you pass in and all its fields must meet the requirements for serialization by the Unity serializer. For the full list of these requirements, refer to [Serialization rules](xref:script-serialization-rules) in the manual.
        ///
        ///<c>ToJson</c> supports any plain class or structure and classes derived from MonoBehaviour or ScriptableObject. Other engine types are not supported. In the Editor only, you can use <see cref="M:UnityEditor.EditorJsonUtility.ToJson" /> to serialize other engine types to JSON.
        ///
        ///If the object contains fields with references to other Unity objects, those references are serialized by recording the InstanceID for each referenced object. Because the Instance ID acts like a handle to the in-memory object instance, the JSON string can only be deserialized back during the same session of the Unity engine.
        ///
        ///Note that while <c>ToJson</c> acccepts primitive types, instead of serializing them directly, it attempts to serialize their public instance fields, producing an empty object as a result. Similarly, passing an array does not produce a JSON array containing each element, but an object containing the public fields of the array object itself (of which there are none). To serialize the actual content of an array or primitive type, you must wrap it in a class or struct.
        ///
        ///<c>ToJson</c> can be called from background threads. You should not alter the object that you pass to this function while it is still executing.</remarks>
        ///<param name="obj">The object to convert to JSON form.</param>
        ///<returns>The object's data in JSON format.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class PlayerState : MonoBehaviour
        ///{
        ///    public string playerName;
        ///    public int lives;
        ///    public float health;
        ///
        ///    public string SaveToString()
        ///    {
        ///        return JsonUtility.ToJson(this);
        ///    }
        ///
        ///    // Given:
        ///    // playerName = "Dr Charles"
        ///    // lives = 3
        ///    // health = 0.8f
        ///    // SaveToString returns:
        ///    // {"playerName":"Dr Charles","lives":3,"health":0.8}
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="MonoBehaviour" />
        ///<seealso cref="ScriptableObject" />
        ///<seealso cref="Object.GetInstanceID" />
        public static string ToJson(object obj) { return ToJson(obj, false); }

        ///<summary>Generate a JSON representation of the public fields of an object.</summary>
        ///<remarks>Internally, this method uses the Unity serializer. The object you pass in and all its fields must meet the requirements for serialization by the Unity serializer. For the full list of these requirements, refer to [Serialization rules](xref:script-serialization-rules) in the manual.
        ///
        ///<c>ToJson</c> supports any plain class or structure and classes derived from MonoBehaviour or ScriptableObject. Other engine types are not supported. In the Editor only, you can use <see cref="M:UnityEditor.EditorJsonUtility.ToJson" /> to serialize other engine types to JSON.
        ///
        ///If the object contains fields with references to other Unity objects, those references are serialized by recording the InstanceID for each referenced object. Because the Instance ID acts like a handle to the in-memory object instance, the JSON string can only be deserialized back during the same session of the Unity engine.
        ///
        ///Note that while <c>ToJson</c> acccepts primitive types, instead of serializing them directly, it attempts to serialize their public instance fields, producing an empty object as a result. Similarly, passing an array does not produce a JSON array containing each element, but an object containing the public fields of the array object itself (of which there are none). To serialize the actual content of an array or primitive type, you must wrap it in a class or struct.
        ///
        ///<c>ToJson</c> can be called from background threads. You should not alter the object that you pass to this function while it is still executing.</remarks>
        ///<param name="obj">The object to convert to JSON form.</param>
        ///<param name="prettyPrint">If true, format the output for readability. If false, format the output for minimum size. Default is false.</param>
        ///<returns>The object's data in JSON format.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class PlayerState : MonoBehaviour
        ///{
        ///    public string playerName;
        ///    public int lives;
        ///    public float health;
        ///
        ///    public string SaveToString()
        ///    {
        ///        return JsonUtility.ToJson(this);
        ///    }
        ///
        ///    // Given:
        ///    // playerName = "Dr Charles"
        ///    // lives = 3
        ///    // health = 0.8f
        ///    // SaveToString returns:
        ///    // {"playerName":"Dr Charles","lives":3,"health":0.8}
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="MonoBehaviour" />
        ///<seealso cref="ScriptableObject" />
        ///<seealso cref="Object.GetInstanceID" />
        public static string ToJson(object obj, bool prettyPrint)
        {
            if (obj == null)
                return "";

            if (obj is UnityEngine.Object && !(obj is MonoBehaviour || obj is ScriptableObject))
                throw new ArgumentException("JsonUtility.ToJson does not support engine types.");

            return ToJsonInternal(obj, prettyPrint);
        }

        ///<summary>Create an object from its JSON representation.</summary>
        ///<remarks>
        ///  <para>Internally, this method uses the Unity serializer. The object you're creating and all its fields must meet the requirements for serialization by the Unity serializer. For the full list of these requirements, refer to [Serialization rules](xref:script-serialization-rules) in the manual.
        ///
        ///<c>FromJson</c> only supports plain classes and structures. It does not support classes derived from <c>UnityEngine.Object</c>, such as MonoBehaviour or ScriptableObject. To deserialize data into classes derived from MonoBehaviour or ScriptableObject, use <see cref="JsonUtility.FromJsonOverwrite" /> instead.
        ///
        ///During deserialization, <c>FromJson</c> calls the parameterless (default) constructor of the target type if one exists. Calling the constructor also runs field initializers. After the object is created, <c>FromJson</c> overwrites values of fields that appear in the JSON with the corresponding JSON values. Fields not present in the JSON keep the values assigned by the constructor or field initializers.
        ///
        ///**Important**: If the type has no parameterless constructor, <c>FromJson</c> does not call any constructor, and field initializers do not run. All fields start at their C# type defaults (<c>0</c>, <c>null</c>, <c>false</c>, and so on), and <c>FromJson</c> then sets fields that appear in the JSON to the corresponding JSON values. Non-serialized fields that depend on initializers keep their C# type default values instead of receiving the initializer values. To make sure field initializers run during deserialization, add a parameterless constructor.
        ///
        ///If the input is null or empty, <c>FromJson</c> returns null.
        ///
        ///<c>FromJson</c> can be called from background threads.</para>
        ///  <para>The following example illustrates what happens when a type has only a parameterized constructor. Because <c>EnemyInfo</c> has no parameterless constructor, <c>FromJson</c> does not call any constructor, and field initializers do not run. The <c>[NonSerialized]</c> field keeps its default value (<c>0</c>) rather than the value the initializer would have assigned to it.</para>
        ///</remarks>
        ///<param name="json">The JSON representation of the object.</param>
        ///<returns>An instance of the object.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class FromJsonTest : MonoBehaviour
        ///{
        ///    public static string completeJson = "{\"name\":\"Dr Charles\",\"lives\":3,\"health\":0.8}";
        ///    // Partial JSON, missing lives and health. In this example, these fields will get their values from the initializer and constructor respectively.
        ///    public static string partialJson = "{\"name\":\"Dr Charles\"}";
        ///
        ///    void Start()
        ///    {
        ///        PlayerInfo player1 = PlayerInfo.CreateFromJSON(completeJson);
        ///        Debug.Log("Player1 Name: " + player1.name); // Dr Charles
        ///        Debug.Log("Player1 Lives: " + player1.lives); // 3
        ///        Debug.Log("Player1 Health: " + player1.health); // 0.8
        ///        PlayerInfo player2 = PlayerInfo.CreateFromJSON(partialJson);
        ///        Debug.Log("Player2 Name: " + player2.name); // Dr Charles (from JSON)
        ///        Debug.Log("Player2 Lives: " + player2.lives); // 2 (from initializer)
        ///        Debug.Log("Player2 Health: " + player2.health); // 1 (from constructor)
        ///    }
        ///
        ///}
        ///
        ///[System.Serializable]
        ///public class PlayerInfo
        ///{
        ///    public string name = "Unknown";
        ///    public int lives = 2;
        ///    public float health;
        ///
        ///    public PlayerInfo()
        ///    {
        ///        health = 1.0f;
        ///    }
        ///
        ///    public static PlayerInfo CreateFromJSON(string jsonString)
        ///    {
        ///        return JsonUtility.FromJson<PlayerInfo>(jsonString);
        ///    }
        ///
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System;
        ///
        ///public class EnemyInfoTest : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        EnemyInfo enemy = JsonUtility.FromJson<EnemyInfo>("{\"serializedField\":42}");
        ///        Debug.Log("serializedField: " + enemy.serializedField);           // 42 (from JSON)
        ///        Debug.Log("nonSerializedField: " + enemy.nonSerializedField); // 0 (NOT 100, initializer did not run)
        ///    }
        ///}
        ///
        ///[Serializable]
        ///public class EnemyInfo
        ///{
        ///    public int serializedField;
        ///
        ///    [NonSerialized]
        ///    public int nonSerializedField = 100;
        ///
        ///    // Only a parameterized constructor: no parameterless constructor exists.
        ///    // As a result, FromJson skips constructor invocation entirely
        ///    // and the field initializer on nonSerializedField never runs.
        ///    public EnemyInfo(int value)
        ///    {
        ///        serializedField = value;
        ///    }
        ///
        ///    // To make the field initializer run during deserialization, add a
        ///    // parameterless constructor, for example:
        ///    // public EnemyInfo() { }
        ///}
        ///]]></code>
        ///</example>
        public static T FromJson<T>(string json) { return (T)FromJson(json, typeof(T)); }

        ///<summary>Create an object from its JSON representation.</summary>
        ///<remarks>Internally, this method uses the Unity serializer. The object you're creating and all its fields must meet the requirements for serialization by the Unity serializer. For the full list of these requirements, refer to [Serialization rules](xref:script-serialization-rules) in the manual.
        ///
        ///<c>FromJson</c> only supports plain classes and structures. It does not support classes derived from <c>UnityEngine.Object</c>, such as MonoBehaviour or ScriptableObject. To deserialize data into classes derived from MonoBehaviour or ScriptableObject, use <see cref="JsonUtility.FromJsonOverwrite" /> instead.
        ///
        ///During deserialization, <c>FromJson</c> calls the parameterless (default) constructor of the target type if one exists. Calling the constructor also runs field initializers. After the object is created, <c>FromJson</c> overwrites values of fields that appear in the JSON with the corresponding JSON values. Fields not present in the JSON keep the values assigned by the constructor or field initializers.
        ///
        ///**Important**: If the type has no parameterless constructor, <c>FromJson</c> does not call any constructor, and field initializers do not run. All fields start at their C# type defaults (<c>0</c>, <c>null</c>, <c>false</c>, and so on), and <c>FromJson</c> then sets fields that appear in the JSON to the corresponding JSON values. Non-serialized fields that depend on initializers keep their C# type default values instead of receiving the initializer values. To make sure field initializers run during deserialization, add a parameterless constructor.
        ///
        ///<c>FromJson</c> can be called from background threads.</remarks>
        ///<param name="json">The JSON representation of the object.</param>
        ///<param name="type">The type of object represented by the Json.</param>
        ///<returns>An instance of the object.</returns>
        public static object FromJson(string json, Type type)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            if (type == null)
                throw new ArgumentNullException("type");

            if (type.IsAbstract || type.IsSubclassOf(typeof(UnityEngine.Object)))
                throw new ArgumentException("Cannot deserialize JSON to new instances of type '" + type.Name + ".'");

            return FromJsonInternal(json, null, type);
        }

        ///<summary>Overwrite data in an object by reading from its JSON representation.</summary>
        ///<remarks>This method is very similar to <see cref="JsonUtility.FromJson" />, except that instead of creating a new object and loading the JSON data into it, it loads the JSON data into an existing object. This allows you to update the values stored in classes or objects without any allocations.
        ///
        ///Internally, this method uses the Unity serializer. The object you're creating and all its fields must meet the requirements for serialization by the Unity serializer. For the full list of these requirements, refer to [Serialization rules](xref:script-serialization-rules) in the manual.
        ///
        ///<c>FromJsonOverwrite</c> supports any plain class and classes derived from MonoBehaviour or ScriptableObject. Other engine types are not supported. In the Editor only, you can use <see cref="M:UnityEditor.EditorJsonUtility.FromJsonOverwrite" /> to overwrite other engine objects.
        ///
        ///**Note**: Since <c>FromJsonOverwrite</c> takes a reference type, passing a struct (a value type) to it requires careful &lt;a href="https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing"&gt;
        ///    boxing and unboxing&lt;/a&gt; and is not recommended. Instead, use <see cref="JsonUtility.FromJson" /> for structs as follows: <c>JsonUtility.FromJson&lt;MyStruct&gt;(json);</c>
        ///
        ///If a field of the object is not present in the JSON representation, that field will be left unchanged.
        ///
        ///<c>FromJsonOverwrite</c> can be called from background threads. You should not alter the object that is being overwritten while the function is running.</remarks>
        ///<param name="json">The JSON representation of the object.</param>
        ///<param name="objectToOverwrite">The object that should be overwritten.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class PlayerState : MonoBehaviour
        ///{
        ///    public string playerName;
        ///    public int lives;
        ///    public float health;
        ///
        ///    public void Load(string savedData)
        ///    {
        ///        JsonUtility.FromJsonOverwrite(savedData, this);
        ///    }
        ///
        ///    // Given JSON input:
        ///    // {"lives":3, "health":0.8}
        ///    // the Load function will change the object on which it is called such that
        ///    // lives == 3 and health == 0.8
        ///    // the 'playerName' field will be left unchanged
        ///}
        ///]]></code>
        ///</example>
        public static void FromJsonOverwrite(string json, object objectToOverwrite)
        {
            if (string.IsNullOrEmpty(json))
                return;

            if (objectToOverwrite == null)
                throw new ArgumentNullException("objectToOverwrite");

            if (objectToOverwrite is UnityEngine.Object && !(objectToOverwrite is MonoBehaviour || objectToOverwrite is ScriptableObject))
                throw new ArgumentException("Engine types cannot be overwritten from JSON outside of the Editor.");

            FromJsonInternal(json, objectToOverwrite, objectToOverwrite.GetType());
        }
    }
}
