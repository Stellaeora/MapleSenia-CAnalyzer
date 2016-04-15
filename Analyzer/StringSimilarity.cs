using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer {
    /// <summary>
    /// Contains approximate string matching using the Levenshtein distance algorithm.
    /// </summary>
    static class StringSimilarity {
        /// <summary>
        /// Determine the percent similarity between two given strings.
        /// </summary>
        /// <returns>A number between 0 and 1 that measures similarity.</returns>
        public static double Compute(string s1, string s2) {
            int maxBlockSize = 12000;
            string longer = s1, shorter = s2;
            if (s1.Length < s2.Length) { // longer should always have greater length
                longer = s2;
                shorter = s1;
            }
            int longerLength = longer.Length;
            if (longerLength == 0) { return 1.0; /* both strings are zero length */ }

            //String length is too large to handle without throwing an OutOfMemoryException -- split up the strings and average the equalities
            //While this will result in some error in the result, for very large texts this should be insignificant.
            if (longerLength > maxBlockSize) {
                //Split the strings into blocks and evaluate each block separately
                int numBlocksLong = (longerLength / maxBlockSize) + 1;
                int numBlocksShort = (shorter.Length / maxBlockSize) + 1;
                string[] longBlocks = new string[numBlocksLong];
                for (int i = 0; i < numBlocksLong; i++) {
                    int remaining = longer.Length - (i * maxBlockSize);
                    longBlocks[i] = longer.Substring(i * maxBlockSize, maxBlockSize > remaining ? remaining : maxBlockSize);
                }
                string[] shortBlocks = new string[numBlocksShort];
                for (int i = 0; i < numBlocksShort; i++) {
                    int remaining = shorter.Length - (i * maxBlockSize);
                    shortBlocks[i] = shorter.Substring(i * maxBlockSize, maxBlockSize > remaining ? remaining : maxBlockSize);
                }
                int totalEditDistance = 0;
                if (numBlocksLong > numBlocksShort) {
                    int trailing = longer.Length;
                    for (int i = 0; i < numBlocksShort; i++) {
                        totalEditDistance += GetEditDistance(longBlocks[i], shortBlocks[i]);
                        trailing -= maxBlockSize;
                    }
                    totalEditDistance += trailing;
                } else if (numBlocksLong == numBlocksShort) {
                    for (int i = 0; i < numBlocksLong; i++) {
                        totalEditDistance += GetEditDistance(longBlocks[i], shortBlocks[i]);
                    }
                }
                return (longerLength - totalEditDistance) / (double)longerLength;
            } else {
                return (longerLength - GetEditDistance(longer, shorter)) / (double)longerLength;
            }
        }
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int GetEditDistance(string s, string t) {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0) {
                return m;
            }

            if (m == 0) {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++) {
            }

            for (int j = 0; j <= m; d[0, j] = j++) {
            }

            // Step 3
            for (int i = 1; i <= n; i++) {
                //Step 4
                for (int j = 1; j <= m; j++) {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}
