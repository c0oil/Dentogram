using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AlternativeSoft.Sec.SecDailyUpdater.Clustering
{
    public class Clusters : IEnumerable<Cluster>
    {
        private readonly HashSet<Cluster> clusters;

        public int Count => clusters.Count;

        public Clusters()
        {
            clusters = new HashSet<Cluster>();
        }
 
        public void Add(Cluster cluster)
        {
            clusters.Add(cluster);
        }

        public void Remove(Cluster cluster)
        {
            clusters.Remove(cluster);
        }

        public Cluster ElementAt(int index)
        {
            return clusters.ElementAt(index);
        }

        //add a single pattern to a cluster 
        public void BuildSingletonCluster(PatternMatrix patternMatrix)
        {
            int clusterId = 0;

            foreach (Pattern item in patternMatrix)
            {
                Cluster cluster = new Cluster();
                cluster.Id = clusterId;
                cluster.Add(item);
                cluster.TotalQuantityOfPatterns = 1;
                clusters.Add(cluster);
                clusterId++;
            }
        }

        IEnumerator<Cluster> IEnumerable<Cluster>.GetEnumerator()
        {
            return clusters.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return clusters.GetEnumerator();
        }
    }

    public class Cluster
    {
        private readonly HashSet<Pattern> cluster; // singleton cluster formed by one pattern
        private readonly HashSet<Cluster> subClusters; // child clusters

        public double Distance { get; set; }
        public int Id { get; set; }
        public int TotalQuantityOfPatterns { get; set; }
        
        public int PatternsCount => cluster.Count;
        public int SubClustersCount => subClusters.Count;

        public Pattern[] Patterns => cluster.ToArray();
        public Cluster[] SubClusters => subClusters.ToArray();

        public Cluster()
        {
            cluster = new HashSet<Pattern>();
            subClusters = new HashSet<Cluster>();
        }

        public void Add(Pattern pattern)
        {
            cluster.Add(pattern);
        }

        public void Add(Cluster subCluster)
        {
            subClusters.Add(subCluster);
        }

        public Pattern PatternAt(int index)
        {
            return cluster.ElementAt(index);
        }

        public Cluster SubClusterAt(int index)
        {
            return subClusters.ElementAt(index);
        }

        public int UpdateTotalQuantityOfPatterns()
        {
            //if cluster has subclustes, then calculate how many patterns there is in each subcluster
            if (subClusters.Any())
            {
                TotalQuantityOfPatterns = 0;
                foreach (Cluster subcluster in subClusters)
                {
                    TotalQuantityOfPatterns = TotalQuantityOfPatterns + subcluster.UpdateTotalQuantityOfPatterns();
                }
            }

            // if there is no subcluster, it is because is a singleton cluster (i.e., totalNumberOfPatterns = 1)
            return TotalQuantityOfPatterns;
        }

        public List<Pattern> GetAllPatterns()
        {
            return SubClustersCount == 0
                ? cluster.ToList()
                : subClusters.SelectMany(GetSubClusterPattern).ToList();
        }

        private IEnumerable<Pattern> GetSubClusterPattern(Cluster subCluster)
        {
            if (SubClustersCount == 0)
            {
                foreach (Pattern pattern in subCluster.cluster)
                {
                    yield return pattern;
                }
            }
            else
            {
                foreach (var iSubClusterPattern in subCluster.subClusters.SelectMany(GetSubClusterPattern))
                {
                    yield return iSubClusterPattern;
                }
            }
        }
    }

    //This class stores the pairs of cluster's id which is the dissimilarity matrix entry
    public class ClusterPair
    {
        public ClusterPair(Cluster cluster1, Cluster cluster2)
        {
            if (cluster1 == null)
                throw new ArgumentNullException(nameof(cluster1));

            if (cluster2 == null)
                throw new ArgumentNullException(nameof(cluster2));

            Cluster1 = cluster1;
            Cluster2 = cluster2;
        }

        public Cluster Cluster1 { get; }
        public Cluster Cluster2 { get; }

        public class EqualityComparer : IEqualityComparer<ClusterPair>
        {
            //see IEqualyComparer_Example in ProgrammingTips folder for better understanding of this concept
            //the implementation of the IEqualityComparer is necessary because ClusterPair has two keys (cluster1.Id and cluster2.Id in ClusterPair) to compare

            public bool Equals(ClusterPair x, ClusterPair y)
            {
                return x.Cluster1.Id == y.Cluster1.Id && x.Cluster2.Id == y.Cluster2.Id;
            }

            public int GetHashCode(ClusterPair x)
            {
                return x.Cluster1.Id ^ x.Cluster2.Id;
            }
        }
    }
}
