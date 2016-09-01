using System;

namespace CustomTFIDF
{
    public class QuickSelect
    {
        public static Tuple<string, double> quickselect(Tuple<string, double>[] G, int k)
        {
            return quickselect(G, 0, G.Length - 1, k - 1);
        }

        public static double NormalSelect(double[] G, int k)
        {
            Array.Sort(G);
            Array.Reverse(G);
            return G[k - 1];
        }
        
        private static Tuple<string, double> quickselect(Tuple<string, double>[] G, int first, int last, int k)
        {
            if (first <= last)
            {
                int pivot = partition(G, first, last);
                if (pivot == k)
                {
                    return G[k];
                }
                if (pivot > k)
                {
                    return quickselect(G, first, pivot - 1, k);
                }
                return quickselect(G, pivot + 1, last, k);
            }
            return null;
        }

        private static int partition(Tuple<string, double>[] G, int first, int last)
        {
            int pivot = first + new Random().Next(last - first + 1);
            swap(G, last, pivot);
            for (int i = first; i < last; i++)
            {
                if (G[i].Item2 > G[last].Item2)
                {
                    swap(G, i, first);
                    first++;
                }
            }
            swap(G, first, last);
            return first;
        }

        private static void swap(Tuple<string, double>[] G, int x, int y)
        {
            Tuple<string, double> tmp = G[x];
            G[x] = G[y];
            G[y] = tmp;
        }
    }
}