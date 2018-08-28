using System.Collections.Generic;
using System.Linq;

namespace Dentogram.Clustering
{
    public class ClusterNode
    {
        private readonly HashSet<ClusterLeaf> leafs;
        private readonly HashSet<ClusterNode> nodes;

        public double Distance { get; set; }
        public int Id { get; set; }
        public int TotalLeafsCount { get; set; }
        
        public int LeafsCount => leafs.Count;
        public int NodesCount => nodes.Count;

        public ClusterLeaf[] Leafs => leafs.ToArray();
        public ClusterNode[] Nodes => nodes.ToArray();

        public ClusterNode()
        {
            leafs = new HashSet<ClusterLeaf>();
            nodes = new HashSet<ClusterNode>();
        }

        public void Add(ClusterLeaf leaf)
        {
            leafs.Add(leaf);
        }

        public void Add(ClusterNode node)
        {
            nodes.Add(node);
        }

        public ClusterLeaf LeafAt(int index)
        {
            return leafs.ElementAt(index);
        }

        public ClusterNode NodeAt(int index)
        {
            return nodes.ElementAt(index);
        }

        public int UpdateTotalLeafs()
        {
            if (nodes.Any())
            {
                TotalLeafsCount = 0;
                foreach (ClusterNode node in nodes)
                {
                    TotalLeafsCount = TotalLeafsCount + node.UpdateTotalLeafs();
                }
            }

            return TotalLeafsCount;
        }

        /*
        public List<ClusterLeaf> GetAllPatterns()
        {
            return SubClustersCount == 0
                ? cluster.ToList()
                : subClusters.SelectMany(GetSubClusterPattern).ToList();
        }

        private IEnumerable<ClusterLeaf> GetSubClusterPattern(ClusterNode subCluster)
        {
            if (SubClustersCount == 0)
            {
                foreach (ClusterLeaf pattern in subCluster.cluster)
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
        */
    }
}
