/*
 * Copyright 2009-17 Williams Technologies Limited.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Kajbity is a trademark of Williams Technologies Limited.
 *
 * http://www.kajabity.com
 */

using System;
using System.Linq;

namespace Kajabity.Tools.Csv.Tests
{
    /// <summary>
    /// Utility methods for test assertions and array comparisons.
    /// </summary>
    public static class TestUtils
    {
        /// <summary>
        /// Convert an array of strings to a readable string.
        /// Example: ["a", "b", null] → {"a", "b", ""}
        /// </summary>
        public static string ToString(string[] strings) =>
            strings == null
                ? "{}"
                : "{" + string.Join(", ", strings.Select(s => $"\"{s ?? ""}\"")) + "}";

        /// <summary>
        /// Return the string, or "" if it is null.
        /// </summary>
        public static string NoNull(string s) => s ?? "";

        /// <summary>
        /// Returns true if two string arrays contain the same values (order-sensitive).
        /// </summary>
        public static bool CompareStringArray(string[] a, string[] b) =>
            a == null && b == null
                ? true
                : a != null && b != null && a.SequenceEqual(b);
    }
}
