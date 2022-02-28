﻿// Source code is modified from Mike Jones's JSON Serialization and Deserialization library (https://www.ghielectronics.com/community/codeshare/entry/357)

using System;
using System.Reflection;
using System.Collections;
using System.Text;
using System.Diagnostics;

namespace nanoFramework.json
{
    /// <summary>
    /// JSON.NetMF - JSON Serialization and Deserialization library for .NET Micro Framework
    /// </summary>
    public class JsonSerializer
    {
        internal JsonSerializer(DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {
            DateFormat = dateTimeFormat;
        }

        static byte[] buffer = new byte[4096];
        static int index = 0;

        /// <summary>
        /// Gets/Sets the format that will be used to display
        /// and parse dates in the Json data.
        /// </summary>
        internal DateTimeFormat DateFormat { get; set; }

        /// <summary>
        /// Convert an object to a JSON string.
        /// </summary>
        /// <param name="o">The value to convert. Supported types are: Boolean, String, Byte, (U)Int16, (U)Int32, Float, Double, Decimal, Array, IDictionary, IEnumerable, Guid, Datetime, DictionaryEntry, Object and null.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        /// <remarks>For objects, only internal properties with getters are converted.</remarks>
		public static string Serialize(object o)
        {
            index = 0;
            SerializeObject(o);
            //return Encoding.UTF8.GetString(buffer, 0, index);
            return String.Empty;
            //return SerializeObject(o);
        }

        internal static void StringToBuffer(string value)
        {
            Encoding.UTF8.GetBytes(value).CopyTo(buffer, index);
            index += value.Length;
        }
        /// <summary>
        /// Convert an object to a JSON string.
        /// </summary>
        /// <param name="o">The value to convert. Supported types are: Boolean, String, Byte, (U)Int16, (U)Int32, Float, Double, Decimal, Array, IDictionary, IEnumerable, Guid, Datetime, TimeSpan, DictionaryEntry, Object and null.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        /// <remarks>For objects, only internal properties with getters are converted.</remarks>
        internal static void SerializeObject(object o, DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {
            if (o == null)
            {
                StringToBuffer("null");
                return; //"null";
            }

            Type type = o.GetType();

            switch (type.Name)
            {
                case "Boolean":
                    {
                        StringToBuffer((bool)o ? "true" : "false");
                        return; //"null";
                        //return (bool)o ? "true" : "false";
                    }
                case "TimeSpan":
                case "String":
                case "Char":
                case "Guid":
                    {
                        buffer[index] = (byte)'"';
                        index++;
                        StringToBuffer(o.ToString());
                        buffer[index] = (byte)'"';
                        index++;
                        return;
                        //return "\"" + o.ToString() + "\"";
                    }
                case "Single":
                    {
                        if (float.IsNaN((Single)o))
                        {
                            StringToBuffer("null");
                            return;
                        }
                        StringToBuffer(o.ToString());
                        return;
                        //return o.ToString();
                    }
                case "Double":
                    {
                        if (double.IsNaN((double)o))
                        {
                            StringToBuffer("null");
                            return;
                        }
                        StringToBuffer(o.ToString());
                        return;
                        //return o.ToString();
                    }
                case "Decimal":
                case "Float":
                case "Byte":
                case "SByte":
                case "Int16":
                case "UInt16":
                case "Int32":
                case "UInt32":
                case "Int64":
                case "UInt64":
                    {
                        StringToBuffer(o.ToString());
                        return;
                    }
                case "DateTime":
                    { //TODO Remove all reference to non IS8601 DATETime
                        buffer[index] = (byte)'"';
                        index++;
                        StringToBuffer(nanoFramework.Json.DateTimeExtensions.ToIso8601((DateTime)o));
                        buffer[index] = (byte)'"';
                        index++;
                        return;
                        //return "\"" + nanoFramework.Json.DateTimeExtensions.ToIso8601((DateTime)o) + "\"";
                    }
            }

            if (o is IDictionary && !type.IsArray)
            {
                IDictionary dictionary = o as IDictionary;
                SerializeIDictionary(dictionary, dateTimeFormat);
                return;
            }

            if (o is IEnumerable)
            {
                IEnumerable enumerable = o as IEnumerable;
                SerializeIEnumerable(enumerable, dateTimeFormat);
                return;
            }

            if (type == typeof(System.Collections.DictionaryEntry))
            {
                Hashtable hashtable = new Hashtable();
                if (o is DictionaryEntry)
                {
                    var dic = (DictionaryEntry)o;
                    DictionaryEntry entry = dic;
                    hashtable.Add(entry.Key, entry.Value);

                }
                SerializeIDictionary(hashtable, dateTimeFormat);
                return;
            }

            if (type.IsClass)
            {
                Hashtable hashtable = new Hashtable();

                // Iterate through all of the methods, looking for internal GET properties
                MethodInfo[] methods = type.GetMethods();
                foreach (MethodInfo method in methods)
                {
                    // We care only about property getters when serializing
                    if (method.Name.StartsWith("get_"))
                    {
                        // Ignore abstract and virtual objects
                        if (method.IsAbstract)
                        {
                            continue;
                        }

                        // Ignore delegates and MethodInfos
                        if ((method.ReturnType == typeof(System.Delegate)) ||
                            (method.ReturnType == typeof(System.MulticastDelegate)) ||
                            (method.ReturnType == typeof(System.Reflection.MethodInfo)))
                        {
                            continue;
                        }
                        // Ditto for DeclaringType
                        if ((method.DeclaringType == typeof(System.Delegate)) ||
                            (method.DeclaringType == typeof(System.MulticastDelegate)))
                        {
                            continue;
                        }

                        object returnObject = method.Invoke(o, null);
                        hashtable.Add(method.Name.Substring(4), returnObject);
                    }
                }
                SerializeIDictionary(hashtable, dateTimeFormat);
                return;
            }

            return;
        }

        /// <summary>
        /// Convert an IEnumerable to a JSON string.
        /// </summary>
        /// <param name="enumerable">The value to convert.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        internal static void SerializeIEnumerable(IEnumerable enumerable, DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {
            //string result = ("[");
            bool firstItem = true;
            buffer[index] = (byte)'[';
            index++;

            foreach (object current in enumerable)
            {
                if (!firstItem)
                {
                    buffer[index] = (byte)',';
                    index++;
                    //result += (",");
                }
                else
                {
                    firstItem = false;
                }
                SerializeObject(current, dateTimeFormat);
                //result += (SerializeObject(current, dateTimeFormat));
            }

            buffer[index] = (byte)']';
            index++;
            //result += ("]");
            return;
        }

        /// <summary>
        /// Convert an IDictionary to a JSON string.
        /// </summary>
        /// <param name="dictionary">The value to convert.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>

        internal static void SerializeIDictionary(IDictionary dictionary, DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {

            //string result = "{";
            bool firstItem = true;
            buffer[index] = (byte)'{';
            index++;

            foreach (DictionaryEntry entry in dictionary)
            {
                if(!firstItem)
                {
                    buffer[index] = (byte)',';
                    index++;
                    //result += (",");
                }
                else
                {
                    firstItem = false;
                }
                buffer[index] = (byte)'"';
                index++;
                StringToBuffer((string)entry.Key);
                buffer[index] = (byte)'"';
                index++;
                buffer[index] = (byte)':';
                index++;
                //result += ("\"" + entry.Key + "\":");

                SerializeObject(entry.Value, dateTimeFormat);
                //var ser = SerializeObject(entry.Value, dateTimeFormat);
                //result += (ser);
            }

            buffer[index] = (byte)'}';
            index++;
            return;
            //result += ("}");
            //return result;
        }

        /// <summary>
        /// Safely serialize a String into a JSON string value, escaping all backslash and quote characters.
        /// </summary>
        /// <param name="str">The string to serialize.</param>
        /// <returns>The serialized JSON string.</returns>
        protected static string SerializeString(String str)
        {
            // If the string is just fine (most are) then make a quick exit for improved performance
            if (str.IndexOf('\\') < 0 && str.IndexOf('\"') < 0)
                return str;

            // Build a new string
            StringBuilder result = new StringBuilder(str.Length + 1); // we know there is at least 1 char to escape
            foreach (char ch in str.ToCharArray())
            {
                if (ch == '\\' || ch == '\"')
                    result.Append('\\');
                result.Append(ch);
            }
            return result.ToString();
        }

    }

    /// <summary>
    /// Enumeration of the popular formats of time and date
    /// within Json.  It's not a standard, so you have to
    /// know which on you're using.
    /// </summary>
    internal enum DateTimeFormat
    {
        Default = 0,
        ISO8601 = 1,
        Ajax = 2
    }
}
