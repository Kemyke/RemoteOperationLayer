using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ArdinDIContainer;
using ArdinRemoteOperations;
using ArdinRemoteOperations.WCF;
using BusinessLogicInterfaces;

namespace RemoteOperationLayerServerExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IDIContainer di = CreateDI();

            di.Register<IWCFConfigManager>(() => new WCFConfigManager());
            di.Register<ICalc>(() => new CalcImpl());

            IRemoteOperationHandler roh = new RemoteOperationHandler(di);
            RemoteSideCommunicator roc = new RemoteSideCommunicator(roh);
            di.Register<IRemoteSideCommunicationContract>(() => roc);
            di.Register<IRemoteSideCommunicationHandler>(() => roc);

            WCFServiceHostFactory factory = new WCFServiceHostFactory(di);
            var rs = factory.CreateInstance();

            rs.Open();

            System.Console.WriteLine("Server started!");
            System.Console.WriteLine("Waiting for client operations!");
            System.Console.WriteLine("Press Enter to stop service host!");
            System.Console.ReadLine();
        }

        #region Create DI

        private static IDIContainer CreateDI()
        {
            DIContainer di = new DIContainer(TestAssemblySelector, (isRunningSynchronouslyRequested, itemName, itemAction) =>
            {
                itemAction();
            });
            return di;
        }

        private static bool TestAssemblySelector(Assembly asm)
        {
            bool ret = false;

            if (asm == Assembly.GetExecutingAssembly())
            {
                ret = true;
            }

            return ret;
        }

        #endregion
    }
}
