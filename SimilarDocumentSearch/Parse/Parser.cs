using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace CustomTFIDF
{
    public class Parser
    {
        private Dictionary<string, int> termFreqDict;

        public List<Document> parseMultipleDocs(List<string> docs, List<string> ids)
        {
            List<Document> documentList = new List<Document>();

            for (int i = 0; i < docs.Count; i++)
            {
                documentList.Add(parseDocument(docs[i], ""));
                Debug.WriteLine("Done with document: " + i);
            }
            
            MegaDictionary.CleanseDictionary();

            return documentList;
        }
        
        public Document parseDocument(string line, string id)
        {
            termFreqDict = new Dictionary<string, int>();

            line = line.ToLower();
            line = line.TrimEnd(' ');
            line = Regex.Replace(line, @"\t|\n|\r", "");

            Regex rgx = new Regex("[^a-z0-9 ]"); // keep just alphanumeric characters
            line = rgx.Replace(line, " ");

            line = Regex.Replace(line, string.Format(@"(\p{{L}}{{{0}}})\p{{L}}+", 11), ""); // remove 12 >
            line = Regex.Replace(line, @"\b\w{1,3}\b", ""); // remove words that have three letters or fewer
            line = Regex.Replace(line, @"\s+", " ");  // remove extra whitespace
            
            var noSpaces = line.Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            HashSet<string> uniqueWords = new HashSet<string>();

            Stemmer stemmer = new Stemmer();

            foreach (var s in noSpaces)
            {
                // stem words
                string word = stemmer.stem(s);
                if (!StopWords.stopWordsSet.Contains(word) && !word.Any(c => char.IsDigit(c)))
                {
                    addToLocalDict(word);

                    if (!uniqueWords.Contains(word))
                    {
                        MegaDictionary.AddToDictionary(word);
                        uniqueWords.Add(word);
                    }
                }
            }
            
            return new Document(termFreqDict, id);
        }

        private void addToLocalDict(string word)
        {
            // add words to dictionary
            if (termFreqDict.ContainsKey(word))
            {
                termFreqDict[word] = termFreqDict[word] += 1;
            }
            else
            {
                termFreqDict[word] = 1;
            }
        }
    }
}