// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

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
            "sheep", "species", "salmon", "trout", "data", "info"
        };

        // Get the pluralized word. Exceptions aren't handled here for now.
        public string Pluralize(string noun)
        {
            if (noun.EndsWith("Data", StringComparison.InvariantCultureIgnoreCase) || noun.EndsWith("Info", StringComparison.InvariantCultureIgnoreCase) ||
                noun.EndsWith("Information", StringComparison.InvariantCultureIgnoreCase))
                return noun;

            if (noun.EndsWith("y", StringComparison.InvariantCultureIgnoreCase))
            {
                if (noun[noun.Length - 2] == 'a' || noun[noun.Length - 2] == 'e' || noun[noun.Length - 2] == 'i' ||
                    noun[noun.Length - 2] == 'o' || noun[noun.Length - 2] == 'u')
                    return noun + "ies";
                else
                    return noun.Remove(noun.Length - 1, 1) + "ies";
            }

            if (noun.EndsWith("o", StringComparison.InvariantCultureIgnoreCase))
            {
                if (noun[noun.Length - 2] == 'a' || noun[noun.Length - 2] == 'e' || noun[noun.Length - 2] == 'i' ||
                    noun[noun.Length - 2] == 'o' || noun[noun.Length - 2] == 'u')
                    return noun + "oes";
                else
                    return noun + "s";
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
