using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpiderMan.Library.Modding.Stillhere
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Use this function like string.Split but instead of a character to split on,
        ///     use a maximum line width size. This is similar to a Word Wrap where no words will be split.
        /// </summary>
        /// Note if the a word is longer than the maxcharactes it will be trimmed from the start.
        /// <param name="initial">The string to parse.</param>
        /// <param name="MaxCharacters">The maximum size.</param>
        /// <remarks>This function will remove some white space at the end of a line, but allow for a blank line.</remarks>
        /// <returns>An array of strings.</returns>
        public static List<string> SplitOn(this string initial, int MaxCharacters)
        {
            var lines = new List<string>();

            if (string.IsNullOrEmpty(initial) == false)
            {
                var targetGroup = "Line";
                var pattern = string.Format(@"(?<{0}>.{{1,{1}}})(?:\W|$)", targetGroup, MaxCharacters);

                lines = Regex.Matches(initial, pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant)
                    .OfType<Match>()
                    .Select(mt => mt.Groups[targetGroup].Value)
                    .ToList();
            }
            return lines;
        }
    }
}