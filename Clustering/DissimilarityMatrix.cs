using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AlternativeSoft.Sec.SecDailyUpdater.Clustering
{
    public class DissimilarityMatrix
    {
        // list to store distance values from a pair of clusters Dictionary<ClusterPair, Distance>
        private readonly ConcurrentDictionary<ClusterPair, double> distanceMatrix;

        public DissimilarityMatrix()
        {
            distanceMatrix = new ConcurrentDictionary<ClusterPair, double>(new ClusterPair.EqualityComparer());
        }

        public void AddClusterPairAndDistance(ClusterPair clusterPair, double distance)
        {
            distanceMatrix.TryAdd(clusterPair, distance);
        }

        public void RemoveClusterPair(ClusterPair clusterPair)
        {
            double outvalue;

            if (distanceMatrix.ContainsKey(clusterPair))
            {
                distanceMatrix.TryRemove(clusterPair, out outvalue);
            }
            else
            {
                distanceMatrix.TryRemove(new ClusterPair(clusterPair.Cluster2, clusterPair.Cluster1), out outvalue);
            }
        }

        // get the closest cluster pair (i.e., min cluster pair distance). it is also important to reduce computational time
        public ClusterPair GetClosestClusterPair()
        {
            return distanceMatrix.Aggregate((target, x) => x.Value > target.Value ? target : x).Key;
        }

        // get the distance value from a cluster pair. THIS METHOD DEPENDS ON THE EqualityComparer IMPLEMENTATION IN ClusterPair CLASS
        public double ReturnClusterPairDistance(ClusterPair clusterPair)
        {
            // look in distance matrix if there is an input of cluster1 and cluster2 (remember that ClusterPair has two childs cluster1 and cluster2)
            return distanceMatrix.ContainsKey(clusterPair) 
                ? distanceMatrix[clusterPair] 
                : distanceMatrix[new ClusterPair(clusterPair.Cluster2, clusterPair.Cluster1)];
        }
    }
}