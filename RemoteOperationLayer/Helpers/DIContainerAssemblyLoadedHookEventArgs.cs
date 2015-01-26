using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ArdinDIContainer
{
    public class DIContainerAssemblyLoadedHookEventArgs : EventArgs
    {
        public Assembly LoadedAssembly { get; private set; }
        public bool AutoRegisterTypesInLoadedAssembly { get; set; }

        public DIContainerAssemblyLoadedHookEventArgs(Assembly loadedAssembly)
        {
            if (loadedAssembly == null)
            {
                throw new ArgumentNullException("loadedAssembly");
            }

            this.LoadedAssembly = loadedAssembly;
            this.AutoRegisterTypesInLoadedAssembly = true;
        }
    }
}
