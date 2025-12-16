using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjacentCountryFinder
{
    public class CountryDetail
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Neighbors { get; set; } = new();
    }
}
