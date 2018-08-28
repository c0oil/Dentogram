using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Dentogram.Clustering
{
    public class DissimilarityMatrix
    {
        private readonly ConcurrentDictionary<ClusterNodePair, double> distanceMatrix;

        public DissimilarityMatrix()
        {
            distanceMatrix = new ConcurrentDictionary<ClusterNodePair, double>(new ClusterNodePair.EqualityComparer());
        }

        public void AddClusterPairAndDistance(ClusterNodePair clusterPair, double distance)
        {
            distanceMatrix.TryAdd(clusterPair, distance);
        }

        public void RemoveClusterPair(ClusterNodePair clusterPair)
        {
            double outvalue;

            if (distanceMatrix.ContainsKey(clusterPair))
            {
                distanceMatrix.TryRemove(clusterPair, out outvalue);
            }
            else
            {
                distanceMatrix.TryRemove(new ClusterNodePair(clusterPair.Cluster2, clusterPair.Cluster1), out outvalue);
            }
        }

        // get the closest cluster pair (i.e., min cluster pair distance). it is also important to reduce computational time
        public ClusterNodePair GetClosestClusterPair()
        {
            return distanceMatrix.Aggregate((target, x) => x.Value > target.Value ? target : x).Key;
        }

        // get the distance value from a cluster pair. THIS METHOD DEPENDS ON THE EqualityComparer IMPLEMENTATION IN ClusterPair CLASS
        public double ReturnClusterPairDistance(ClusterNodePair clusterPair)
        {
            return distanceMatrix.ContainsKey(clusterPair) 
                ? distanceMatrix[clusterPair] 
                : distanceMatrix[new ClusterNodePair(clusterPair.Cluster2, clusterPair.Cluster1)]; // Reverse
        }
    }
}