using System.Collections.Generic;

namespace CustomTFIDF
{
    public class VectorUtil
    {
        /// <summary>
        /// Calculates the dot product of two vectors
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static double dotProd(double[] v1, double[] v2)
        {
            double prod = 0;
            for (int i = 0; i < v1.Length && i < v2.Length; i++)
            {
                prod += v1[i] * v2[i];
            }
            return prod;
        }

        public static double dotProductDictionary(Dictionary<int, double> d1, Dictionary<int, double> d2)
        {
            double sum = 0.0;
            foreach (var key in d1.Keys)
            {
                if (d2.ContainsKey(key))
                {
                    sum += d1[key] * d2[key];
                }
            }
            return sum;
        }
    }
}