using System.Collections.Generic;

namespace CustomTFIDF
{
    public class DictionaryUtils
    {
        public static bool DictionaryEqual<TKey, TValue>(IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        {
            return DictionaryEqual(first, second, null);
        }

        public static bool DictionaryEqual<TKey, TValue>(IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second,
            IEqualityComparer<TValue> valueComparer)
        {
            if (first == second) return true;
            if ((first == null) || (second == null)) return false;
            if (first.Count != second.Count) return false;

            valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

            foreach (var kvp in first)
            {
                TValue secondValue;
                if (!second.TryGetValue(kvp.Key, out secondValue)) return false;
                if (!valueComparer.Equals(kvp.Value, secondValue)) return false;
            }
            return true;
        }
    }
}