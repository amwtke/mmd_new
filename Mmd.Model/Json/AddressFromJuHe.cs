using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Model.Json
{
    public class AddressFromJuHe
    {
        public string reason { get; set; }
        public List<Province> result { get; set; }
    }

    public class Province
    {
        public int id { get; set; }
        public string province { get; set; }

        public List<City> city { get; set; }
    }

    public class City
    {
        public int id { get; set; }
        public string city { get; set; }
        public List<District> district { get; set; }
    }

    public class District
    {
        public int id { get; set; }
        public string district { get; set; }
    }
}
