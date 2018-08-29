namespace Dentogram.Clustering
{
    public class ClusterLeaf
    {
        public int Id { get; set; }
        public object Value { get; set; }

        public string FileName { get; set; }
        public string FileText { get; set; }
        public string Region { get; set; }
    }
}