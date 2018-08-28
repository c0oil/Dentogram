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
        private readonly Regex regex = new Regex(@"1\s?[\.\)]?\s?NAMES? OF REPORTING PERSONS?[\s\S]+?\d+[\.\)]?\s+TYPE OF REPORTING PERSON", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex regex1 = new Regex(@"(NAMES?(?: ?OF ?REPORTING| ?AND ?IRS) ?[\s\S]{0,100}PERSONS?(?: ?ENTITIES ONLY)? ?[\.\,]*)[\s\S]{0,400}?(?:2\.? ?CHECK|CHECK THE APPROPRIATE|MEMBER)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex regex2 = new Regex(@"(CUSIP(?: NUMBER)? [\w]+ ITEM 1 REPORTING PERSON) [\s\S]{0,200}? ITEM \d", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex regex3 = new Regex(@"NAMES?(?: ?OF)? ?REPORTING", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //private readonly Regex regexTrim = new Regex(@"\D[\,\.](?=\D)|\D[\,\.](?=\d)|\d[\,\.](?=\D)|_|[^\w\s\.,]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex regexTrim = new Regex(@"\D[\,\.](?=\D)|\D[\,\.](?=\d)|\d[\,\.](?=\D)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        //Cusip #11133B409
        //Item 1:	Reporting Person - FMR LLC
        //Item 4:	Delaware

        private string text = @"

        Cusip #11133B409
        Item 1	Reporting Person FMR LLC
        Item 4	Delaware
";

        public string TrimForClustering(string text)
        {
            string trimText = Regex.Replace(text, @"[\.,\d]", "");
            trimText = Regex.Replace(trimText, @" \w{1,3}(?= )", " ");
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

        public string[] ParseTable(string trimText)
        {
            //string trimText = Regex.Replace(text.ToUpperInvariant(), @"[-:_\(\)""]", "");
            //trimText = Regex.Replace(trimText.ToUpperInvariant(), @" \D ", " ");
            MatchCollection matches = regex1.Matches(trimText);
            if (matches.Count == 0)
            {
                matches = regex2.Matches(trimText);
            }
            if (matches.Count == 0)
            {
                return new [] { string.Empty , string.Empty, };
            }
            
            string group = matches.OfType<Match>().FirstOrDefault()?.Groups[1].Value;
            string match = matches.OfType<Match>().FirstOrDefault()?.Value;
            
            return new [] { group ?? string.Empty, group == null || match == null ? string.Empty : match.Substring(0, Math.Min(group.Length + 400, match.Length))};

            /*
            string group = matches.OfType<Match>().FirstOrDefault()?.Groups[1].Value;
            string match = matches.OfType<Match>().FirstOrDefault()?.Value;
            
            if (group == null || match == null)
            {
                return string.Empty;
            }

            return match.Substring(0, group.Length + 10);
            */
            //return matches.OfType<Match>().FirstOrDefault()?.Value ?? string.Empty;
        }

        private string TrimSigns(string text)
        {
            MatchCollection matches = regexTrim.Matches(text);
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
    }
}
