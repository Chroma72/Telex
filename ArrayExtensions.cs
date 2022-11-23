using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telex
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Returns a subset of an array. Use sparingly as it does allocate.
        /// </summary>
        public static T[] SubArray<T>(this T[] array, int offset, int length)
        {
            T[] result = new T[length];
            Array.Copy(array, offset, result, 0, length);
            return result;
        }

        /// <summary>
        /// Checks to see if an array is null or has a length of 0.
        /// </summary>
        public static bool IsNullOrEmpty(this Array array)
        {
            return (array == null || array.Length == 0);
        }
    }
}
