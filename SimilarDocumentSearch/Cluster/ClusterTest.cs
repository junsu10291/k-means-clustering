using System.Collections.Generic;

namespace CustomTFIDF
{
    public class ClusterTest
    {
        public Dictionary<int, double> CentroidDictionary { get; set; }
        public List<int> Documents { get; set; }

        public ClusterTest(Dictionary<int, double> centroid)
        {
            CentroidDictionary = centroid;
            Documents = new List<int>();
        }
    }
}