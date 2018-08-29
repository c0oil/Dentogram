using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dentogram
{
    public class ParsingText
    {
        public struct ParseResult
        {
            public string NamePersonPrefix;
            public string NamePersonValue;
            public string AggregatedAmountPrefix;
            public string AggregatedAmountValue;
            public string Region;
        }

        private readonly StringSearch namePersonPrefixSearch = new StringSearch(PrefixWorkbook.NamePersonPrefixes);
        private readonly StringSearch namePersonPostfixSearch = new StringSearch(PrefixWorkbook.NamePersonPostfixes);

        private readonly Regex namePersonRegex1 = new Regex(@"(NAMES?(?: ?OF ?REPORTING| ?AND ?IRS) ?[\s\S]{0,100}PERSONS?(?: ?ENTITIES ONLY)?)([\s\S]{0,400}?)2 ?(?:CHECK|MEMBER)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex namePersonRegex2 = new Regex(@"(CUSIP(?: NUMBER)? [\w]+ ITEM 1 REPORTING PERSON) ([\s\S]{0,200}?) ITEM \d", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        //9AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON 291,065
        //10CHECK BOX IF THE AGGREGATE
        private readonly Regex aggregatedAmountRegex1 = new Regex(@"((?:9|11) ?AGGREGATED? ?AMOUN?T ?(?:[\s\S]{0,100}PERSONS?(?: ?DISCRETIONARY ?NONDISCRETIONARY ?ACCOUNTS)?|BENEFICIALLY ?OWNED)) ?((?:\d+(?:\,\d+)*)|\*|NONE|SEE ROW 6 ABOVE)[\s\S]*?(?:10|12)? ?(?:CHECK(?: ?BOX)? ?IF(?: ?THE)? ?AGGREGATE|AGGREGATE ?AMOUNT)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex aggregatedAmountRegex2 = new Regex(@"(ITEM 9) ((?:\d+(?:\,\d+)*)|\*|NONE) ITEM 11", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        //private readonly Regex percentOwnedRegex1 = new Regex(@"(?:11|13) ?PERCENT(?:AGE)?\s+OF\s+CLASS\s+REPRESENTED\s+BY\s+AMOUNT\s+(?:IN|OF)\s+ROW(?:\s+\(?\d\d?\)?)?(?:\s+\(SEE\s+ITEM\s+\d+\))?\s+\b((?:\d+(?:\.\d+)?)|\*)(?:\s*%)?\b[\s\S]*?\b(?:12|14)\b\b\s*[\.\)]?\s*TYPE", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex punctuationRegex = new Regex(@"\D[\,\.](?=\D)|\D[\,\.](?=\d)|\d[\,\.](?=\D)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string TrimForClustering(string text)
        {
            string trimText = Regex.Replace(text, @"[\.,\d]", "");
            //trimText = Regex.Replace(trimText, @" \w{1,3}(?= )", " ");
            trimText = Regex.Replace(trimText, @"\s+", " ");
            return trimText;
        }

        public string TrimForParsing(string text)
        {
            string trimText = TrimSigns(text);
            trimText = Regex.Replace(trimText, @"_", "");
            trimText = Regex.Replace(trimText, @"[^\w\s\.,]", "");
            trimText = Regex.Replace(trimText, @"\s+", " ");
            return trimText;
        }

        public ParseResult ParseTableNew(string trimText)
        {
            try
            {
                StringSearchResult namePersonPrefixMatch = namePersonPrefixSearch.FindAll(trimText).GroupBy(x => x.Index).FirstOrDefault()?.Last() ?? StringSearchResult.Empty;
                StringSearchResult namePersonValueMatch = new StringSearchResult();
                if (!namePersonPrefixMatch.IsEmpty)
                {
                    int iNamePersonValue = namePersonPrefixMatch.Index + namePersonPrefixMatch.Keyword.Length;
                    StringSearchResult namePersonPostfixMatch = namePersonPostfixSearch.FindFirst(trimText.Substring(iNamePersonValue, PrefixWorkbook.NamePersonMaxLength + PrefixWorkbook.NamePersonPostfixMaxLength));
                    if (!namePersonPostfixMatch.IsEmpty)
                    {
                        namePersonValueMatch = new StringSearchResult(
                            iNamePersonValue, 
                            trimText.Substring(iNamePersonValue, namePersonPostfixMatch.Index));
                    }
                }
                else
                {
                    var namePersonMatches = namePersonRegex2.Matches(trimText);
                    if (namePersonMatches.Count > 0)
                    {
                        namePersonPrefixMatch = new StringSearchResult(
                            namePersonMatches[0].Groups[1].Index,
                            namePersonMatches[0].Groups[1].Value);
                        namePersonValueMatch = new StringSearchResult(
                            namePersonMatches[0].Groups[2].Index,
                            namePersonMatches[0].Groups[2].Value);
                    }
                }

                if (namePersonPrefixMatch.IsEmpty || namePersonValueMatch.IsEmpty)
                {
                    return new ParseResult();
                }

                string namePersonRegion = trimText.Substring(namePersonPrefixMatch.Index, Math.Min(namePersonPrefixMatch.Keyword.Length + 1000, trimText.Length - namePersonPrefixMatch.Index));

                return new ParseResult
                {
                    NamePersonPrefix = namePersonPrefixMatch.Keyword,
                    NamePersonValue = namePersonValueMatch.Keyword,
                    Region = namePersonRegion.Substring(0, namePersonRegion.LastIndexOf(' '))
                };

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public ParseResult ParseTable(string trimText)
        {
            //string trimText = Regex.Replace(text.ToUpperInvariant(), @"[-:_\(\)""]", "");
            //trimText = Regex.Replace(trimText.ToUpperInvariant(), @" \D ", " ");
            MatchCollection namePersonMatches = namePersonRegex1.Matches(trimText);
            if (namePersonMatches.Count == 0)
            {
                namePersonMatches = namePersonRegex2.Matches(trimText);
            }
            if (namePersonMatches.Count == 0)
            {
                return new ParseResult();
            }

            Match namePersonMatch = namePersonMatches[0];
            Group namePersonPrefixGroup = namePersonMatches[0].Groups[1];
            Group namePersonValueGroup = namePersonMatches[0].Groups[2];

            string namePersonRegion = trimText.Substring(namePersonMatch.Index, Math.Min(namePersonMatch.Length + 1000, trimText.Length - namePersonMatch.Index));

            Match aggregatedAmountMatch = ParseAggregatedAmount(namePersonRegion);
            Group aggregatedAmountPrefixGroup = aggregatedAmountMatch?.Groups[1];
            Group aggregatedAmountValueGroup = aggregatedAmountMatch?.Groups[2];

            return new ParseResult
            {
                NamePersonPrefix = namePersonPrefixGroup.Value,
                NamePersonValue = namePersonValueGroup.Value, 
                AggregatedAmountPrefix = aggregatedAmountPrefixGroup?.Value, 
                AggregatedAmountValue = aggregatedAmountValueGroup?.Value, 
                Region = namePersonRegion.Substring(0, namePersonRegion.LastIndexOf(' '))
            };
        }

        public Match ParseAggregatedAmount(string trimText)
        {
            MatchCollection aggregatedAmountMatches = aggregatedAmountRegex1.Matches(trimText);
            if (aggregatedAmountMatches.Count == 0)
            {
                aggregatedAmountMatches = aggregatedAmountRegex2.Matches(trimText);
            }
            return aggregatedAmountMatches.Count == 0 ? null : aggregatedAmountMatches[0];
        }

        private string TrimSigns(string text)
        {
            MatchCollection matches = punctuationRegex.Matches(text);
            int prevMatchIndex = 0;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < matches.Count; i++)
            {
                int matchIndex = matches[i].Value.Length == 2 ? matches[i].Index + 1 : matches[i].Index;
                builder.Append(text.Substring(prevMatchIndex, matchIndex - prevMatchIndex));

                prevMatchIndex = matchIndex + 1;
            }
            builder.Append(text.Substring(prevMatchIndex, text.Length - prevMatchIndex));
            return builder.ToString();
        }

        /*
        public string[] ParseTableTest(string trimText)
        {
            //string trimText = Regex.Replace(text.ToUpperInvariant(), @"[-:_\(\)""]", "");
            //trimText = Regex.Replace(trimText.ToUpperInvariant(), @" \D ", " ");
            MatchCollection matches = regex3.Matches(trimText);
            if (matches.Count == 0)
            {
                return new [] { string.Empty , string.Empty, };
            }

            Match match = matches.OfType<Match>().FirstOrDefault();

            return new [] { match?.Value ?? string.Empty, match == null ? string.Empty : trimText.Substring(match.Index, Math.Min(match.Length + 100, trimText.Length))};
        }
        */
    }

    public static class PrefixWorkbook
    {
        public static int NamePersonMaxLength = 400;

        public static string[] NamePersonPostfixes = new[]
        {
            "2 CHECK",  
            "2 MEMBER",
            "2CHECK",
            "2MEMBER",
        };

        public static int NamePersonPostfixMaxLength = NamePersonPostfixes.Max(x => x.Length);

        public static string[] NamePersonPrefixes = new[]
        {
            "NAME AND IRS IDENTIFICATION NO OF REPORTING PERSON",
            "NAME AND IRS NUMBER OF REPORTING PERSONS",
            "NAME OF REPORTING PERSON",
            "NAME OF REPORTING PERSON 1 SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON 1SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON I R S IDENTIFICATION NO OF ABOVE PERSON ENTITIES ONLY",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSON ENTITIES ONLY",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSONS",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSONS ENTITIES ONLY",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NOS OF ABOVE PERSON ENTITIES ONLY",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NOS OF ABOVE PERSONS ENTITIES ONLY",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION OF ABOVE PERSON",
            "NAME OF REPORTING PERSON OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON SS OR IRS IDENTIFICATION NO OF ABOVE PERSON ENTITIES ONLY",
            "NAME OF REPORTING PERSON SS OR IRS IDENTIFICATION NOS OF ABOVE PERSON",
            "NAME OF REPORTING PERSON SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAME OF REPORTING PERSON SS OR IRS INDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSONIRS IDENTIFICATION NO OF ABOVE PERSON ENTITIES ONLY",
            "NAME OF REPORTING PERSONIRS IDENTIFICATION NO OF ABOVE PERSONS",
            "NAME OF REPORTING PERSONOR IRS IDENTIFICATION NO OF ABOVE PERSON ENTITIES ONLY",
            "NAME OF REPORTING PERSONS",
            "NAME OF REPORTING PERSONS I R S IDENTIFICATION NOS OF ABOVE PERSONS ENTITIES ONLY",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSONS",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSONS ENTITIES ONLY",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NOS OF ABOVE PERSONS ENTITIES ONLY",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NOS OF REPORTING PERSONS ENTITIES ONLY",
            "NAME OF REPORTING PERSONS SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSONS SS OR IRS IDENTIFICATION NO OF ABOVE PERSONS",
            "NAME OF REPORTING PERSONS SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAME OF REPORTING PERSONS SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS ENTITIES ONLY",
            "NAME OF REPORTING PERSONSS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSONSS OR IRS IDENTIFICATION NO OF ABOVE PERSON ENTITIES ONLY",
            "NAME OF REPORTING PERSONSS OR IRS INDENTIFICATION NO OF ABOVE PERSON ENTITIES ONLY",
            "NAMES OF REPORTING PERSON",
            "NAMES OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAMES OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSONS ENTITIES ONLY",
            "NAMES OF REPORTING PERSON SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAMES OF REPORTING PERSONS",
            "NAMES OF REPORTING PERSONS AND IRS IDENTIFICATION NOS OF SUCH PERSONS ENTITIES ONLY",
            "NAMES OF REPORTING PERSONS ENTITIES ONLY",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSON ENTITIES ONLY",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSONS ENTITIES ONLY",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NOS OF ABOVE PERSONS ENTITIES ONLY",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NOS OR ABOVE PERSONS ENTITIES ONLY",
            "NAMES OF REPORTING PERSONS SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAMES OF REPORTING PERSONS SS OR IRS IDENTIFICATION NO OF ABOVE PERSONS",
            "NAMES OF REPORTING PERSONS SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAMES OF REPORTING PERSONS SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS ENTITIES ONLY",
            "NAMES OF REPORTING PERSONSIRS IDENTIFICATION NO OF ABOVE PERSONS ENTITIES ONLY",
            "1 ENTITIES ONLY",

            // Regexps
            /*
            "NAME OF REPORTING PERSON {0} IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON {0} IRS IDENTIFICATION NO OF ABOVE PERSON ENTITIES ONLY",
            "NAME OF REPORTING PERSON {0} IRS IDENTIFICATION NOS OF ABOVE PERSONS ENTITIES ONLY",
            "NAME OF REPORTING PERSON {0} SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSONS {0} SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAMES OF REPORTING {0} IRS IDENTIFICATION NO OF {0} ABOVE PERSONS ENTITIES ONLY",
            "NAMES OF REPORTING PERSONS {0} IRS IDENTIFICATION NOS OF ABOVE PERSONS ENTITIES ONLY",
            */
        };
    }
}
