using System.Collections.Generic;

namespace CustomTFIDF
{
    public class Cluster
    {
        public double[] CentroidVector { get; set; }
        public List<int> Documents { get; set; }

        public Cluster(double[] centroid)
        {
            CentroidVector = centroid;
            Documents = new List<int>();
        }
    }
}