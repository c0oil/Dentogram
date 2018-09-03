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
            public string NamePerson;

            public string AggregatedAmountPrefix;
            public string AggregatedAmountValue;
            public string AggregatedAmountPostfix;
            public string AggregatedAmount;

            public string PercentOwnedPrefix;
            public string PercentOwnedValue;
            public string PercentOwnedPostfix;
            public string PercentOwned;

            public string Region;
        }
        
        // Name Person
        private readonly Regex[] namePersonPrefixRegex =
        {
            new Regex(@"NAMES? OF REPORTING(?: PER ?SONS?\(?S?\)?)(?:(?: [1\(]? ?SS)? OR| AND)?(?: ?I ?R ?S ?IN?DENTIFICATION(?: NUMBERS?| NO\(?S?S?\)?)?(?: O[FR] (?:ABOVE|REPORTING) PERSON\(?S?S?\)?)?)?(?: \(ENTIT(?:Y|IES) ONLY\))?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"NAME (?:AND|OR) IRS(?: IDENTIFICATION)? (?:NO|NUMBER) OF REPORTING PERSONS?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"1 \(ENTITIES ONLY\)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        };
        private readonly Regex[] namePersonValueRegex =
        {
            new Regex(@"([\w\s\(\),]{4,}?)(?:\(1\))?(?: \( ?THE REPORTING PERSON ?\))?(?:(?: SS)? OR)?(?: DATED [A-Z]{3,11} \d{1,2}(?: \d{1,2})? \d{2,4})? ?\(?(?:IRS)? IDENTIFICATION NOS? OF(?: THE)? ABOVE PERSONS?(?: \(ENTITIES ONLY\)|IRS NO)? ?(?:\d{2,2} \d{6,8}|N A|NOT APPLICABLE)?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"([\w\s\(\),]{4,}?)DATED [A-Z]{3,11} \d{1,2}(?: \d{1,2})? \d{2,4}(?: IRS IDENTIFICATION NOS OF ABOVE PERSONS \(ENTITIES ONLY\))?(?: \d{2,2} \d{6,8})?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"^ ?(?:\d{2,2} )?\d{6,8} ([\w\s\(\),]{4,})$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?:1)?([\w\s\(\),]{4,}?)\(? ?(?:IRS)? ?(?:(?:FEDERAL )?ID(?:ENTIFICATION)?(?: NO| NUMBER)?|NO|EIN|(?:\(B\) )?TAX(?: ID)?|DIRECTLY AND ON BEHALF OF CERTAIN SUBSIDIARIES)? ?(?:\d{2,2} (?:\d{6,8}|\d{3,3} \d{4,4})|[\dX]{3,3} [\dX]{2,2} [\dX]{4,4}|\d{7,9})\)?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"([\w\s\(\),]{4,}?)\((?:1|NO IRS IDENTIFICATION NO|NONE|NO EIN|N A)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?:\(VOLUNTARY\|\(OPTIONAL\)|1 ?\)? |\.{2,})(?: EIN NO)?([\w\s\(\),]{4,}?)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            //  PLEASE CREATE A SEPARATE COVER SHEET FOR EACH ENTITY
        };
        private readonly Regex[] namePersonPostfixRegex =
        {
            new Regex(@"\(?(?:2|14)\)? ?(?:CHECK|MEMBER)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        };
        
        // Aggregated Amount
        private readonly Regex aggregatedAmountPrefixRegex = new Regex(@"(?:\(?(?:9|11)\)? ?)?AGGREGATED? AMOUN?T(?:(?: OF)? BENE?FICI?AL?LY)? OWNED(?: BY(?: EACH)?(?: REPORTING)? ?PERSON(?: \(DISCRETIONARY NON DISCRETIONARY ACCOUNTS\))?)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex aggregatedAmountPostfixRegex = new Regex(@"(?:\(?1[02]\)? ?)?(?:CHECK(?: BOX)? IF(?: THE)? AGGREGATE|AGGREGATE AMOUNT)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex aggregatedAmountValueRegex = new Regex(@"(?:^|[^\(])((?:(?:\d{1,3}(?: \d{3,3})+)|\d+(?:\,\d+)*)|\*|NONE|NIL SHARES OF COMMON STOCK|SEE (?:ITEM|ROW) \d)(?!\))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Percent Owned
        private readonly Regex percentOwnedPrefixRegex = new Regex(@"\(?1?[123]\)? ?PERCENT(?:AGE)? OF (?:CLASS|SERIES) REPRESENTED(?: BY)?(?: AMOUNT)?(?: IN| OF)? (?:ROW|BOX)(?: ?(?:\( ?)?(?:9|11)(?: ?\))?)?(?: ?\(SEE ITEM 5\))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex percentOwnedPostfixRegex = new Regex(@"(?:\(?1[24]\)? ?TYPE|TYPE OF REPORTING PERSON)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex percentOwnedValueRegex = new Regex(@"(?:^|[^\(])(?:\d+(?:\,\d+)+ \d+(?:\,\d+)+ )?((?:\d+(?:\.\d+)?)|\*) ?%?(?!\))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex namePersonFullRegex = new Regex(@"(CUSIP(?: NUMBER)? [\w]+ ITEM 1 REPORTING PERSON) ([\s\S]{0,200}?)(?:\d{2,2} \d{6,8})? (ITEM \d)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex aggregatedFullAmountRegex = new Regex(@"(ITEM 9) ((?:\d+(?:\,\d+)*)|\*|NONE) (ITEM 11)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex percentOwnedFullRegex = new Regex(@"(ITEM 11) ((?:\d+(?:\.\d+)?)|\*) ?%? (ITEM 12)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            /*
            string trimText = text;
            trimText = Regex.Replace(trimText, @"\.{2,}", ".");
            trimText = Regex.Replace(trimText, @",{2,}", ",");
            */
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
                    ParseRegionValue(namePersonRegion, aggregatedAmountPrefixRegex, aggregatedAmountPostfixRegex, aggregatedAmountValueRegex, aggregatedFullAmountRegex,
                        out aggregatedAmountPrefixResult, out aggregatedAmountValueResult, out aggregatedAmountPostfixResult);

                    StringSearchResult percentOwnedPrefixResult;
                    StringSearchResult percentOwnedValueResult;
                    StringSearchResult percentOwnedPostfixResult;
                    ParseRegionValue(namePersonRegion, percentOwnedPrefixRegex, percentOwnedPostfixRegex, percentOwnedValueRegex, percentOwnedFullRegex,
                        out percentOwnedPrefixResult, out percentOwnedValueResult, out percentOwnedPostfixResult);

                    yield return new ParseResult
                    {
                        NamePersonPrefix = namePersonPrefixMatch.Keyword,
                        NamePersonValue = namePersonValueMatch.Keyword,
                        NamePersonPostfix = namePersonPostfixMatch.Keyword,
                        NamePerson = Test(trimText, namePersonPrefixMatch, namePersonPostfixMatch),

                        AggregatedAmountPrefix = aggregatedAmountPrefixResult.Keyword,
                        AggregatedAmountValue = aggregatedAmountValueResult.Keyword,
                        AggregatedAmountPostfix = aggregatedAmountPostfixResult.Keyword,
                        AggregatedAmount = Test(namePersonRegion, aggregatedAmountPrefixResult, aggregatedAmountPostfixResult),

                        PercentOwnedPrefix = percentOwnedPrefixResult.Keyword,
                        PercentOwnedValue = percentOwnedValueResult.Keyword,
                        PercentOwnedPostfix = percentOwnedPostfixResult.Keyword,
                        PercentOwned = Test(namePersonRegion, percentOwnedPrefixResult, percentOwnedPostfixResult),

                        Region = namePersonRegion.Substring(0, namePersonRegion.LastIndexOf(' '))
                    };
                }
            }
            else
            {
                yield return new ParseResult();
            }
        }

        private string Test(string namePersonRegion, StringSearchResult aggregatedAmountPrefixResult, StringSearchResult aggregatedAmountPostfixResult)
        {
            try
            {
                return aggregatedAmountPrefixResult.IsEmpty || aggregatedAmountPostfixResult.IsEmpty
                    ? string.Empty
                    : namePersonRegion.Substring(
                        aggregatedAmountPrefixResult.Index + aggregatedAmountPrefixResult.Keyword.Length,
                        aggregatedAmountPostfixResult.Index - (aggregatedAmountPrefixResult.Index + aggregatedAmountPrefixResult.Keyword.Length));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void ParseRegionValue(string text, Regex valuePrefixRegex, Regex valuePostfixRegex, Regex valueRegex, Regex fullValueRegex,
            out StringSearchResult valuePrefixResult, out StringSearchResult valueResult, out StringSearchResult valuePostfixResult)
        {
            valuePrefixResult = StringSearchResult.Empty;
            valueResult = StringSearchResult.Empty;
            valuePostfixResult = StringSearchResult.Empty;

            valuePrefixResult = ParseFirstByRegexp(text, valuePrefixRegex, 0);
            if (valuePrefixResult.IsEmpty)
            {
                Match regexMatch = fullValueRegex.Match(text);
                if (regexMatch.Success)
                {
                    valuePrefixResult = new StringSearchResult(regexMatch.Groups[1].Index, regexMatch.Groups[1].Value);
                    valueResult = new StringSearchResult(regexMatch.Groups[2].Index, regexMatch.Groups[2].Value);
                    valuePostfixResult = new StringSearchResult(regexMatch.Groups[3].Index, regexMatch.Groups[3].Value);
                }
                return;
            }
            
            int iValue = valuePrefixResult.Index + valuePrefixResult.Keyword.Length;
            valuePostfixResult = ParseFirstByRegexp(text.Substring(iValue, text.Length - iValue), valuePostfixRegex, 0).OffsetResult(iValue);
            valueResult = valuePostfixResult.IsEmpty 
                ? StringSearchResult.Empty
                : ParseFirstByRegexp(text.Substring(iValue, valuePostfixResult.Index - iValue), valueRegex, 1).OffsetResult(iValue);
        }

        private void ParseNamePersons(string trimText, out List<StringSearchResult> namePersonPrefixResult, out List<StringSearchResult> namePersonValueResult, out List<StringSearchResult> namePersonPostfixResult)
        {
            namePersonPrefixResult = new List<StringSearchResult>();
            namePersonValueResult = new List<StringSearchResult>();
            namePersonPostfixResult = new List<StringSearchResult>();
            
            List<StringSearchResult> searchMatches = ParseAllByRegexp(trimText, namePersonPrefixRegex);
            if (searchMatches.Any())
            {
                foreach (StringSearchResult namePersonPrefixMatch in searchMatches)
                {
                    int iNamePersonValue = namePersonPrefixMatch.Index + namePersonPrefixMatch.Keyword.Length;
                    StringSearchResult namePersonPostfixMatch = ParseFirstByRegexp(trimText.Substring(iNamePersonValue, ParsingTextHelper.NamePersonMaxLength), namePersonPostfixRegex, 0).OffsetResult(iNamePersonValue);
                    string namePersonValueText = namePersonPostfixMatch.IsEmpty ? string.Empty: trimText.Substring(iNamePersonValue, namePersonPostfixMatch.Index - iNamePersonValue);
                    StringSearchResult namePersonValueMatch = namePersonPostfixMatch.IsEmpty 
                        ? StringSearchResult.Empty
                        : ParseFirstByRegexp(namePersonValueText, namePersonValueRegex, 1).OffsetResult(iNamePersonValue);
                    if (!namePersonPostfixMatch.IsEmpty && namePersonValueMatch.IsEmpty)
                    {
                        namePersonValueMatch = new StringSearchResult(iNamePersonValue, namePersonValueText);
                    }

                    namePersonPrefixResult.Add(namePersonPrefixMatch);
                    namePersonValueResult.Add(namePersonValueMatch);
                    namePersonPostfixResult.Add(namePersonPostfixMatch);
                }
            }
            else
            {
                MatchCollection regexMatches = namePersonFullRegex.Matches(trimText);
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

        private static List<StringSearchResult> ParseAllByRegexp(string text, IEnumerable<Regex> regexps)
        {
            foreach (Regex regexp in regexps)
            {
                var matches = regexp.Matches(text);
                if (matches.Count > 0)
                {
                    return matches.
                        OfType<Match>().
                        Select(match => new StringSearchResult(match.Index, match.Value)).
                        ToList();
                }
            }

            return new List<StringSearchResult>();
        }
        
        private static StringSearchResult ParseFirstByRegexp(string regionText, Regex regexp, int groupIndex)
        {
            return ParseFirstByRegexp(regionText, new [] { regexp }, groupIndex);
        }

        private static StringSearchResult ParseFirstByRegexp(string regionText, IEnumerable<Regex> regexps, int groupIndex)
        {
            foreach (Regex regexp in regexps)
            {
                Match match = regexp.Match(regionText);
                if (match.Success)
                {
                    return new StringSearchResult(match.Groups[groupIndex].Index, match.Groups[groupIndex].Value);
                }
            }

            return StringSearchResult.Empty;
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
    }

    public static class ParsingTextHelper
    {
        public static int NamePersonMaxLength = 450;

        public static StringSearchResult OffsetResult(this StringSearchResult result, int offset)
        {
            return result.IsEmpty ? result : new StringSearchResult(result.Index + offset, result.Keyword);
        }
    }
}
