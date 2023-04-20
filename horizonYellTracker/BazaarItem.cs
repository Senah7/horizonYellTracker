using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace horizonYellTracker
{
    public class BazaarItem
    {
        public BazaarItem()
        {
            Charname = "";
            Name = "";
            Bazaar = 0;
        }
        public string Charname { get; set; }
        public string Name { get; set; }
        public int Bazaar { get; set; }
    }
}
