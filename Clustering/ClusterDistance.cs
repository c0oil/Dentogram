using System;
using System.Collections.Generic;
using System.Linq;
using FuzzyString;
using Microsoft.Practices.ObjectBuilder2;

namespace AlternativeSoft.Sec.SecDailyUpdater.Clustering
{
    public static class ClusterDistance
    {
        public enum Strategy
        {
            SingleLinkage,
            CompleteLinkage,
            AverageLinkageWPGMA,
            AverageLinkageUPGMA,
        }

        // this method compute distance between 2 singleton clusters
        public static double ComputeDistance(Cluster cluster1, Cluster cluster2)
        {
            // if singleton cluster, then compute distance between patterns
            return cluster1.PatternsCount == 1 && cluster2.PatternsCount == 1
                ? GetDistance(cluster1.PatternAt(0).Attribute, cluster2.PatternAt(0).Attribute)
                : 0;
        }

        // this method compute distance between clusters thas has subclusters (cluster2 represents the new cluster)
        public static double ComputeDistance(Cluster cluster1, Cluster cluster2, DissimilarityMatrix dissimilarityMatrix, Strategy strategy)
        {
            Cluster cluster21 = cluster2.SubClusterAt(0);
            Cluster cluster22 = cluster2.SubClusterAt(1);
            double distance1 = dissimilarityMatrix.ReturnClusterPairDistance(new ClusterPair(cluster1, cluster21));
            double distance2 = dissimilarityMatrix.ReturnClusterPairDistance(new ClusterPair(cluster1, cluster22));

            switch (strategy)
            {
                case Strategy.SingleLinkage:
                    return distance1 < distance2 ? distance1 : distance2;
                case Strategy.CompleteLinkage:
                    return distance1 > distance2 ? distance1 : distance2;
                case Strategy.AverageLinkageWPGMA:
                    return (distance1 + distance2) / 2;
                case Strategy.AverageLinkageUPGMA:
                    return distance1 * cluster21.TotalQuantityOfPatterns / cluster2.TotalQuantityOfPatterns + 
                           distance2 * cluster22.TotalQuantityOfPatterns / cluster2.TotalQuantityOfPatterns;
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
            };

        private static double GetDistance(string x, string y)
        {
            try
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
                    return JaccardDistance(x, y, 5);
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
                    return JaccardIndex(x, y, 5);
                    //return x.JaccardIndex(y);
                case "SorensenDiceIndex":
                    return x.SorensenDiceIndex(y);
                    
                case "RatcliffObershelpSimilarity":
                    return x.RatcliffObershelpSimilarity(y);
                    
                case "LongestCommonSubstring":
                    return Math.Max(x.Length, y.Length) - x.LongestCommonSubstring(y)?.Length ?? 0;
                case "LongestCommonSubsequence":
                    return Math.Max(x.Length, y.Length) - x.LongestCommonSubsequence(y)?.Length ?? 0;
            }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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
                return (List<string>) null;
            if (n == wordsCount)
            {
                stringList.Add(words);
                return stringList;
            }

            int prevSpaceIndex = spaces[0];
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

        /*
        private static List<string> ListNGrams(List<string> words, int n)
        {
            List<string> stringList = new List<string>();
            if (n > words.Count)
                return (List<string>) null;
            if (n == words.Count)
            {
                stringList.Add(words.JoinStrings(" "));
                return stringList;
            }

            for (int startIndex = 0; startIndex < words.Count - n; startIndex++)
            {
                stringList.Add(words.Skip(startIndex).Take(n).JoinStrings(" "));
            }
            return stringList;
        }
        */

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

            return Convert.ToDouble(g1.Intersect(g2).Count()) / Convert.ToDouble(minCount);
        }
    }
}