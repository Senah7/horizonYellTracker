using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace horizonYellTracker
{
    public class BazaarItemComparer : IEqualityComparer<BazaarItem>
    {
        public bool Equals(BazaarItem? x, BazaarItem? y)
        {
            if (x != null && y != null)
                return x.Name == y.Name;
            else return false;
        }

        public int GetHashCode(BazaarItem obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
