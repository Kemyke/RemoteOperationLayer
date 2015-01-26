using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicInterfaces;

namespace RemoteOperationLayerServerExample
{
    public class CalcImpl : ICalc
    {
        public int Add(int lp, int rp)
        {
            return lp + rp;
        }

        public int Subtract(int lp, int rp)
        {
            return lp - rp;
        }
    }
}
