using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dentogram.Clustering
{
    public class ClusteringTreeModel
    {
        private readonly ClusterLeafCollection leafCollection;
        private readonly ClusterNodeCollection nodeCollection;
        private DissimilarityMatrix dissimilarityMatrix;
        
        public ClusteringTreeModel(List<string> dataSet, List<string> parsedRegions, List<string> fileTextes, List<string> files)
        {
            int leafIndex = 0;
            nodeCollection = new ClusterNodeCollection();
            leafCollection = new ClusterLeafCollection();

            var hash = new HashSet<string>();
            foreach (string item in dataSet)
            {
                if (hash.Contains(item))
                {
                    leafIndex++;
                    continue;
                }
                hash.Add(item);

                var leaf = new ClusterLeaf
                {
                    Id = leafIndex,
                    FileName = files[leafIndex],
                    FileText = fileTextes[leafIndex],
                    Region = parsedRegions[leafIndex],
                    Value = dataSet[leafIndex]
                };
                leafCollection.Add(leaf);
                leafIndex++;
            }
        }

        public ClusterNodeCollection ExecuteClustering(ClusterDistance.Strategy strategy, int k)
        {
            nodeCollection.BuildSingletonCluster(leafCollection);
            
            dissimilarityMatrix = new DissimilarityMatrix();
            foreach (ClusterNodePair clusterPair in GetClusterPairCollection())
            {
                double distanceBetweenTwoClusters = ClusterDistance.ComputeLeafDistance(clusterPair.Cluster1, clusterPair.Cluster2);
                dissimilarityMatrix.AddClusterPairAndDistance(clusterPair, distanceBetweenTwoClusters);
            }
            
            BuildHierarchicalClustering(nodeCollection.Count, strategy, k);

            return nodeCollection;
        }


        private void BuildHierarchicalClustering(int indexNewNode, ClusterDistance.Strategy strategy, int k)
        {
            ClusterNodePair closestClusterPair = dissimilarityMatrix.GetClosestClusterPair();

            ClusterNode newNode = new ClusterNode();
            newNode.Add(closestClusterPair.Cluster1);
            newNode.Add(closestClusterPair.Cluster2);
            newNode.Id = indexNewNode;
            newNode.Distance = dissimilarityMatrix.ReturnClusterPairDistance(closestClusterPair);
            newNode.UpdateTotalLeafs();
     
            nodeCollection.Remove(closestClusterPair.Cluster1);
            nodeCollection.Remove(closestClusterPair.Cluster2);
            UpdateDissimilarityMatrix(newNode, strategy);

            nodeCollection.Add(newNode);

            if (nodeCollection.Count > k)
            {
                BuildHierarchicalClustering(indexNewNode + 1, strategy, k);
            }
        }
        
        private void UpdateDissimilarityMatrix(ClusterNode newNode, ClusterDistance.Strategy strategie)
        {
            ClusterNode node1 = newNode.NodeAt(0);
            ClusterNode node2 = newNode.NodeAt(1);
            for (int i = 0; i < nodeCollection.Count; i++)
            {
                ClusterNode node = nodeCollection.ElementAt(i);

                double distanceBetweenClusters = ClusterDistance.ComputeNodeDistance(node, newNode, dissimilarityMatrix, strategie);

                dissimilarityMatrix.AddClusterPairAndDistance(new ClusterNodePair(newNode, node), distanceBetweenClusters);
                dissimilarityMatrix.RemoveClusterPair(new ClusterNodePair(node1, node));
                dissimilarityMatrix.RemoveClusterPair(new ClusterNodePair(node2, node));
            }
            
            dissimilarityMatrix.RemoveClusterPair(new ClusterNodePair(node1, node2));
        }

        private IEnumerable<ClusterNodePair> GetClusterPairCollection()
        {
            for (int i = 0; i < nodeCollection.Count; i++)
            {
                for (int j = i + 1; j < nodeCollection.Count; j++)
                {
                    yield return new ClusterNodePair(nodeCollection.ElementAt(i), nodeCollection.ElementAt(j));
                }
            }
        }

        /*
        public ClusterNode[] BuildFlatClustersFromHierarchicalClustering(ClusterNodeCollection clusters, int k)
        {
            ClusterNode[] flatClusters = new ClusterNode[k];
            for (int i = 0; i < k; i++)
            {
                flatClusters[i] = new ClusterNode();
                flatClusters[i].Id = i;
                foreach (ClusterLeaf pattern in clusters.ElementAt(i).GetAllPatterns())
                {
                    flatClusters[i].Add(pattern);
                }
            }

            return flatClusters;
        }

        public void CreateCSVMatrixFile(string path)
        {
            File.Delete(path);
            nodeCollection.BuildSingletonCluster(leafCollection);

            StringBuilder matrix = new StringBuilder();
            string headerLine = "AggloCluster";
            foreach (ClusterNode cluster in nodeCollection)
            {
                headerLine = headerLine + ", Cluster" + cluster.Id;
            }
            matrix.Append(headerLine);
            
            bool writeBlank = false;
            for (int i = 0; i < nodeCollection.Count; i++)
            {
                matrix.Append("\r\n");
                matrix.Append("Cluster" + nodeCollection.ElementAt(i).Id);
                writeBlank = false;

                for (int j = 0; j < nodeCollection.Count; j++)
                {
                    ClusterNodePair clusterPair = new ClusterNodePair(nodeCollection.ElementAt(i), nodeCollection.ElementAt(j));
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
        */
    }
}
