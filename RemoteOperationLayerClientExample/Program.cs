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

namespace RemoteOperationLayerClientExample
{
    public class Program
    {
        static void Main(string[] args)
        {
            IDIContainer di = CreateDI();

            di.Register<IWCFConfigManager>(() => new WCFConfigManager());

            IRemoteOperationHandler roh = new RemoteOperationHandler(di);
            RemoteSideCommunicator roc = new RemoteSideCommunicator(roh);
            di.Register<IRemoteSideCommunicationContract>(() => roc);
            di.Register<IRemoteSideCommunicationHandler>(() => roc);

            WCFServiceClientFactory factory = new WCFServiceClientFactory(di);
            var rs = factory.CreateInstance();

            System.Console.WriteLine("Client started!");
            System.Console.WriteLine("Press Enter to call server side Add method.");
            System.Console.ReadLine();

            RemoteOperationDescriptor rod = new RemoteOperationDescriptor(typeof(ICalc).AssemblyQualifiedName, "Add", 1,2);
            int sum = roc.ExecuteOnRemoteSide<int>(rs.ID, rod);

            System.Console.WriteLine("Add(1,2) = {0}", sum);
            System.Console.WriteLine("Press Enter to stop service client!");
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
