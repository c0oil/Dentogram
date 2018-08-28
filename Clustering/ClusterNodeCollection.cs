using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dentogram.Clustering
{
    public class ClusterNodeCollection : IEnumerable<ClusterNode>
    {
        private readonly HashSet<ClusterNode> nodes;

        public int Count => nodes.Count;

        public ClusterNodeCollection()
        {
            nodes = new HashSet<ClusterNode>();
        }

        public void BuildSingletonCluster(ClusterLeafCollection clusterLeafCollection)
        {
            int clusterId = 0;

            foreach (ClusterLeaf leaf in clusterLeafCollection)
            {
                ClusterNode cluster = new ClusterNode
                {
                    Id = clusterId,
                    TotalLeafsCount = 1,
                };
                cluster.Add(leaf);
                nodes.Add(cluster);
                clusterId++;
            }
        }
 
        public void Add(ClusterNode node)
        {
            nodes.Add(node);
        }

        public void Remove(ClusterNode node)
        {
            nodes.Remove(node);
        }

        public ClusterNode ElementAt(int index)
        {
            return nodes.ElementAt(index);
        }

        IEnumerator<ClusterNode> IEnumerable<ClusterNode>.GetEnumerator()
        {
            return nodes.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return nodes.GetEnumerator();
        }
    }
}