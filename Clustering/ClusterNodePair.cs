using System;
using System.Collections.Generic;

namespace Dentogram.Clustering
{
    public class ClusterNodePair
    {
        public ClusterNode Cluster1 { get; }
        public ClusterNode Cluster2 { get; }

        public ClusterNodePair(ClusterNode cluster1, ClusterNode cluster2)
        {
            if (cluster1 == null)
                throw new ArgumentNullException(nameof(cluster1));

            if (cluster2 == null)
                throw new ArgumentNullException(nameof(cluster2));

            Cluster1 = cluster1;
            Cluster2 = cluster2;
        }

        public class EqualityComparer : IEqualityComparer<ClusterNodePair>
        {
            //see IEqualyComparer_Example in ProgrammingTips folder for better understanding of this concept
            //the implementation of the IEqualityComparer is necessary because ClusterPair has two keys (cluster1.Id and cluster2.Id in ClusterPair) to compare

            public bool Equals(ClusterNodePair x, ClusterNodePair y)
            {
                return x.Cluster1.Id == y.Cluster1.Id && x.Cluster2.Id == y.Cluster2.Id;
            }

            public int GetHashCode(ClusterNodePair x)
            {
                return x.Cluster1.Id ^ x.Cluster2.Id;
            }
        }
    }
}