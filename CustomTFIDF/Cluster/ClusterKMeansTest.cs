using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace CustomTFIDF
{
    public class ClusterKMeansTest
    {
        private int _k;
        private List<Document> _documents;
        private Dictionary<int, double>[] _TFIDFDicts; 

        private List<ClusterTest> _clusters;
        private int _maxIter;
        private int _curIter;
        private Dictionary<int, string> _indexToTitles;
        Random random;
        private List<ClusterTest> _initClustersChosenSoFar;
        private double[] _distanceSqToClosestCentroid;
        private Boolean _hasChanged = true;

        private List<double> _kMeasure = new List<double>();

        private double _previousSK = 0.0;
        private double _previousAK = 0.0;

        private Dictionary<Tuple<int, int>, double> _lowerBoundsDictionary; 

        public ClusterKMeansTest(int maxIter, string[] documents, string[] titles)
        {
            // create new parser and create documents
            Parser parser = new Parser();
            List<Document> documentList = parser.parseMultipleDocs(documents.ToList(), titles.ToList());

            _documents = documentList;
            List<string> titlesList = titles.ToList();
            random = new Random();
            _lowerBoundsDictionary = new Dictionary<Tuple<int, int>, double>();

            // remove empty documents -- TODO Refactor!
            for (int i = 0; i < documentList.Count; i++)
            {
                if (documentList[i].UniqueWordsFreq() == 0) // empty document
                {
                    documentList.RemoveAt(i);
                    titlesList.RemoveAt(i);
                }
            }
            
            // initialize maxiter
            _maxIter = maxIter;

            // initialize and add to dictionary mapping index to document titles
            _indexToTitles = new Dictionary<int, string>();
            for (int i = 0; i < documentList.Count; i++)
            {
                _indexToTitles.Add(i, titlesList[i]);
            }
        }

        public void calcTFIDFVectors()
        {
            // calculate TFIDF Vectors for all documents
            _TFIDFDicts = CalcTFIDF.ReturnTFIDFDicts(_documents);
        }

        public void GenerateClustersWithoutK()
        {
            for (int i = 1; i <= 15; i++)
            {
                Debug.WriteLine("Running cluster generation with " + i + " clusters");
                GenerateClustersWithK(i);
                //Debug.WriteLine("SSE: " + DistortionSum());
                evaluateCurrentFK();
                _previousSK = DistortionSum();
            }

            // initialize
            int argMin = -1;
            double min = Double.MaxValue;

            // find argmin and min from _kMeasure
            for (int i = 0; i < _kMeasure.Count; i++)
            {
                if (_kMeasure[i] < min)
                {
                    argMin = i + 1; // to correct off by one
                    min = _kMeasure[i];
                }
            }

            // print out test stuff
            for (int i = 0; i < _kMeasure.Count; i++)
            {
                Debug.WriteLine("K = " + (i + 1) + ": " + _kMeasure[i]);
            }

            if (min > 0.95) // no clustering = 1 cluster
            {
                Debug.WriteLine("Suggested k is 1, there is no FK lower than 0.95: ");
            }
            else
            {
                Debug.WriteLine("Suggested k is " + argMin + " with an FK of " + min);
            }
        }

        public void GenerateClustersWithK(int k)
        {
            //reset stuff

            // hasChanged
            _hasChanged = true;

            // set curiter
            _curIter = 0;

            // set k
            _k = k;

            // initialize first k centroids with k-means++ algorithm
            _clusters = InitCentroidsPlusPlus();

            GenerateClustersWithK();
            
            //Debug.WriteLine("Done with cluster generation, SSE: " + DistortionSum());
        }

        private void GenerateClustersWithK()
        {
            while (_curIter < _maxIter && _hasChanged) //check condition for break point of algorithm
            {
                _curIter++;
                //TestOutPut();

                var watch = System.Diagnostics.Stopwatch.StartNew();
                UpdateClusterDocuments();
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Debug.WriteLine("updating cluster documents took: "  + elapsedMs + "  ms");

                var watch1 = System.Diagnostics.Stopwatch.StartNew();
                UpdateCentroidMeans();
                watch1.Stop();
                var elapsedMs1 = watch1.ElapsedMilliseconds;
                Debug.WriteLine("updating centroid means took: " + elapsedMs1 + "  ms");
            }
            TestOutPut();
        }

        //TODO : Slow, make CosineSimilarity more efficient
        private void UpdateClusterDocuments()
        {
            // For each cluster, reset the Documents list
            foreach (var cluster in _clusters)
            {
                cluster.Documents = new List<int>();
            }

            // Assign each document to a cluster that is "closest" using cosine similarity -- bigger is closer!
            for (int i = 0; i < _TFIDFDicts.Length; i++) // for each document vector
            {
                Dictionary<int, double> currentDocDictionary = _TFIDFDicts[i];

                int argmax = 0;
                double max = 0; // from 0 ~ 1

                for (int j = 0; j < _clusters.Count; j++) // loop through clusters
                {
                    Dictionary<int, double> currentClusterDictionary = _clusters[j].CentroidDictionary;
                    double cosSim = CosineSimilarity(currentDocDictionary, currentClusterDictionary); 
                    
                    if (cosSim > max) // current cluster is closer, update argmax and max
                    {
                        argmax = j;
                        max = cosSim;
                    }
                }

                // add to max (most similar) cluster's list of documents
                _clusters[argmax].Documents.Add(i);
            }
        }

        /// <summary>
        /// Updates the Centroid means, returns a boolean (true if at least one has changed)
        /// </summary>
        private void UpdateCentroidMeans()
        {
            // so that algorithm checks that centroid means have changed -- the condition for the algorithm to keep running
            _hasChanged = false;
            
            for (int i = 0; i < _clusters.Count; i++) // for each cluster
            {
                ClusterTest currentCluster = _clusters[i];
                Dictionary<int, double> updatedDictionary = new Dictionary<int, double>();
                
                // calculate the updated vector for the centroid (average)
                foreach (var documentIndex in currentCluster.Documents)
                {
                    Dictionary<int, double> currentDocumentDictionary = _TFIDFDicts[documentIndex];

                    foreach (var key in currentDocumentDictionary.Keys)
                    {
                        if (updatedDictionary.ContainsKey(key))
                        {
                            updatedDictionary[key] = updatedDictionary[key] + currentDocumentDictionary[key];
                        }
                        else
                        {
                            updatedDictionary.Add(key, currentDocumentDictionary[key]);
                        }
                    }
                }
                
                // Divide by how many documents were in the cluster to calculate average
                List<int> keysList = new List<int>(updatedDictionary.Keys);
                foreach (var key in keysList)
                {
                    updatedDictionary[key] = updatedDictionary[key] / currentCluster.Documents.Count;
                }

                // if centroid means have not changed yet, check if this one has changed
                if (_hasChanged == false)
                {
                    if (!DictionaryUtils.DictionaryEqual(currentCluster.CentroidDictionary, updatedDictionary))
                    {
                        _hasChanged = true;
                    }
                }

                // actually update centroid for current cluster
                currentCluster.CentroidDictionary = updatedDictionary;
            }
        }

        #region InitCentroids
        /// <summary>
        /// Choosing the first k centroids via the k-means++ algorithm: http://theory.stanford.edu/~sergei/papers/kMeansPP-soda.pdf
        /// REEAAAALLLY SLOW, FIX!
        /// </summary>
        /// <returns></returns>
        private List<ClusterTest> InitCentroidsPlusPlus()
        {
            // initialize list of clusters to add to
            _initClustersChosenSoFar = new List<ClusterTest>();

            // choose initial center c_1 uniformly at random from X (= set containing all document vectors, x_1, x_2, ..., x_n)
            int randomInt = random.Next(_TFIDFDicts.Length);
            ClusterTest c1 = new ClusterTest(_TFIDFDicts[randomInt]);
            _initClustersChosenSoFar.Add(c1);

            Debug.WriteLine("Adding initial clusters!");
            Debug.WriteLine(randomInt);

            while (_initClustersChosenSoFar.Count < _k)
            {
                // initialize array that maps document index --> distance squared to closest centroid
                _distanceSqToClosestCentroid = new double[_documents.Count];

                // for each document, calculate the squared distance to the closest centroid (out of those initialized so far)
                var watch = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < _documents.Count; i++)
                {
                    
                    _distanceSqToClosestCentroid[i] = DistanceSqToClosestCentroid(_TFIDFDicts[i]);
                    
                }
                watch.Stop();
                Debug.WriteLine("Getting distance squareds: " + watch.ElapsedMilliseconds);

                // choose next center, c_i, by selecting c_i = x' in X with custom probability function
                var watch1 = System.Diagnostics.Stopwatch.StartNew();
                int nextCenter = ReturnIntFromProbabilityFunction(_distanceSqToClosestCentroid);
                watch1.Stop();
                Debug.WriteLine("Probability: " + watch1.ElapsedMilliseconds);
                _initClustersChosenSoFar.Add(new ClusterTest(_TFIDFDicts[nextCenter]));
                Debug.WriteLine(nextCenter);
            }

            return _initClustersChosenSoFar;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private int ReturnIntFromProbabilityFunction(double[] array)
        {
            double sum = array.Sum();

            // create cumulative distribution function
            double[] test = new double[array.Length + 1];
            // initialize first index
            test[0] = 0;
            test[1] = array[0];

            for (int i = 2; i < test.Length; i++)
            {
                test[i] = test[i - 1] + array[i - 1];
            }

            test = test.Select(item => item / sum).ToArray();

            double randomDouble = random.NextDouble();

            // now loop through the cdf array
            for (int i = 0; i < test.Length - 1; i++)
            {
                if (randomDouble >= test[i] && randomDouble < test[i + 1])
                {
                    return i;
                }
            }

            Debug.WriteLine("Shouldn't ever come here!!!! CDFInverse");
            return -1;
        }

        /// <summary>
        /// Given a document, computes the shortest distance^2 to the closest centroid (from the ones that we have picked out so far)
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private double DistanceSqToClosestCentroid(Dictionary<int, double> tfidfDictionary)
        {
            ClusterTest closestCluster = null;
            double minDist = Double.MaxValue;

            foreach (var cluster in _initClustersChosenSoFar)
            {
                double cosine = CosineSimilarity(tfidfDictionary, cluster.CentroidDictionary);
                double checkCosine = cosine > 1 ? 1 : cosine;

                double distance = Math.Acos(checkCosine);

                // update closestCluster if closer
                if (distance < minDist)
                {
                    minDist = distance;
                    closestCluster = cluster;
                }
            }

            // Shouldn't be null!
            Debug.Assert(!closestCluster.Equals(null));

            return Math.Pow(minDist, 2);
        }
        #endregion

        /// <summary>
        /// The sum of all distortion, where for each cluster the distortion is the sum of the distances from the documents in that cluster to its centroid
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private double DistortionSum()
        {
            double distortionSum = 0.0;
            // sum of distortions
            foreach (var cluster in _clusters)
            {
                // local distortion for each cluster
                foreach (var document in cluster.Documents)
                {
                    double distance = Math.Acos(CosineSimilarity(_TFIDFDicts[document], cluster.CentroidDictionary));
                    distortionSum += distance;
                }
            }
            return distortionSum;
        }

        /// <summary>
        /// Calculates the cosine similarity (both vectors must be UNIT vectors)
        /// </summary>
        /// <param name="vec1"></param>
        /// <param name="vec2"></param>
        /// <returns></returns>
        public static double CosineSimilarity(Dictionary<int, double> d1, Dictionary<int, double> d2)
        {
            double dotProduct = Runner.testDotProduct2(d1, d2);

            // for error
            if (dotProduct > 1)
            {
                dotProduct = 1;
            }

            return dotProduct;
        }

        /// <summary>
        /// Algorithm to choose optimal k for the dataset, http://www.ee.columbia.edu/~dpwe/papers/PhamDN05-kmeans.pdf
        /// </summary>
        /// <returns></returns>
        private double evaluateCurrentFK()
        {
            double currentFK = 0.0;

            if (_k < 1)
            {
                currentFK = -1.0;
            }
            else if (_k == 1)
            {
                currentFK = 1.0;
            }
            else
            {
                if (_previousSK == 0)
                {
                    currentFK = 1.0;
                }
                else
                {
                    var previousSK = _previousSK;
                    currentFK = DistortionSum() / (previousSK * aK());
                }
            }

            Debug.Assert(!currentFK.Equals(-1.0));

            // add to list
            _kMeasure.Add(currentFK);

            return currentFK;
        }

        private double aK()
        {
            if (_k < 2)
            {
                _previousAK = -1.0;
            }
            else if (_k == 2)
            {
                _previousAK = 1 - ((double)3 / (4 * MegaDictionary.ReturnKeysList().Count)); // set current aK to previous
            }
            else
            {
                _previousAK = _previousAK + ((1 - _previousAK) / 6);
            }

            Debug.Assert(!_previousAK.Equals(-1.0));
            return _previousAK;
        }

        private void TestOutPut()
        {
            Debug.WriteLine(_curIter + " iterations: ");

            Debug.WriteLine("Clusters: ");
            for (int c = 0; c < _clusters.Count; c++)
            {
                Debug.WriteLine("Cluster #" + c);
                string s = "";
                foreach (var documentIndex in _clusters[c].Documents)
                {
                    string sb;
                    if (_indexToTitles.TryGetValue(documentIndex, out sb))
                    {
                        s += sb + ", ";
                    }
                }
                Debug.WriteLine(s);
            }
        }
    }
}
