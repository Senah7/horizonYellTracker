using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace horizonYellTracker
{
    public class YellList
    {
        public YellList()
        {
            ListOfYells = new List<Yell>();
        }
        List<Yell> ListOfYells { get; set; }
    }
}
