using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public sealed class GenericsJSONParser
{
    public const int TOKEN_NONE = 0;
    public const int TOKEN_CURLY_OPEN = 1;
    public const int TOKEN_CURLY_CLOSE = 2;
    public const int TOKEN_SQUARED_OPEN = 3;
    public const int TOKEN_SQUARED_CLOSE = 4;
    public const int TOKEN_COLON = 5;
    public const int TOKEN_COMMA = 6;
    public const int TOKEN_STRING = 7;
    public const int TOKEN_NUMBER = 8;
    public const int TOKEN_TRUE = 9;
    public const int TOKEN_FALSE = 10;
    public const int TOKEN_NULL = 11;

    private const int BUILDER_CAPACITY = 2000;

    /// <summary>
    /// Parses the string json into a value
    /// </summary>
    /// <param name="json">A JSON string.</param>
    /// <returns>A List, a Dictionary, a double, a string, null, true, or false</returns>
    public static object JsonDecode(string json)
    {
        bool success = true;
        return JsonDecode(json, ref success);
    }

    /// <summary>
    /// Parses the string json into a value; and fills 'success' with the successfullness of the parse.
    /// </summary>
    /// <param name="json">A JSON string.</param>
    /// <param name="success">Successful parse?</param>
    /// <returns>A List, a Dictionary, a double, a string, null, true, or false</returns>
    public static object JsonDecode(string json, ref bool success)
    {
        success = true;
        if (json != null)
        {
            char[] charArray = json.ToCharArray();
            int index = 0;
            object value = ParseValue(charArray, ref index, ref success);
            return value;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Converts a Dictionary / List object into a JSON string
    /// </summary>
    /// <param name="json">A Dictionary / List</param>
    /// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
    public static string JsonEncode(object json)
    {
        StringBuilder builder = new StringBuilder(BUILDER_CAPACITY);
        bool success = SerializeValue(json, builder);
        return (success ? builder.ToString() : null);
    }

    private static Dictionary<string, object> ParseObject(char[] json, ref int index, ref bool success)
    {
        Dictionary<string, object> table = new Dictionary<string, object>();
        int token;

        // {
        NextToken(json, ref index);

        bool done = false;
        while (!done)
        {
            token = LookAhead(json, index);
            if (token == GenericsJSONParser.TOKEN_NONE)
            {
                success = false;
                return null;
            }
            else if (token == GenericsJSONParser.TOKEN_COMMA)
            {
                NextToken(json, ref index);
            }
            else if (token == GenericsJSONParser.TOKEN_CURLY_CLOSE)
            {
                NextToken(json, ref index);
                return table;
            }
            else
            {
                // name
                string name = ParseString(json, ref index, ref success);
                if (!success)
                {
                    success = false;
                    return null;
                }

                // :
                token = NextToken(json, ref index);
                if (token != GenericsJSONParser.TOKEN_COLON)
                {
                    success = false;
                    return null;
                }

                // value
                object value = ParseValue(json, ref index, ref success);
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

    private static List<object> ParseArray(char[] json, ref int index, ref bool success)
    {
        List<object> array = new List<object>();

        // [
        NextToken(json, ref index);

        bool done = false;
        while (!done)
        {
            int token = LookAhead(json, index);
            if (token == GenericsJSONParser.TOKEN_NONE)
            {
                success = false;
                return null;
            }
            else if (token == GenericsJSONParser.TOKEN_COMMA)
            {
                NextToken(json, ref index);
            }
            else if (token == GenericsJSONParser.TOKEN_SQUARED_CLOSE)
            {
                NextToken(json, ref index);
                break;
            }
            else
            {
                object value = ParseValue(json, ref index, ref success);
                if (!success)
                {
                    return null;
                }

                array.Add(value);
            }
        }

        return array;
    }

    private static object ParseValue(char[] json, ref int index, ref bool success)
    {
        switch (LookAhead(json, index))
        {
            case GenericsJSONParser.TOKEN_STRING:
                return ParseString(json, ref index, ref success);
            case GenericsJSONParser.TOKEN_NUMBER:
                return ParseNumber(json, ref index, ref success);
            case GenericsJSONParser.TOKEN_CURLY_OPEN:
                return ParseObject(json, ref index, ref success);
            case GenericsJSONParser.TOKEN_SQUARED_OPEN:
                return ParseArray(json, ref index, ref success);
            case GenericsJSONParser.TOKEN_TRUE:
                NextToken(json, ref index);
                return true;
            case GenericsJSONParser.TOKEN_FALSE:
                NextToken(json, ref index);
                return false;
            case GenericsJSONParser.TOKEN_NULL:
                NextToken(json, ref index);
                return null;
            case GenericsJSONParser.TOKEN_NONE:
                break;
        }

        success = false;
        return null;
    }

    private static string ParseString(char[] json, ref int index, ref bool success)
    {
        StringBuilder s = new StringBuilder(BUILDER_CAPACITY);
        char c;

        EatWhitespace(json, ref index);

        // "
        c = json[index++];

        bool complete = false;
        while (!complete)
        {
            if (index == json.Length)
            {
                break;
            }

            c = json[index++];
            if (c == '"')
            {
                complete = true;
                break;
            }
            else if (c == '\\')
            {
                if (index == json.Length)
                {
                    break;
                }
                c = json[index++];
                if (c == '"')
                {
                    s.Append('"');
                }
                else if (c == '\\')
                {
                    s.Append('\\');
                }
                else if (c == '/')
                {
                    s.Append('/');
                }
                else if (c == 'b')
                {
                    s.Append('\b');
                }
                else if (c == 'f')
                {
                    s.Append('\f');
                }
                else if (c == 'n')
                {
                    s.Append('\n');
                }
                else if (c == 'r')
                {
                    s.Append('\r');
                }
                else if (c == 't')
                {
                    s.Append('\t');
                }
                else if (c == 'u')
                {
                    int remainingLength = json.Length - index;
                    if (remainingLength >= 4)
                    {
                        // Parse the 32 bit hex into an integer codepoint
                        uint codePoint;
                        if (!(success = UInt32.TryParse(new string(json, index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint)))
                        {
                            return "";
                        }
                        // Convert the integer codepoint to a unicode char and add to string
                        try
                        {
                            s.Append(Char.ConvertFromUtf32((int)codePoint));
                        }
                        catch (Exception e)
                        {
                            MDebug.LogError($"Failed to convert UTF32 codepoint '{codePoint}'. Got exception: {e?.ToString()}");
                        }
                        // Skip 4 chars
                        index += 4;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                s.Append(c);
            }
        }

        if (!complete)
        {
            success = false;
            return null;
        }

        return s.ToString();
    }

    private static double ParseNumber(char[] json, ref int index, ref bool success)
    {
        EatWhitespace(json, ref index);

        int lastIndex = GetLastIndexOfNumber(json, index);
        int charLength = (lastIndex - index) + 1;

        double number;
        success = Double.TryParse(new string(json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out number);

        index = lastIndex + 1;
        return number;
    }

    private static int GetLastIndexOfNumber(char[] json, int index)
    {
        int lastIndex;

        for (lastIndex = index; lastIndex < json.Length; lastIndex++)
        {
            if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1)
            {
                break;
            }
        }
        return lastIndex - 1;
    }

    private static void EatWhitespace(char[] json, ref int index)
    {
        for (; index < json.Length; index++)
        {
            if (" \t\n\r".IndexOf(json[index]) == -1)
            {
                break;
            }
        }
    }

    private static int LookAhead(char[] json, int index)
    {
        int saveIndex = index;
        return NextToken(json, ref saveIndex);
    }

    private static int NextToken(char[] json, ref int index)
    {
        EatWhitespace(json, ref index);

        if (index == json.Length)
        {
            return GenericsJSONParser.TOKEN_NONE;
        }

        char c = json[index];
        index++;
        switch (c)
        {
            case '{':
                return GenericsJSONParser.TOKEN_CURLY_OPEN;
            case '}':
                return GenericsJSONParser.TOKEN_CURLY_CLOSE;
            case '[':
                return GenericsJSONParser.TOKEN_SQUARED_OPEN;
            case ']':
                return GenericsJSONParser.TOKEN_SQUARED_CLOSE;
            case ',':
                return GenericsJSONParser.TOKEN_COMMA;
            case '"':
                return GenericsJSONParser.TOKEN_STRING;
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
                return GenericsJSONParser.TOKEN_NUMBER;
            case ':':
                return GenericsJSONParser.TOKEN_COLON;
        }
        index--;

        int remainingLength = json.Length - index;

        // false
        if (remainingLength >= 5)
        {
            if (json[index] == 'f' &&
              json[index + 1] == 'a' &&
              json[index + 2] == 'l' &&
              json[index + 3] == 's' &&
              json[index + 4] == 'e')
            {
                index += 5;
                return GenericsJSONParser.TOKEN_FALSE;
            }
        }

        // true
        if (remainingLength >= 4)
        {
            if (json[index] == 't' &&
              json[index + 1] == 'r' &&
              json[index + 2] == 'u' &&
              json[index + 3] == 'e')
            {
                index += 4;
                return GenericsJSONParser.TOKEN_TRUE;
            }
        }

        // null
        if (remainingLength >= 4)
        {
            if (json[index] == 'n' &&
              json[index + 1] == 'u' &&
              json[index + 2] == 'l' &&
              json[index + 3] == 'l')
            {
                index += 4;
                return GenericsJSONParser.TOKEN_NULL;
            }
        }

        return GenericsJSONParser.TOKEN_NONE;
    }

    private static bool SerializeValue(object value, StringBuilder builder)
    {
        bool success = true;

        if (value is string)
        {
            success = SerializeString((string)value, builder);
        }
        else if (value is GenericJSONDictionaryBlob)
        {
            GenericJSONDictionaryBlob dict = value as GenericJSONDictionaryBlob;
            success = SerializeObject(dict.blob, builder);
        }
        else if (value is Dictionary<string, object>)
        {
            success = SerializeObject((Dictionary<string, object>)value, builder);
        }
        else if (value is List<object>)
        {
            success = SerializeArray((List<object>)value, builder);
        }
        else if (IsNumeric(value))
        {
            success = SerializeNumber(Convert.ToDouble(value), builder);
        }
        else if ((value is Boolean) && ((Boolean)value == true))
        {
            builder.Append("true");
        }
        else if ((value is Boolean) && ((Boolean)value == false))
        {
            builder.Append("false");
        }
        else if (value == null)
        {
            builder.Append("null");
        }
        else
        {
            success = false;
        }
        return success;
    }

    private static bool SerializeObject(Dictionary<string, object> anObject, StringBuilder builder)
    {
        builder.Append("{");

        bool first = true;

        List<string> keyList = new List<string>();
        keyList.AddRange(anObject.Keys);

        for (int i = 0; i < anObject.Keys.Count; i++)
        {
            string key = keyList[i];
            object value = anObject[key];
            if (!first)
            {
                builder.Append(",\n");
            }

            SerializeString(key, builder);
            builder.Append(":");
            if (!SerializeValue(value, builder))
            {
                MDebug.LogBlue($"CANNOT SERIALIZE: {key} {value}");
                return false;
            }

            first = false;
        }

        builder.Append("}");
        return true;
    }

    private static bool SerializeArray(List<object> anArray, StringBuilder builder)
    {
        builder.Append("[");

        bool first = true;
        for (int i = 0; i < anArray.Count; i++)
        {
            object value = anArray[i];

            if (!first)
            {
                builder.Append(",\n");
            }

            if (!SerializeValue(value, builder))
            {
                return false;
            }

            first = false;
        }

        builder.Append("]");
        return true;
    }

    private static bool SerializeString(string aString, StringBuilder builder)
    {
        builder.Append("\"");

        char[] charArray = aString.ToCharArray();
        for (int i = 0; i < charArray.Length; i++)
        {
            char c = charArray[i];
            if (c == '"')
            {
                builder.Append("\\\"");
            }
            else if (c == '\\')
            {
                builder.Append("\\\\");
            }
            else if (c == '\b')
            {
                builder.Append("\\b");
            }
            else if (c == '\f')
            {
                builder.Append("\\f");
            }
            else if (c == '\n')
            {
                builder.Append("\\n");
            }
            else if (c == '\r')
            {
                builder.Append("\\r");
            }
            else if (c == '\t')
            {
                builder.Append("\\t");
            }
            else
            {
                int codepoint = Convert.ToInt32(c);
                if ((codepoint >= 32) && (codepoint <= 126))
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                }
            }
        }

        builder.Append("\"");
        return true;
    }

    private static bool SerializeNumber(double number, StringBuilder builder)
    {
        builder.Append(Convert.ToString(number, CultureInfo.InvariantCulture));
        return true;
    }

    /// <summary>
    /// Determines if a given object is numeric in any way
    /// (can be integer, double, null, etc).
    /// </summary>
    private static bool IsNumeric(object o)
    {
        double result;

        return (o == null) ? false : Double.TryParse(o.ToString(), out result);
    }
}
