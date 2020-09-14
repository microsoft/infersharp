// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;

namespace Cilsil.Extensions
{
    internal static class DictionaryExtensions
    {
        /// <summary>
        /// Gets the value for a given key, or creates and inserts it if the key does not exist.
        /// </summary>
        /// <typeparam name="TK">Type of the key.</typeparam>
        /// <typeparam name="TV">Type of the value.</typeparam>
        /// <param name="dictionary">Dictionary to query from.</param>
        /// <param name="key">Lookup key.</param>
        /// <param name="value">Value to insert if key does not exist.</param>
        /// <returns>The value in the dictionary for the given key.</returns>
        public static TV GetOrCreateValue<TK, TV>(this IDictionary<TK, TV> dictionary,
                                                  TK key,
                                                  TV value) => dictionary.GetOrCreateValue(key, () => value);

        /// <summary>
        /// Gets the value for a given key, or creates and inserts it if the key does not exist.
        /// </summary>
        /// <typeparam name="TK">Type of the key.</typeparam>
        /// <typeparam name="TV">Type of the value.</typeparam>
        /// <param name="dictionary">Dictionary to query from.</param>
        /// <param name="key">Lookup key.</param>
        /// <param name="valueProvider">Function that creates a default value if the key
        /// does not exist.</param>
        /// <returns>The value in the dictionary for the given key.</returns>
        public static TV GetOrCreateValue<TK, TV>(this IDictionary<TK, TV> dictionary,
                                                  TK key,
                                                  Func<TV> valueProvider)
        {
            if (!dictionary.TryGetValue(key, out var ret))
            {
                ret = valueProvider();
                dictionary[key] = ret;
            }
            return ret;
        }
    }
}
