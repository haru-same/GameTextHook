using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryUtils
{
    public static class CollectionExtensions
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static T GetSafely<T>(this List<T> data, int index, T defaultValue = default(T))
        {
            if (data != null && index < data.Count) return data[index];
            return defaultValue;
        }

        public static int FindInRange<T>(this List<T> data, int start, int count, T value)
        {
            for(int i = start; i < Math.Min(data.Count, start + count); i++)
            {
                if (data[i].Equals(value)) return i;
            }
            return -1;
        }

        public static int FindInRange<T>(this List<T> data, int start, int count, Func<T, bool> compare)
        {
            for (int i = start; i < Math.Min(data.Count, start + count); i++)
            {
                if (compare(data[i])) return i;
            }
            return -1;
        }
    }
}
