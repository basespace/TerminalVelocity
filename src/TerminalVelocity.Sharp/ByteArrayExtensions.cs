using System.Collections.Generic;

namespace Illumina.TerminalVelocity
{

    /// <summary>
    /// Find the index of a bytearray given a search bytearray
    /// </summary>
    /// <see cref="http://stackoverflow.com/questions/283456/byte-array-pattern-search"/>
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Zero-base index of they location of the candidate bytearray
        /// </summary>
        /// <param name="self"></param>
        /// <param name="candidate"></param>
        /// <returns>zero-based position or -1</returns>
        public static int IndexOf(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return -1;

            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                return i;
            }
            return -1;
        }

        private static readonly int[] Empty = new int[0];

        public static int[] IndexOfAny(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        private static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                   || candidate == null
                   || array.Length == 0
                   || candidate.Length == 0
                   || candidate.Length > array.Length;
        }

        private static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

    }


}

