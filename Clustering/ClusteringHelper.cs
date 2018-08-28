using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dentogram;

namespace AlternativeSoft.Sec.SecDailyUpdater.Clustering
{
    public class ClusteringModel
    {
        private readonly PatternMatrix patternMatrix;
        private readonly Clusters clusters; // data structure for clustering
        private DissimilarityMatrix dissimilarityMatrix;
        
        public ClusteringModel(List<string> dataSet, List<string> fileTextes, List<string> files)
        {
            int patternIndex = 0;
            clusters = new Clusters();
            patternMatrix = new PatternMatrix();

            var hash = new HashSet<string>();
            foreach (string item in dataSet)
            {
                if (hash.Contains(item))
                {
                    patternIndex++;
                    continue;
                }
                hash.Add(item);

                var pattern = new Pattern();
                pattern.Id = patternIndex;
                pattern.FileName = files[patternIndex];
                pattern.FileText = fileTextes[patternIndex];
                pattern.Attribute = dataSet[patternIndex];
                patternMatrix.AddPattern(pattern);
                patternIndex++;
            }
        }

        public Clusters ExecuteClustering(ClusterDistance.Strategy strategy, int k)
        {
            clusters.BuildSingletonCluster(patternMatrix);
            
            dissimilarityMatrix = new DissimilarityMatrix();
            ClusterPair[] clusterPairCollection = GetClusterPairCollection().ToArray();
            foreach (ClusterPair clusterPair in clusterPairCollection)
            {
                double distanceBetweenTwoClusters = ClusterDistance.ComputeDistance(clusterPair.Cluster1, clusterPair.Cluster2);
                dissimilarityMatrix.AddClusterPairAndDistance(clusterPair, distanceBetweenTwoClusters);
            }
            
            BuildHierarchicalClustering(clusters.Count, strategy, k);

            return clusters;
        }

        public Cluster[] BuildFlatClustersFromHierarchicalClustering(Clusters clusters, int k)
        {
            Cluster[] flatClusters = new Cluster[k];
            for (int i = 0; i < k; i++)
            {
                flatClusters[i] = new Cluster();
                flatClusters[i].Id = i;
                foreach (Pattern pattern in clusters.ElementAt(i).GetAllPatterns())
                {
                    flatClusters[i].Add(pattern);
                }
            }

            return flatClusters;
        }

        public void CreateCSVMatrixFile(string path)
        {
            File.Delete(path);
            clusters.BuildSingletonCluster(patternMatrix);

            StringBuilder matrix = new StringBuilder();
            string headerLine = "AggloCluster";
            foreach (Cluster cluster in clusters)
            {
                headerLine = headerLine + ", Cluster" + cluster.Id;
            }
            matrix.Append(headerLine);
            
            bool writeBlank = false;
            for (int i = 0; i < clusters.Count; i++)
            {
                matrix.Append("\r\n");
                matrix.Append("Cluster" + clusters.ElementAt(i).Id);
                writeBlank = false;

                for (int j = 0; j < clusters.Count; j++)
                {
                    ClusterPair clusterPair = new ClusterPair(clusters.ElementAt(i), clusters.ElementAt(j));
                    double distanceBetweenTwoClusters = ClusterDistance.ComputeDistance(clusterPair.Cluster1, clusterPair.Cluster2);

                    if (distanceBetweenTwoClusters == 0)
                    {
                        writeBlank = true;
                        matrix.Append(",0");
                    }
                    else
                    {
                        matrix.Append("," + (writeBlank ? string.Empty : distanceBetweenTwoClusters.ToString()));
                    }
                }
            }

            File.AppendAllText(path, matrix.ToString());
        }

        private void BuildHierarchicalClustering(int indexNewCluster, ClusterDistance.Strategy strategy, int k)
        {
            ClusterPair closestClusterPair = dissimilarityMatrix.GetClosestClusterPair();

            Cluster newCluster = new Cluster();
            newCluster.Add(closestClusterPair.Cluster1);
            newCluster.Add(closestClusterPair.Cluster2);
            newCluster.Id = indexNewCluster;
            newCluster.Distance = dissimilarityMatrix.ReturnClusterPairDistance(closestClusterPair);
            newCluster.UpdateTotalQuantityOfPatterns(); //update the total quantity of patterns of the new cluster (this quantity is used by UPGMA clustering strategy)
     
            clusters.Remove(closestClusterPair.Cluster1);
            clusters.Remove(closestClusterPair.Cluster2);
            UpdateDissimilarityMatrix(newCluster, strategy);

            clusters.Add(newCluster);

            // recursive call of this method while there is more than 1 cluster (k>2) in the clustering
            if (clusters.Count > k)
            {
                BuildHierarchicalClustering(indexNewCluster + 1, strategy, k);
            }
        }
        
        private void UpdateDissimilarityMatrix(Cluster newCluster, ClusterDistance.Strategy strategie)
        {
            Cluster cluster1 = newCluster.SubClusterAt(0);
            Cluster cluster2 = newCluster.SubClusterAt(1);
            for (int i = 0; i < clusters.Count; i++)
            {
                Cluster cluster = clusters.ElementAt(i);

                double distanceBetweenClusters = ClusterDistance.ComputeDistance(cluster, newCluster, dissimilarityMatrix, strategie);

                dissimilarityMatrix.AddClusterPairAndDistance(new ClusterPair(newCluster, cluster), distanceBetweenClusters);
                dissimilarityMatrix.RemoveClusterPair(new ClusterPair(cluster1, cluster));
                dissimilarityMatrix.RemoveClusterPair(new ClusterPair(cluster2, cluster));
            }
            
            dissimilarityMatrix.RemoveClusterPair(new ClusterPair(cluster1, cluster2));
        }

        private IEnumerable<ClusterPair> GetClusterPairCollection()
        {
            for (int i = 0; i < clusters.Count; i++)
            {
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    yield return new ClusterPair(clusters.ElementAt(i), clusters.ElementAt(j));
                }
            }
        }
    }
}
