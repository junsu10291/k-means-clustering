using System.Collections.Generic;

namespace CustomTFIDF
{
    public class Document
    {
        public string Id { get; set; }
        private Dictionary<string, int> _termFreqDictionary;

        public Document(Dictionary<string, int> dict, string id = "")
        {
            _termFreqDictionary = dict;
            Id = id;
        }

        public int ReturnFrequency(string key)
        {
            int value;

            if (_termFreqDictionary.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }

        public int UniqueWordsFreq()
        {
            return _termFreqDictionary.Count;
        }

        public List<string> ReturnKeysList()
        {
            return new List<string>(_termFreqDictionary.Keys);
        }
    }
}