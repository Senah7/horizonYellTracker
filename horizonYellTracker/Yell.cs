using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace horizonYellTracker
{
    public class Yell
    {
        public Yell()
        {
            Date = "";
            Speaker = "";
            Message = "";
        }

        public string Date { get; set; }
        public string Speaker { get; set; }
        public string Message { get; set; }
    }
}
