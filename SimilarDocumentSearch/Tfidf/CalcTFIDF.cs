using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CustomTFIDF
{
    public class CalcTFIDF
    {
        public static double[][] LshTfidf(List<Document> documents)
        {
            // new megakeyslist
            HashSet<string> megaKeysList = new HashSet<string>();

            int counter = 0;
            foreach (var document in documents)
            {
                List<string> keysList = document.ReturnKeysList();
                List<Tuple<string, double>> documentVector = new List<Tuple<string, double>>();

                Debug.WriteLine("Generating k largest tfidf words for document: " + counter);
                for (int i = 0; i < keysList.Count; i++)
                {
                    string word = keysList[i];
                    if (!MegaDictionary.ReturnTermFrequency(word).Equals(-1))
                    {
                        double tf = document.UniqueWordsFreq() == 0 ? 0 : (double)document.ReturnFrequency(word) / document.UniqueWordsFreq(); // if document has 0 terms it it, return 0
                        double calc = documents.Count / MegaDictionary.ReturnTermFrequency(word);
                        double idf = Math.Log(calc);
                        documentVector.Add(new Tuple<string, double>(word, tf * idf));
                    }
                }
              
                // change into array
                Tuple<string, double>[] docVectorArray = documentVector.ToArray();

                // for now, lets use top 50%
                int k = (int) (documentVector.Count*0.5);
                Tuple<string, double> kthLargest = QuickSelect.quickselect(docVectorArray, k);
                
                foreach (Tuple<string, double> pair in documentVector)
                {
                    if (pair.Item2 >= kthLargest.Item2)
                    { 
                        // add to keys list if tfidf is greater than kth largest
                        megaKeysList.Add(pair.Item1);
                    }
                }
                counter++;
            }

            // now megakeyslist contains only the top 50% tfidf words from each document, change into list to generate an ordering
            List<string> wordsList = megaKeysList.ToList();

            // [][] that will store all document vectors
            double[][] TFIDVectors = new double[documents.Count][];

            //loop through documents again and create vector for each
            for (int j = 0; j < documents.Count; j++)
            {
                Debug.WriteLine("Generating actual vectors for : " + j);
                double[] newDocumentVector = new double[wordsList.Count];

                for(int i = 0; i < wordsList.Count; i++)
                {
                    newDocumentVector[i] = documents[j].ReturnFrequency(wordsList[i]) == 0 ? 0 : 1;
                }
                TFIDVectors[j] = newDocumentVector;
            }

            return TFIDVectors;
        }


        public static double[][] ReturnTFIDFVectors(List<Document> documents)
        {
            // generate list ordering of megadictionary
            List<string> keysList = MegaDictionary.ReturnKeysList();
            List<List<double>> TFIDVectors = new List<List<double>>();

            int counter = 1;
            foreach (var document in documents)
            {
                //Debug.WriteLine("TFIDF vector for document #: " + counter);
                List<double> documentVector = new List<double>();
                // calculate TFDIF vector for document
                foreach (var word in keysList)
                {
                    double tf = document.UniqueWordsFreq() == 0 ? 0 : (double) document.ReturnFrequency(word) / document.UniqueWordsFreq(); // if document has 0 terms it it, return 0
                    double calc = documents.Count / MegaDictionary.ReturnTermFrequency(word);
                    double idf = Math.Log(calc);
                    documentVector.Add(tf * idf);
                } 
                
                TFIDVectors.Add(documentVector);
                counter++;
            }

            // change into double[][] and normalize
            double[][] vectors = TFIDVectors.Select(v => v.ToArray()).ToArray();
            Normalize(vectors);
            return vectors;
        }

        public static Dictionary<int, double>[] ReturnTFIDFDicts(List<Document> documents)
        {
            // generate list ordering of megadictionary
            List<string> keysList = MegaDictionary.ReturnKeysList();
            
            List<Dictionary<int, double>> TFIDFDictionaryList = new List<Dictionary<int, double>>();
            int counter = 1; 

            foreach (var document in documents)
            {
                Debug.WriteLine("TFIDF vector for document #: " + counter);
                Dictionary<int, double> TFIDFDict = new Dictionary<int, double>();

                // calculate TFDIF vector for document
                for (int i = 0; i < keysList.Count; i++)
                {
                    string word = keysList[i];
                    double tf = document.UniqueWordsFreq() == 0 ? 0 : (double)document.ReturnFrequency(word) / document.UniqueWordsFreq(); // if document has 0 terms it it, return 0
                    double calc = documents.Count / MegaDictionary.ReturnTermFrequency(word);
                    double idf = Math.Log(calc);
                    double tfidf = tf * idf;

                    // only add to dictionary if tfidf is not 0
                    if (tfidf != 0)
                    {
                        TFIDFDict.Add(i, tfidf);
                    }
                }

                TFIDFDictionaryList.Add(TFIDFDict);
                counter++;
            }

            // change into array and normalize
            Dictionary<int, double>[] listOfDictionaries = TFIDFDictionaryList.ToArray();
            NormalizeDictionaryArray(listOfDictionaries);
            return listOfDictionaries;
        }

        private static void NormalizeDictionaryArray(Dictionary<int, double>[] inputs)
        {
            foreach (Dictionary<int, double> dictionary in inputs)
            {
                double sumSquare = 0;
                foreach (double d in dictionary.Values)
                {
                    sumSquare += (d * d);
                }
                double sqrtSumSquare = Math.Sqrt(sumSquare);

                if (sqrtSumSquare != 0)
                {
                    List<int> keysList = new List<int>(dictionary.Keys);
                    
                    foreach (var key in keysList)
                    {
                        dictionary[key] = dictionary[key] / sqrtSumSquare;
                    }
                }
            }
        }

        private static void Normalize(double[][] inputs)
        {
            foreach (double[] dArr in inputs)
            {
                double sumSquare = 0;
                foreach (double d in dArr)
                {
                    sumSquare += (d * d);
                }
                double sqrtSumSquare = Math.Sqrt(sumSquare);

                if (sqrtSumSquare != 0)
                {
                    for (int i = 0; i < dArr.Length; i++)
                    {
                        dArr[i] /= sqrtSumSquare;
                    }
                }
            }
        }
    }
}