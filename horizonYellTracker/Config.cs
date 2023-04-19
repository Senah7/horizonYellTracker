using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace horizonYellTracker
{
    public class Config
    {
        public Config()
        {
            Queries = new List<string>();
        }

        public List<string> Queries { get; set; }
        public string? Webhook { get; set; }
    }
}
