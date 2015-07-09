// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lappa_ORM.Settings;

namespace Lappa_ORM.Misc
{
    // Basic pluralization class. Needs to be extended with more exceptions.
    // English supported only.
    internal class PluralizationService
    {
        Dictionary<string, string> irregularNouns = new Dictionary<string, string>()
        {
            // -is
            ["axis"] = "axes", ["analysis"] = "analyses", ["basis"] = "bases",
            ["diagnosis"] = "diagnoses", ["ellipsis"] = "ellipses", ["hypothesis"] = "hypotheses",
            ["synthesis"] = "syntheses", ["synopsis"] = "synopses", ["thesis"] = "theses",
            // -ix
            ["appendix"] = "appendices", ["index"] = "indices", ["matrix"] = "matrices",
            // other
            ["child"] = "children" ,["man"] = "men", ["ox"] = "oxen",
            ["woman"] = "women", ["person"] = "persons",
        };

        List<string> nonChangingNouns = new List<string>()
        {
            "aircraft", "deer", "fish", "moose", "offspring",
            "sheep", "species", "salmon", "trout", "data", "info",
            "information"
        };

        public string Pluralize(string noun)
        {
            if (PluralizationSettings.PluralizationExceptions.Contains(noun))
                return noun;

            KeyValuePair<string, string> irregularNoun;

            if ((irregularNoun = irregularNouns.SingleOrDefault(s => noun.EndsWith(s.Key, StringComparison.InvariantCultureIgnoreCase))).Key != null)
                return noun.Remove(noun.Length - irregularNoun.Key.Length, irregularNoun.Key.Length) + noun[noun.Length - irregularNoun.Key.Length] + irregularNoun.Value.Substring(1);

            if (nonChangingNouns.Any(s => noun.EndsWith(s, StringComparison.InvariantCultureIgnoreCase)))
                return noun;

            if (Regex.IsMatch(noun, @"\d$"))
                return noun;

            if (noun.Length >= 2)
            {
                var nounPreEndChar = noun[noun.Length - 2];

                if (noun.EndsWith("y", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (nounPreEndChar == 'a' || nounPreEndChar == 'e' || nounPreEndChar == 'i' || nounPreEndChar == 'o' || nounPreEndChar == 'u')
                        return noun + "ies";

                    return noun.Remove(noun.Length - 1, 1) + "ies";
                }

                if (noun.EndsWith("o", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (nounPreEndChar == 'a' || nounPreEndChar == 'e' || nounPreEndChar == 'i' || nounPreEndChar == 'o' || nounPreEndChar == 'u')
                        return noun + "oes";

                    return noun + "s";
                }
            }

            if (noun.EndsWith("ies", StringComparison.InvariantCultureIgnoreCase))
                return noun;

            if (noun.EndsWith("s", StringComparison.InvariantCultureIgnoreCase) || noun.EndsWith("x", StringComparison.InvariantCultureIgnoreCase) || 
                noun.EndsWith("ch", StringComparison.InvariantCultureIgnoreCase) || noun.EndsWith("sh", StringComparison.InvariantCultureIgnoreCase))
                return noun + "es";

            return noun + "s";
        }
    }
}
