using System.Collections.Generic;

namespace CustomTFIDF
{
    public class MegaDictionary
    {
        private static Dictionary<string, int> _megaDictionary;

        public MegaDictionary()
        {
            _megaDictionary = new Dictionary<string, int>();
            // initialize new dictionary
        }

        public static void ResetDictionary()
        {
            _megaDictionary = new Dictionary<string, int>();
        }
        public static void AddToDictionary(string key)
        {
            if (_megaDictionary.ContainsKey(key))
            {
                _megaDictionary[key] = _megaDictionary[key] += 1;
            }
            else
            {
                _megaDictionary[key] = 1;
            }
        }

        public static List<string> ReturnKeysList()
        {
            return new List<string>(_megaDictionary.Keys);
        }

        public static double ReturnTermFrequency(string word)
        {
            int value;

            if (_megaDictionary.TryGetValue(word, out value))
            {
                return value;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Removes all entries in dictionary that have a value of 1 (removing all terms that show up in only 
        /// one document in the corpus, which dramatically reduces the number of entries and hence the size of each
        /// TFIDF vector -- most of these entries are typos / gibberish and removing them doesn't change the output to 
        /// any large extent)
        /// </summary>
        public static void CleanseDictionary()
        {
            List<string> removals = new List<string>();

            foreach (KeyValuePair<string, int> entry in _megaDictionary)
            {
                // do something with entry.Value or entry.Key
                if (entry.Value < 3) // there exists only one document with this term - high probability it is a typo
                {
                    removals.Add(entry.Key);
                }
            }

            foreach (var word in removals)
            {
                _megaDictionary.Remove(word);
            }
        }
    }
}