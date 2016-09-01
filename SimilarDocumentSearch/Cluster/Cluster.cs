using System.Collections.Generic;

namespace CustomTFIDF
{
    public class Cluster
    {
        public Dictionary<int, double> CentroidDictionary { get; set; }
        public List<int> Documents { get; set; }

        public Cluster(Dictionary<int, double> centroid)
        {
            CentroidDictionary = centroid;
            Documents = new List<int>();
        }
    }
}