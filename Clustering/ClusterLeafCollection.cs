using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dentogram.Clustering
{
    public class ClusterLeafCollection : IEnumerable
    {
        private readonly HashSet<ClusterLeaf> leafCollection;

        public ClusterLeafCollection()
        {
            leafCollection = new HashSet<ClusterLeaf>();
        }

        public void Add(ClusterLeaf leaf)
        {
            leafCollection.Add(leaf);
        }
            
        public IEnumerator GetEnumerator()
        {
            return leafCollection.GetEnumerator();
        }
    }
}