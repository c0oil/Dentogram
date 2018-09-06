using System;
using System.Collections.Generic;
using System.Linq;
using FuzzyString;

namespace Dentogram.Clustering
{
    public static class ClusterDistance
    {
        public enum Strategy
        {
            //SingleLinkage,
            MinLinkage,
            //CompleteLinkage,
            MaxLinkage,
            //AverageLinkageWPGMA,
            AverageLinkage,
            //AverageLinkageUPGMA,
            AverageLinkageWeighted,
        }

        public static double ComputeLeafDistance(ClusterNode node1, ClusterNode node2)
        {
            return node1.LeafsCount == 1 && node2.LeafsCount == 1
                ? GetDistance(node1.LeafAt(0).Value, node2.LeafAt(0).Value)
                : 0;
        }

        // this method compute distance between clusters thas has subclusters (cluster2 represents the new cluster)
        public static double ComputeNodeDistance(ClusterNode node1, ClusterNode node2, DissimilarityMatrix dissimilarityMatrix, Strategy strategy)
        {
            ClusterNode node21 = node2.NodeAt(0);
            ClusterNode node22 = node2.NodeAt(1);
            double distance1 = dissimilarityMatrix.ReturnClusterPairDistance(new ClusterNodePair(node1, node21));
            double distance2 = dissimilarityMatrix.ReturnClusterPairDistance(new ClusterNodePair(node1, node22));

            switch (strategy)
            {
                case Strategy.MinLinkage:
                    return distance1 < distance2 ? distance1 : distance2;
                case Strategy.MaxLinkage:
                    return distance1 > distance2 ? distance1 : distance2;
                case Strategy.AverageLinkage:
                    return (distance1 + distance2) / 2;
                case Strategy.AverageLinkageWeighted:
                    return distance1 * node21.TotalLeafsCount / node2.TotalLeafsCount + 
                           distance2 * node22.TotalLeafsCount / node2.TotalLeafsCount;
                default:
                    return 0;
            }
        }
        
        // TODO: Change
        private static double GetDistance(object x, object y)
        {
            if (x is double)
            {
                return GetDistance((double)x, (double)y);
            }

            if (x is string)
            {
                return GetDistance((string)x, (string)y);
            }

            throw new Exception();
        }

        private static double GetDistance(double x, double y)
        {
            return Math.Abs(y - x);
        }

        public static string Mode = "JaccardDistance";
        public static int Shindel = 5;

        public static List<string> AllModes = new List<string>
            {
                "SorensenDiceDistance",
                "JaroWinklerDistance",
                "JaroDistance",
                "JaccardDistance",
                "HammingDistance",
                //"LevenshteinDistance",
                //"NormalizedLevenshteinDistance",
                "LevenshteinDistanceUpperBounds",
                "LevenshteinDistanceLowerBounds",

                "TanimotoCoefficient",
                "OverlapCoefficient",

                "JaccardIndex",
                "SorensenDiceIndex",
                    
                "RatcliffObershelpSimilarity",
                    
                "LongestCommonSubstring",
                "LongestCommonSubsequence",
                
                "TEST_Distance",
            };

        private static double GetDistance(string x, string y)
        {
            double distance;
            switch (Mode)
            {
                case "SorensenDiceDistance":
                    return x.SorensenDiceDistance(y);
                case "JaroWinklerDistance":
                    return x.JaroWinklerDistance(y);
                case "JaroDistance":
                    return x.JaroDistance(y);
                case "JaccardDistance":
                    return JaccardDistance(x, y, Shindel);
                    //return x.JaccardDistance(y);
                case "HammingDistance":
                    return x.HammingDistance(y);
                case "LevenshteinDistance":
                    return x.LevenshteinDistance(y);
                case "NormalizedLevenshteinDistance":
                    return x.NormalizedLevenshteinDistance(y);
                case "LevenshteinDistanceUpperBounds":
                    return x.LevenshteinDistanceUpperBounds(y);
                case "LevenshteinDistanceLowerBounds":
                    return x.LevenshteinDistanceLowerBounds(y);

                case "TanimotoCoefficient":
                    distance = x.TanimotoCoefficient(y);
                    if (double.IsNaN(distance))
                    {
                        return 1;
                    }
                    return distance;
                case "OverlapCoefficient":
                    distance = x.OverlapCoefficient(y);
                    if (double.IsNaN(distance))
                    {
                        return 1;
                    }
                    return distance;

                case "JaccardIndex":
                    return JaccardIndex(x, y, Shindel);
                    //return x.JaccardIndex(y);
                case "SorensenDiceIndex":
                    return x.SorensenDiceIndex(y);
                    
                case "RatcliffObershelpSimilarity":
                    return x.RatcliffObershelpSimilarity(y);
                    
                case "LongestCommonSubstring":
                    return Math.Max(x.Length, y.Length) - x.LongestCommonSubstring(y)?.Length ?? 0;
                case "LongestCommonSubsequence":
                    return Math.Max(x.Length, y.Length) - x.LongestCommonSubsequence(y)?.Length ?? 0;

                case "TEST_Distance":
                    return TestDistance(x, y, Shindel);
            }

            return 0;
        }
        
        
        private static List<string> ListNGrams(string words, int n)
        {
            if (string.IsNullOrEmpty(words))
            {
                return null;
            }

            List<int> spaces = new List<int>();

            if (words[0] != ' ')
            {
                spaces.Add(-1);
            }
            int currIndex = 0;
            foreach (char c in words)
            {
                if (c == ' ')
                {
                    spaces.Add(currIndex);
                }
                currIndex++;
            }
            if (words[words.Length - 1] != ' ')
            {
                spaces.Add(words.Length);
            }

            int wordsCount = spaces.Count - 1;

            List<string> stringList = new List<string>();
            if (n > wordsCount)
            {
                stringList.Add(words);
                return stringList;
            }
            if (n == wordsCount)
            {
                stringList.Add(words);
                return stringList;
            }

            int prevSpaceIndex = 0;
            for (int spaceIndex = 1; spaceIndex < wordsCount - n + 2; spaceIndex++)
            {
                int iStart = spaces[prevSpaceIndex] + 1;
                int iEnd = spaces[spaceIndex + n - 1];
                if (iEnd - iStart > 1)
                {
                    stringList.Add(words.Substring(iStart, iEnd - iStart));
                }
                prevSpaceIndex = spaceIndex;
            }
            return stringList;
        }

        private static double JaccardDistance(string source, string target, int n)
        {
            return 1.0 - JaccardIndex(source, target, n);
        }

        private static double JaccardIndex(string source, string target, int n)
        {
            List<string> g1 = ListNGrams(source, n);
            List<string> g2 = ListNGrams(target, n);
            int minCount = Math.Min(g1.Count, g2.Count);
            if (minCount == 0)
            {
                return 0;
            }

            return Convert.ToDouble(g1.Intersect(g2).Count()) / Convert.ToDouble(g1.Union(g2).Count());
        }

        private static double TestDistance(string source, string target, int n)
        {
            List<string> g1 = ListNGrams(source, n);
            List<string> g2 = ListNGrams(target, n);
            int minCount = Math.Min(g1.Count, g2.Count);
            if (minCount == 0)
            {
                return Math.Max(g1.Count, g2.Count);
            }

            //return Convert.ToDouble(Convert.ToDouble(g1.Union(g2).Count()) - g1.Intersect(g2).Count());
            return Convert.ToDouble(Convert.ToDouble(g1.Union(g2).Count()) - g1.Intersect(g2).Count());
        }
    }
}