using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APKEasyTool
{
    public class StringExtension
    {
        public static string Regex(string text, string match)
        {
            Regex myRegex = new Regex(text);
            Match matched = myRegex.Match(match);
            return matched.ToString();
        }
    }
}
