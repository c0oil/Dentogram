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
            public string NamePersonPostfix;

            public string AggregatedAmountPrefix;
            public string AggregatedAmountValue;
            public string AggregatedAmountPostfix;

            public string PercentOwnedPrefix;
            public string PercentOwnedValue;
            public string PercentOwnedPostfix;
            public string PercentOwned;

            public string Region;
        }

        private readonly StringSearch namePersonPrefixSearch = new StringSearch(PrefixWorkbook.NamePersonPrefixes);
        private readonly StringSearch namePersonPostfixSearch = new StringSearch(PrefixWorkbook.NamePersonPostfixes);

        private readonly StringSearch aggregatedAmountPrefixSearch = new StringSearch(PrefixWorkbook.AggregatedAmountPrefixes);
        private readonly StringSearch aggregatedAmountPostfixSearch = new StringSearch(PrefixWorkbook.AggregatedAmountPostfixes);

        private readonly StringSearch percentOwnedPrefixSearch = new StringSearch(PrefixWorkbook.PercentOwnedPrefixes);
        private readonly StringSearch percentOwnedPostfixSearch = new StringSearch(PrefixWorkbook.PercentOwnedPostfixes);

        private readonly Regex namePersonRegex1 = new Regex(@"(NAMES?(?: ?OF ?REPORTING| ?AND ?IRS) ?[\s\S]{0,100}PERSONS?(?: ?\(?ENTITIES ONLY\)?)?)([\s\S]{0,400}?)2 ?(?:CHECK|MEMBER)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex namePersonRegex2 = new Regex(@"(CUSIP(?: NUMBER)? [\w]+ ITEM 1 REPORTING PERSON) ([\s\S]{0,200}?) ITEM \d", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        private readonly Regex aggregatedAmountRegex1 = new Regex(@"((?:9|11) ?\)? ?AGGREGATED? ?AMOUN?T ?(?:[\s\S]{0,100}PERSONS?(?: ?DISCRETIONARY ?NONDISCRETIONARY ?ACCOUNTS)?|BENEFICIALLY ?OWNED)) ?((?:\d+(?:\,\d+)*)|\*|NONE|SEE ROW 6 ABOVE)[\s\S]*?((?:10|12)? ?(?:CHECK(?: ?BOX)? ?IF(?: ?THE)? ?AGGREGATE|AGGREGATE ?AMOUNT))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex aggregatedAmountRegex2 = new Regex(@"(ITEM 9) ((?:\d+(?:\,\d+)*)|\*|NONE) (ITEM 11)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        //private readonly Regex percentOwnedRegex1 = new Regex(@"((?:11|13) ?\) ?PERCENT(?:AGE)? ?OF ?CLASS\s+REPRESENTED ?BY ?AMOUNT ?(?:IN|OF) ?ROW(?: ?\(? ?\d\d? ?\(?)?(?: ?SEE ?ITEM ?\d+)?) ?((?:\d+(?:\.\d+)?)|\*) ?%?([\s\S]*? ?\(? (?:12|14) ?\)? ?TYPE)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex percentOwnedRegex1 = new Regex(@"\b((?:11|13)\b\s*[\.\)]?\s*PERCENT(?:AGE)?\s+OF\s+CLASS\s+REPRESENTED\s+BY\s+AMOUNT\s+(?:IN|OF)\s+ROW(?:\s+\(?\d\d?\)?)?(?:\s+\(SEE\s+ITEM\s+\d+\))?)\s+\b((?:\d+(?:\.\d+)?)|\*)(?:\s*%)?(\b[\s\S]*?\b(?:12|14)\b\b\s*[\.\)]?\s*TYPE)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex percentOwnedRegex2 = new Regex(@"(ITEM 11) ((?:\d+(?:\.\d+)?)|\*) ?%? (ITEM 12)", RegexOptions.Compiled | RegexOptions.IgnoreCase);


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
            trimText = Regex.Replace(trimText, @"[^\w\s\.,\(\)]", " ");
            //trimText = Regex.Replace(trimText, @"[^\w\s\.,]", "");
            trimText = Regex.Replace(trimText, @"\s+", " ");
            return trimText;
        }

        public IEnumerable<ParseResult> ParseBySearch(string trimText)
        {
            List<StringSearchResult> namePersonPrefixResult;
            List<StringSearchResult> namePersonValueResult;
            List<StringSearchResult> namePersonPostfixResult;
            ParseNamePersons(trimText, out namePersonPrefixResult, out namePersonValueResult, out namePersonPostfixResult);

            if (namePersonValueResult.Any())
            {
                for (int i = 0; i < namePersonPrefixResult.Count; i++)
                {
                    StringSearchResult namePersonPrefixMatch = namePersonPrefixResult[i];
                    StringSearchResult namePersonValueMatch = namePersonValueResult[i];
                    StringSearchResult namePersonPostfixMatch = namePersonPostfixResult[i];

                    if (namePersonValueMatch.IsEmpty)
                    {
                        continue;
                    }

                    int namePersonRegionLength = i + 1 < namePersonPrefixResult.Count 
                        ? namePersonPrefixResult[i + 1].Index - namePersonPrefixMatch.Index
                        : Math.Min(namePersonPrefixMatch.Keyword.Length + namePersonValueMatch.Keyword.Length + 1400, trimText.Length - namePersonPrefixMatch.Index);
                    string namePersonRegion = trimText.Substring(namePersonPrefixMatch.Index, namePersonRegionLength);
                
                    StringSearchResult aggregatedAmountPrefixResult;
                    StringSearchResult aggregatedAmountValueResult;
                    StringSearchResult aggregatedAmountPostfixResult;
                    ParseRegionValue(namePersonRegion, aggregatedAmountPrefixSearch, aggregatedAmountPostfixSearch, aggregatedAmountRegex2,
                        out aggregatedAmountPrefixResult, out aggregatedAmountValueResult, out aggregatedAmountPostfixResult);

                    StringSearchResult percentOwnedPrefixResult;
                    StringSearchResult percentOwnedValueResult;
                    StringSearchResult percentOwnedPostfixResult;
                    ParseRegionValue(namePersonRegion, percentOwnedPrefixSearch, percentOwnedPostfixSearch, percentOwnedRegex2,
                        out percentOwnedPrefixResult, out percentOwnedValueResult, out percentOwnedPostfixResult);

                    
                    yield return new ParseResult
                    {
                        NamePersonPrefix = namePersonPrefixMatch.Keyword,
                        NamePersonValue = namePersonValueMatch.Keyword,
                        NamePersonPostfix = namePersonPostfixMatch.Keyword,

                        AggregatedAmountPrefix = aggregatedAmountPrefixResult.Keyword,
                        AggregatedAmountValue = aggregatedAmountValueResult.Keyword,
                        AggregatedAmountPostfix = aggregatedAmountPostfixResult.Keyword,

                        PercentOwnedPrefix = percentOwnedPrefixResult.Keyword,
                        PercentOwnedValue = percentOwnedValueResult.Keyword,
                        PercentOwnedPostfix = percentOwnedPostfixResult.Keyword,

                        Region = namePersonRegion.Substring(0, namePersonRegion.LastIndexOf(' '))
                    };
                }
            }
            else
            {
                yield return new ParseResult();

            }
        }

        private static void ParseRegionValue(string trimText, StringSearch valuePrefixSearch, StringSearch valuePostfixSearch, Regex valueRegex,
            out StringSearchResult valuePrefixResult, out StringSearchResult valueResult, out StringSearchResult valuePostfixResult)
        {
            valuePrefixResult = StringSearchResult.Empty;
            valueResult = StringSearchResult.Empty;
            valuePostfixResult = StringSearchResult.Empty;

            StringSearchResult[] searchMatches = valuePrefixSearch.FindAll(trimText);
            if (!searchMatches.Any())
            {
                Match regexMatch = valueRegex.Match(trimText);
                if (regexMatch.Success)
                {
                    valuePrefixResult = new StringSearchResult(regexMatch.Groups[1].Index, regexMatch.Groups[1].Value);
                    valueResult = new StringSearchResult(regexMatch.Groups[2].Index, regexMatch.Groups[2].Value);
                    valuePostfixResult = new StringSearchResult(regexMatch.Groups[3].Index, regexMatch.Groups[3].Value);
                }
                return;
            }

            int minIndex = searchMatches.Min(x => x.Index);
            valuePrefixResult = searchMatches.
                Where(x => x.Index == minIndex).
                Aggregate((target, next) => target.Keyword.Length > next.Keyword.Length ? target : next);
            
            int iValue = valuePrefixResult.Index + valuePrefixResult.Keyword.Length;
            string regionText = trimText.Substring(iValue, trimText.Length - iValue);
            valuePostfixResult = valuePostfixSearch.FindFirst(regionText);
            valueResult = valuePostfixResult.IsEmpty 
                ? StringSearchResult.Empty
                : new StringSearchResult(iValue, regionText.Substring(0, valuePostfixResult.Index));
        }

        private void ParseNamePersons(string trimText, out List<StringSearchResult> namePersonPrefixResult, out List<StringSearchResult> namePersonValueResult, out List<StringSearchResult> namePersonPostfixResult)
        {
            namePersonPrefixResult = new List<StringSearchResult>();
            namePersonValueResult = new List<StringSearchResult>();
            namePersonPostfixResult = new List<StringSearchResult>();

            StringSearchResult[] searchMatches = namePersonPrefixSearch.
                FindAll(trimText).
                GroupBy(x => x.Index).
                Select(x => x.Aggregate((target, next) => next.Keyword.Length < target.Keyword.Length ? target : next)).
                ToArray();

            if (searchMatches.Any())
            {
                foreach (StringSearchResult namePersonPrefixMatch in searchMatches)
                {
                    int iNamePersonValue = namePersonPrefixMatch.Index + namePersonPrefixMatch.Keyword.Length;
                    string regionText = trimText.Substring(iNamePersonValue, PrefixWorkbook.NamePersonMaxLength + PrefixWorkbook.NamePersonPostfixMaxLength);
                    StringSearchResult namePersonPostfixMatch = namePersonPostfixSearch.FindFirst(regionText);
                    StringSearchResult namePersonValueMatch = namePersonPostfixMatch.IsEmpty 
                        ? StringSearchResult.Empty
                        : new StringSearchResult(iNamePersonValue, regionText.Substring(0, namePersonPostfixMatch.Index));

                    namePersonPrefixResult.Add(namePersonPrefixMatch);
                    namePersonValueResult.Add(namePersonValueMatch);
                    namePersonPostfixResult.Add(namePersonPostfixMatch);
                }
            }
            else
            {
                MatchCollection regexMatches = namePersonRegex2.Matches(trimText);
                foreach (Match namePersonMatch in regexMatches)
                {
                    var namePersonPrefixMatch = new StringSearchResult(
                        namePersonMatch.Groups[1].Index,
                        namePersonMatch.Groups[1].Value);
                    var namePersonValueMatch = new StringSearchResult(
                        namePersonMatch.Groups[2].Index,
                        namePersonMatch.Groups[2].Value);
                    var namePersonPostfixMatch = new StringSearchResult(
                        namePersonMatch.Groups[3].Index,
                        namePersonMatch.Groups[3].Value);

                    namePersonPrefixResult.Add(namePersonPrefixMatch);
                    namePersonValueResult.Add(namePersonValueMatch);
                    namePersonPostfixResult.Add(namePersonPostfixMatch);
                }
            }
        }

        public ParseResult ParseByRegexp(string trimText)
        {
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
            Group aggregatedAmountPostfixGroup = aggregatedAmountMatch?.Groups[3];

            Match percentOwnedMatch = ParsePercentOwned(namePersonRegion);
            Group percentOwnedPrefixGroup = percentOwnedMatch?.Groups[1];
            Group percentOwnedValueGroup = percentOwnedMatch?.Groups[2];
            Group percentOwnedPostfixGroup = percentOwnedMatch?.Groups[3];

            return new ParseResult
            {
                NamePersonPrefix = namePersonPrefixGroup.Value,
                NamePersonValue = namePersonValueGroup.Value,

                AggregatedAmountPrefix = aggregatedAmountPrefixGroup?.Value,
                AggregatedAmountValue = aggregatedAmountValueGroup?.Value,
                AggregatedAmountPostfix = aggregatedAmountPostfixGroup?.Value,

                PercentOwnedPrefix = percentOwnedPrefixGroup?.Value,
                PercentOwnedValue = percentOwnedValueGroup?.Value,
                PercentOwnedPostfix = percentOwnedPostfixGroup?.Value,
                PercentOwned = percentOwnedMatch?.Value,

                Region = namePersonRegion.Substring(0, namePersonRegion.LastIndexOf(' '))
            };
        }

        private Match ParseAggregatedAmount(string trimText)
        {
            MatchCollection aggregatedAmountMatches = aggregatedAmountRegex1.Matches(trimText);
            if (aggregatedAmountMatches.Count == 0)
            {
                aggregatedAmountMatches = aggregatedAmountRegex2.Matches(trimText);
            }
            return aggregatedAmountMatches.Count == 0 ? null : aggregatedAmountMatches[0];
        }

        private Match ParsePercentOwned(string trimText)
        {
            MatchCollection percentOwnedMatches = percentOwnedRegex1.Matches(trimText);
            if (percentOwnedMatches.Count == 0)
            {
                percentOwnedMatches = percentOwnedRegex2.Matches(trimText);
            }
            return percentOwnedMatches.Count == 0 ? null : percentOwnedMatches[0];
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


        public static readonly string[] NamePersonPrefixes =
        {
            "1 (ENTITIES ONLY)",
            "NAME AND IRS IDENTIFICATION NO OF REPORTING PERSON",
            "NAME AND IRS NUMBER OF REPORTING PERSONS",
            "NAME AND IRS IDENTIFICATION NUMBER OF REPORTING PERSON",
            "NAME OF IRS IDENTIFICATION NO OF REPORTING PERSON",
            "NAME OF REPORTING PERSON",
            "NAME OF REPORTING PERSON (SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON 1 SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON 1SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON I R S IDENTIFICATION NO OF ABOVE PERSON (ENTITIES ONLY)",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSON (ENTITIES ONLY)",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSONS",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NO(S) OF ABOVE PERSON",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NOS OF ABOVE PERSON (ENTITIES ONLY)",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION NOS OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAME OF REPORTING PERSON IRS IDENTIFICATION OF ABOVE PERSON",
            "NAME OF REPORTING PERSON OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON SS OF IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON SS OR IRS IDENTIFICATION NO OF ABOVE PERSON (ENTITIES ONLY)",
            "NAME OF REPORTING PERSON SS OR IRS IDENTIFICATION NOS OF ABOVE PERSON",
            "NAME OF REPORTING PERSON SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAME OF REPORTING PERSON SS OR IRS IDENTIFICATION NOS OF REPORTING PERSON",
            "NAME OF REPORTING PERSON SS OR IRS INDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON SS OR IRS INDENTIFICATION NO OF ABOVE PERSON (ENTITIES ONLY)",
            "NAME OF REPORTING PERSON SS OR IRSIDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSON(S) IRS IDENTIFICATION NO OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAME OF REPORTING PERSON(S) SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSONIRS IDENTIFICATION NO OF ABOVE PERSONS",
            "NAME OF REPORTING PERSONOR IRS IDENTIFICATION NO OF ABOVE PERSON (ENTITIES ONLY)",
            "NAME OF REPORTING PERSONS",
            "NAME OF REPORTING PERSONS I R S IDENTIFICATION NOS OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATIOIN NOS OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSON (ENTITIES ONLY)",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSONS",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NO(S) OF ABOVE PERSON",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NOS OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAME OF REPORTING PERSONS IRS IDENTIFICATION NOS OF REPORTING PERSONS (ENTITIES ONLY)",
            "NAME OF REPORTING PERSONS SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSONS SS OR IRS IDENTIFICATION NO OF ABOVE PERSONS",
            "NAME OF REPORTING PERSONS SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAME OF REPORTING PERSONS SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAME OF REPORTING PERSONSS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAME OF REPORTING PERSONSS OR IRS IDENTIFICATION NO OF ABOVE PERSON (ENTITIES ONLY)",
            "NAME OF REPORTING SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAMES OF REPORTING PER SONS SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAMES OF REPORTING PERSON",
            "NAMES OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAMES OF REPORTING PERSON IRS IDENTIFICATION NO OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSON IRS IDENTIFICATION NOS OF ABOVE PERSON (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSON SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAMES OF REPORTING PERSONS",
            "NAMES OF REPORTING PERSONS (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSONS AND IRS IDENTIFICATION NOS OF SUCH PERSONS (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSON (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NO OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NOS OF ABOVE PERSON (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NOS OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NOS OR ABOVE PERSONS (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSONS IRS IDENTIFICATION NUMBERS OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSONS SS OR IRS IDENTIFICATION NO OF ABOVE PERSON",
            "NAMES OF REPORTING PERSONS SS OR IRS IDENTIFICATION NO OF ABOVE PERSONS",
            "NAMES OF REPORTING PERSONS SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS",
            "NAMES OF REPORTING PERSONS SS OR IRS IDENTIFICATION NOS OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSONSIRS IDENTIFICATION NO OF ABOVE PERSONS (ENTITIES ONLY)",
            "NAMES OF REPORTING PERSONSIRS IDENTIFICATION NOS OF ABOVE PERSONS (ENTITIES ONLY)",

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

        public static readonly string[] NamePersonPostfixes =
        {
            "2) CHECK",
            "2) MEMBER",
            "2 CHECK",
            "2 MEMBER",
            "2CHECK",
            "2MEMBER",
        };

        public static readonly int NamePersonPostfixMaxLength = NamePersonPostfixes.Max(x => x.Length);
        

        public static readonly string[] AggregatedAmountPrefixes =
        {
            "11 AGGREGATE AMOUNT BENEFICALLY OWNED BY EACH REPORTING PERSON",
            "11 AGGREGATE AMOUNT BENEFICIALLY OWNED",
            "11 AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH PERSON",
            "11 AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "11 AGGREGATE AMOUNT BENEFICIALLY OWNED BY REPORTING PERSON",
            "11 AGGREGATE AMOUNT BENEFICIALLY OWNED BY THE REPORTING PERSON",
            "11 AGGREGATE AMOUNT OF BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "11 AGGREGATE AMOUNT OWNED BY EACH REPORTING PERSON",
            "11 AGGREGATE AMOUT BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "11) AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "11)AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "11AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "9 AGGREGATE AMOUNT BENEFICALLY OWNED BY EACH REPORTING PERSON",
            "9 AGGREGATE AMOUNT BENEFICIALLY OWNED",
            "9 AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH PERSON",
            "9 AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "9 AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTINGPERSON",
            "9 AGGREGATE AMOUNT BENEFICIALLY OWNED BY REPORTING PERSON",
            "9 AGGREGATE AMOUNT BENEFICIALLY OWNED BY THE REPORTING PERSON",
            "9 AGGREGATE AMOUNT BENEFICIALY OWNED BY EACH REPORTING PERSON",
            "9 AGGREGATE AMOUNT BENFICIALLY OWNED BY EACH REPORTING PERSON",
            "9 AGGREGATE AMOUNT OF BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "9 AGGREGATED AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "9) AGGREGATE AMOUNT BENEFICIALLY OWNED",
            "9) AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "9)AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "9AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON",
            "AGGREGATE AMOUNT BENEFICIALLY OWNED BY EACH REPORTING PERSON",

            // Regexps
            /*
             "11 AGGREGATE AMOUNT BENEFICIALLY OWNED {0} BY EACH REPORTING PERSON",
             "9 AGGREGATE AMOUNT BENEFICIALLY OWNED {0} BY EACH REPORTING PERSON",
             */
        };

        public static readonly string[] AggregatedAmountPostfixes = 
        {
            "10 CHECK BOX IF AGGREGATE",
            "10 CHECK BOX IF THE AGGREGATE",
            "10 CHECK IF AGGREGATE",
            "10 CHECK IF THE AGGREGATE",
            "10) AGGREGATE AMOUNT",
            "10) CHECK BOX IF THE AGGREGATE",
            "10) CHECK IF AGGREGATE",
            "10) CHECK IF THE AGGREGATE",
            "10)CHECK BOX IF THE AGGREGATE",
            "10)CHECK IF THE AGGREGATE",
            "10CHECK BOX IF THE AGGREGATE",
            "10CHECK IF THE AGGREGATE",
            "12 CHECK BOX IF AGGREGATE",
            "12 CHECK BOX IF THE AGGREGATE",
            "12 CHECK IF AGGREGATE",
            "12 CHECK IF THE AGGREGATE",
            "12) CHECK BOX IF THE AGGREGATE",
            "12) CHECK IF THE AGGREGATE",
            "12)CHECK BOX IF THE AGGREGATE",
            "12)CHECK IF THE AGGREGATE",
            "12CHECK BOX IF THE AGGREGATE",
            "12CHECK IF THE AGGREGATE",
            "AGGREGATE AMOUNT",
            "CHECK BOX IF THE AGGREGATE",
            "CHECK IF THE AGGREGATE",

        };

        public static int AggregatedAmountPostfixMaxLength = AggregatedAmountPostfixes.Max(x => x.Length);
        

        public static readonly string[] PercentOwnedPrefixes =
        {
            "11 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW",
            "11 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (11)",
            "11 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (9)",
            "11 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 9",
            "11 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 9)",
            "11 PERCENT OF CLASS REPRESENTED IN ROW (9)",
            "11 PERCENTAGE OF CLASS REPRESENTED BY AMOUNT IN ROW (9)",
            "11) PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW",
            "11) PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (9)",
            "11) PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 9",
            "11)PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 9",
            "11PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (9)",
            "11PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 9",
            "13 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW",
            "13 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (11)",
            "13 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (11) (SEE ITEM 5)",
            "13 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (9)",
            "13 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 11",
            "13 PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 9",
            "13 PERCENT OF CLASS REPRESENTED BY AMOUNT OF ROW (11)",
            "13 PERCENT OF CLASS REPRESENTED BY ROW 11",
            "13 PERCENT OF CLASS REPRESENTED IN ROW (11)",
            "13 PERCENT OF SERIES REPRESENTED BY AMOUNT IN ROW (11)",
            "13 PERCENTAGE OF CLASS REPRESENTED BY AMOUNT IN ROW (11)",
            "13) PERCENT OF CLASS REPRESENTED BY AMOUNT IN BOX (11)",
            "13) PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW",
            "13) PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (11)",
            "13) PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (9)",
            "13) PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 11",
            "13) PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 9",
            "13)PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 11",
            "13PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (11)",
            "13PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW 11",
            "PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (11)",
            "PERCENT OF CLASS REPRESENTED BY AMOUNT IN ROW (9)",
        };

        // 13) PERCENT OF CLASS REPRESENTED BY AMOUNT IN BOX (11) 0.0
        // 14) TYPE
        public static readonly string[] PercentOwnedPostfixes = 
        {
            "12 TYPE",
            "14 TYPE",
            "12) TYPE",
            "14) TYPE",
            "12TYPE",
            "14TYPE",
            "TYPE OF REPORTING PERSON",
            
            // Regexps
            /*
            "FOR {0} 12 TYPE",
            "FOR {0} 14 TYPE",
            */
        };

        public static int PercentOwnedPostfixMaxLength = PercentOwnedPostfixes.Max(x => x.Length);
    }
}
