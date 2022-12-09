using System;

namespace Ruzzie.SimpleReports.Reading;

public static class StringIdExtensions
{
    /// <summary>
    /// Strips a string of characters that are not [^a-zA-Z0-9-]*, to uppercase, trims the start and replace all spaces with dashes.
    /// </summary>
    /// <param name="str">The string to convert to an id string.</param>
    /// <returns>The stripped and converted string</returns>
    public static string ToStringId(this ReadOnlySpan<char> str)
    {
        if (str.IsEmpty)
        {
            return string.Empty;
        }

        var inputLength = str.Length;
        var appendIndex = 0;

        //max size in chars
        const int maxStackSize = 128;

        //Use a stackalloc when the input string is smaller than the maxStackSize; else use an array allocation
        var buffer = inputLength < maxStackSize ? stackalloc char[maxStackSize] : new char[inputLength];

        for (var i = 0; i < inputLength; ++i)
        {
            var c = str[i];

            //In the leading part
            if (appendIndex == 0 && char.IsWhiteSpace(c))
            {
                //Trim start
                continue;
            }

            if (97 <= c && c <= 122) //a-z
            {
                buffer[appendIndex++] = (char) (c - 32); //to uppercase
            }
            else if (65 <= c && c <= 90) //A-Z
            {
                buffer[appendIndex++] = c;
            }
            else if (48 <= c && c <= 57) //0-9
            {
                buffer[appendIndex++] = c;
            }
            else if (32 == c) //space
            {
                buffer[appendIndex++] = (char) 45; // replace space with dash
            }
            else if (45 == c) //-
            {
                buffer[appendIndex++] = c;
            }
        }

        return new string(buffer.Slice(0, appendIndex));
    }
}