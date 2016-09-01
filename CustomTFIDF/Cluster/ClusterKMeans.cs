using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CustomTFIDF
{
    public class ClusterKMeans
    {
        private int _k;
        private List<Document> _documents;
        private double[][] _TFIDFVectors;

        private List<Cluster> _clusters;
        private int _maxIter;
        private int _curIter;
        private Dictionary<int, string> _indexToTitles;
        Random random;
        private List<Cluster> _initClustersChosenSoFar;
        private double[] _distanceSqToClosestCentroid;
        private Boolean _hasChanged = true;

        private List<double> _kMeasure = new List<double>();

        private double _previousSK = 0.0;
        private double _previousAK = 0.0;

        // when we don't know k
        public ClusterKMeans(int maxIter, string[] documents, string[] titles)
        {
            // create new parser and create documents
            Parser parser = new Parser();
            List<Document> documentList = parser.parseMultipleDocs(documents.ToList(), titles.ToList());

            _documents = documentList;
            List<string> titlesList = titles.ToList();
            random = new Random();

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
            _TFIDFVectors = CalcTFIDF.LshTfidf(_documents);
        }

        public void GenerateClustersWithoutK()
        {
            int numData = _documents.Count;
            //int ruleOfThumbK = (int) Math.Sqrt(numData/2);

            // for now, lets just go up until the rule of thumb k
            for (int i = 1; i <= 10; i++)
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

            // call generate
            GenerateClustersWithK();

            // after done, generate distortion
            Debug.WriteLine("SSE: " + DistortionSum());
        }

        private void GenerateClustersWithK()
        {
            while (_curIter < _maxIter && _hasChanged) //check condition for break point of algorithm
            {
                _curIter++;
                //TestOutPut();
                Debug.WriteLine(_curIter + " iterations: ");
                UpdateClusterDocuments(); // takes too long
                UpdateCentroidMeans();
            }
            //TestOutPut();
            Debug.WriteLine("Done");
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

        private void UpdateClusterDocuments()
        {
            // For each cluster, reset the Documents list
            foreach (var cluster in _clusters)
            {
                cluster.Documents = new List<int>();
            }

            // Assign each document to a cluster that is "closest" using cosine similarity -- bigger is closer!
            for (int i = 0; i < _TFIDFVectors.Length; i++) // for each document vector
            {
                double[] currentDocVector = _TFIDFVectors[i];
                int argmax = 0;
                double max = 0; // from 0 ~ 1
                
                for (int j = 0; j < _clusters.Count; j++) // loop through clusters
                {
                    double[] currentClusterVector = _clusters[j].CentroidVector;
                    double cosSim = CosineSimilarity(currentDocVector, currentClusterVector); // higher --> more similar

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
            List<double[]> oldCentroidVectors = new List<double[]>();

            for (int i = 0; i < _clusters.Count; i++) // for each cluster
            {
                // add to old
                oldCentroidVectors.Add(_clusters[i].CentroidVector);
                double[] updatedVector = new double[_clusters[i].CentroidVector.Length];

                foreach (var documentIndex in _clusters[i].Documents) // for each document in cluster
                {
                    double[] documentVector = _TFIDFVectors[documentIndex];
                    for (int j = 0; j < documentVector.Length; j++) // for each index in vector
                    {
                        updatedVector[j] += documentVector[j];
                    }
                }

                // Divide by how many documents were in the cluster to calculate average
                for (int k = 0; k < updatedVector.Length; k++)
                {
                    updatedVector[k] = updatedVector[k] / _clusters[i].Documents.Count;
                }

                // update centroid's vector
                _clusters[i].CentroidVector = updatedVector;
            }

            // Check that at least one has changed!
            for (int l = 0; l < oldCentroidVectors.Count; l++)
            {
                // at least one has changed, return true
                if (!oldCentroidVectors[l].SequenceEqual(_clusters[l].CentroidVector))
                {
                    _hasChanged = true;
                    return;
                }
            }

            // none have changed, return false -- stops algorithm
            _hasChanged = false;
        }
        
        /// <summary>
        /// Choosing the first k centroids via the k-means++ algorithm: http://theory.stanford.edu/~sergei/papers/kMeansPP-soda.pdf
        /// </summary>
        /// <returns></returns>
        private List<Cluster> InitCentroidsPlusPlus()
        {
            // initialize list of clusters to add to
            _initClustersChosenSoFar = new List<Cluster>();

            // choose initial center c_1 uniformly at random from X (= set containing all document vectors, x_1, x_2, ..., x_n)
            int randomInt = random.Next(_TFIDFVectors.Length);
            Cluster c1 = new Cluster(_TFIDFVectors[randomInt]);
            _initClustersChosenSoFar.Add(c1);

            Debug.WriteLine("Adding initial clusters!");
            Debug.WriteLine(randomInt);

            while (_initClustersChosenSoFar.Count < _k)
            {
                // initialize array that maps document index --> distance squared to closest centroid
                _distanceSqToClosestCentroid = new double[_documents.Count];

                // for each document, calculate the squared distance to the closest centroid (out of those initialized so far)
                for (int i = 0; i < _documents.Count; i++)
                {
                    _distanceSqToClosestCentroid[i] = DistanceSqToClosestCentroid(_TFIDFVectors[i]);
                }

                // choose next center, c_i, by selecting c_i = x' in X with custom probability function
                int nextCenter = ReturnIntFromProbabilityFunction(_distanceSqToClosestCentroid);
                _initClustersChosenSoFar.Add(new Cluster(_TFIDFVectors[nextCenter]));
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
        private double DistanceSqToClosestCentroid(double[] tfidfVector)
        {
            Cluster closestCluster = null;
            double minDist = Double.MaxValue;
            
            foreach (var cluster in _initClustersChosenSoFar)
            {
                // need to convert consine similarity into a distance measure, distance = abs(cosinesimilarity - 1)...mathematically sound??
                //double distance = Math.Abs(CosineSimilarity(tfidfVector, cluster.CentroidVector) - 1);

                // distance as theta
                //Debug.WriteLine("cosine: " + CosineSimilarity(tfidfVector, cluster.CentroidVector));
                double cosine = CosineSimilarity(tfidfVector, cluster.CentroidVector) > 1 ? 1 : CosineSimilarity(tfidfVector, cluster.CentroidVector);
                
                double distance = Math.Acos(cosine);
                //Debug.WriteLine("distance: " + distance);
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
                    //double distance = Math.Abs(CosineSimilarity(_TFIDFVectors[document], cluster.CentroidVector) - 1);
                    double distance = Math.Acos(CosineSimilarity(_TFIDFVectors[document], cluster.CentroidVector));
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
        public static double CosineSimilarity(double[] vec1, double[] vec2)
        {
            double dotProduct = Runner.dotProd(vec1, vec2);
            return dotProduct;
        }
        
        /// <summary>
        /// Calculates the magnitude of a vector
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        private double VectorMagnitude(double[] vec)
        {
            double squareSum = 0.0;

            foreach (var doub in vec)
            {
                squareSum += Math.Pow(doub, 2);
            }

            return Math.Sqrt(squareSum);
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
            } else 
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
            } else if (_k == 2)
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
    }
}