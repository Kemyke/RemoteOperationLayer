using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ArdinDIContainer
{
    public partial class DIContainer
    {
        private class RegisteredTypeContainer
        {
            private SpinLock syncRoot = new SpinLock();
            private IDIContainer diContainer = null;
            private Type registeredType = null;
            private Func<object> getInstanceFunc = null;
            private List<Type> unregisteredTypesWeAreDependingOn = null;

            public RegisteredTypeContainer(IDIContainer diContainer, Type registeredType, Func<object> getInstanceFunc, List<Type> typesWeAreDependingOn)
            {
                if (diContainer == null)
                {
                    throw new ArgumentNullException("diContainer");
                }
                if (registeredType == null)
                {
                    throw new ArgumentNullException("registeredType");
                }
                if (getInstanceFunc == null)
                {
                    throw new ArgumentNullException("getInstanceFunc");
                }

                this.diContainer = diContainer;
                this.registeredType = registeredType;
                this.getInstanceFunc = getInstanceFunc;
                this.unregisteredTypesWeAreDependingOn = typesWeAreDependingOn;

                if (typesWeAreDependingOn != null && typesWeAreDependingOn.Any())
                {
                    diContainer.NewTypesRegistered += diContainer_NewTypesRegistered;
                }
            }

            public object GetInstance()
            {
                object ret = null;

                if (!IsAvailable())
                {
                    throw new InvalidOperationException(String.Format("Cannot get instance of '{0}', because following dependencies are missing: {1}", registeredType, String.Join(",", this.unregisteredTypesWeAreDependingOn.Select(t => t.ToString()).ToArray())));
                }

                ret = getInstanceFunc();

                return ret;
            }

            public bool IsAvailable()
            {
                bool ret = true;

                bool lockTaken = false;
                try
                {
                    syncRoot.Enter(ref lockTaken);

                    if (unregisteredTypesWeAreDependingOn != null && unregisteredTypesWeAreDependingOn.Any())
                    {
                        CheckDependencies();
                        ret = !unregisteredTypesWeAreDependingOn.Any();
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        syncRoot.Exit();
                    }
                }

                return ret;
            }

            private void diContainer_NewTypesRegistered(object sender, EventArgs e)
            {
                bool lockTaken = false;
                try
                {
                    syncRoot.Enter(ref lockTaken);

                    CheckDependencies();
                }
                finally
                {
                    if (lockTaken)
                    {
                        syncRoot.Exit();
                    }
                }
            }

            private void CheckDependencies()
            {
                foreach (var t in unregisteredTypesWeAreDependingOn.ToList())
                {
                    if (diContainer.IsRegistered(t))
                    {
                        unregisteredTypesWeAreDependingOn.Remove(t);
                    }
                }

                if (!unregisteredTypesWeAreDependingOn.Any())
                {
                    diContainer.NewTypesRegistered -= diContainer_NewTypesRegistered;
                }
            }
        }
    }
}
