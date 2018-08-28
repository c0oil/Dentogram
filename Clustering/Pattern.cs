using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AlternativeSoft.Sec.SecDailyUpdater.Clustering
{
    public class PatternMatrix : IEnumerable
    {
        private readonly HashSet<Pattern> patternCollection;

        public PatternMatrix()
        {
            patternCollection = new HashSet<Pattern>();
        }

        public void AddPattern(Pattern pattern)
        {
            patternCollection.Add(pattern);
        }
            
        public IEnumerator GetEnumerator()
        {
            return patternCollection.GetEnumerator();
        }
    }

    public class Pattern
    {
        public string FileName { get; set; }
        public string FileText { get; set; }
        public int Id { get; set; }
        //public string Attribute { get; set; }
        public object Attribute { get; set; }
    }
}