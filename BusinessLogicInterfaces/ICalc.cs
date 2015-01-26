using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArdinRemoteOperations;

namespace BusinessLogicInterfaces
{
    [RemoteCallableType]
    public interface ICalc
    {
        [RemoteCallableFunc]
        int Add(int lp, int rp);
        [RemoteCallableFunc]
        int Subtract(int lp, int rp);
    }
}
