using System;
using TinyToml.Parsing;
using TinyToml.Types;

namespace TinyToml
{
    public static class Toml
    {
        public static TomlDoc Parse(string toml)
        {
            return TomlParser.Parse(toml);
        }

        /// <summary>
        /// Tries to read the <paramref name="keyName"/> value as a <see cref="TomlString"/> and returns the value.
        /// When the <paramref name="keyName"/> is not found or isn't a <see cref="TomlString"/> returns <paramref name="default"/>.
        /// </summary>
        /// <param name="table">the table to read the key from</param>
        /// <param name="keyName">the name of the key</param>
        /// <param name="default">the value to return when the key is not found or is not a string</param>
        /// <returns>the value or default</returns>
        public static string ReadStringValueOr(this TomlTable table, string keyName, string @default = "")
        {
            if (table.TryGetValue<TomlString>(keyName, out var stringValue))
            {
                return stringValue.Value;
            }

            return @default;
        }

        public static string ReadStringValueOrThrow(this TomlTable table, string keyName, string errorMessage)
        {
            if (table.TryGetValue<TomlString>(keyName, out var stringValue))
            {
                return stringValue.Value;
            }

            throw new ArgumentException(errorMessage, keyName);
        }

        /// <summary>
        /// Tries to read the <paramref name="keyName"/> value as a <see cref="TomlBoolean"/> and returns the value.
        /// When the <paramref name="keyName"/> is not found or isn't a <see cref="TomlBoolean"/> returns <paramref name="default"/>.
        /// </summary>
        /// <param name="table">the table to read the key from</param>
        /// <param name="keyName">the name of the key</param>
        /// <param name="default">the value to return when the key is not found or is not a <see cref="TomlBoolean"/></param>
        /// <returns>the value or default</returns>
        public static bool ReadBoolValueOrDefault(this TomlTable table, string keyName, bool @default = false)
        {
            if (table.TryGetValue<TomlBoolean>(keyName, out var tomlBoolean))
            {
                return tomlBoolean.Value;
            }
            return @default;
        }
    }
}