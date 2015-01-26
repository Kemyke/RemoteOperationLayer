using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using ArdinHelpers;

namespace ArdinDIContainer
{
	public partial class DIContainer : IDIContainer
	{
        private SpinLock syncRoot = new SpinLock();
        private Dictionary<Type, RegisteredTypeContainer> registeredTypes = new Dictionary<Type, RegisteredTypeContainer>();
        private bool autoRegistrationDoneOnceWithCustomRegistrationFuncDisabled = false;
        private bool autoRegistrationDoneOnceWithCustomRegistrationFuncEnabled = false;
        private bool typesWithCustomRegistrationFuncEnabled = false;

        private AppDomain ownAppDomain = null;

        private Action<bool, string, Action> runOnTypeAvailableOrRegisteredActionExecuterAction = null;
        private Func<Assembly, bool> assemblySelector = null;

        private RunOnTypeRegisteredOrAvailableQueue _runOnTypeRegisteredOrAvailableQueue = null;
        private RunOnTypeRegisteredOrAvailableQueue runOnTypeRegisteredOrAvailableQueue 
        {
            get
            {
                if (_runOnTypeRegisteredOrAvailableQueue == null)
                {
                    _runOnTypeRegisteredOrAvailableQueue = new RunOnTypeRegisteredOrAvailableQueue(this, runOnTypeAvailableOrRegisteredActionExecuterAction);
                }
                return _runOnTypeRegisteredOrAvailableQueue;
            }
        }

        /// <summary>
        /// Register given type with DIContainer.
        /// When the type will be requested with GetInstance the oGetInstanceFunc will be executed 
        /// to retrieve an instance.
        /// E.g. that func can be a ()=>{return new MyClassOfTInterface(); } style lambda to always retrieve a new instance,
        /// or a ()=>{ return MySingletonClassOfTInterface.Instance; } style one for singletons.
        /// </summary>
        public virtual void Register<TInterface>(Func<TInterface> getInstanceFunc)
			where TInterface : class
		{
            Register<TInterface>(getInstanceFunc, null);
		}

        /// <summary>
        /// Register given type with DIContainer.
        /// When the type will be requested with GetInstance the oGetInstanceFunc will be executed 
        /// to retrieve an instance.
        /// E.g. that func can be a ()=>{return new MyClassOfTInterface(); } style lambda to always retrieve a new instance,
        /// or a ()=>{ return MySingletonClassOfTInterface.Instance; } style one for singletons.
        /// </summary>
        public virtual void Register<TInterface>(Func<TInterface> getInstanceFunc, List<Type> otherTypesWeAreDependingOn)
            where TInterface : class
        {
            Register(typeof(TInterface), getInstanceFunc, otherTypesWeAreDependingOn);
        }

        /// <summary>
        /// Register given type with DIContainer.
        /// When the type will be requested with GetInstance the oGetInstanceFunc will be executed 
        /// to retrieve an instance.
        /// E.g. that func can be a ()=>{return new MyClassOfTInterface(); } style lambda to always retrieve a new instance,
        /// or a ()=>{ return MySingletonClassOfTInterface.Instance; } style one for singletons.
        /// </summary>
        public virtual void Register(Type interfaceType, Func<object> getInstanceFunc)
        {
            Register(interfaceType, getInstanceFunc, null);
        }

        /// <summary>
        /// Register given type with DIContainer.
        /// When the type will be requested with GetInstance the oGetInstanceFunc will be executed 
        /// to retrieve an instance.
        /// E.g. that func can be a ()=>{return new MyClassOfTInterface(); } style lambda to always retrieve a new instance,
        /// or a ()=>{ return MySingletonClassOfTInterface.Instance; } style one for singletons.
        /// </summary>
        public virtual void Register(Type interfaceType, Func<object> getInstanceFunc, List<Type> otherTypesWeAreDependingOn)
        {
            InternalRegister(interfaceType, getInstanceFunc, otherTypesWeAreDependingOn);
            OnNewTypesRegistered();
        }

        /// <summary>
        /// AutoRegister method could be very complicated if it should call generic Register method...
        /// No sanity checks, called from inside.
        /// </summary>
        private void InternalRegister(Type interfaceType, Func<object> getInstanceFunc, List<Type> otherTypesWeAreDependingOn)
        {
            RegisteredTypeContainer c = new RegisteredTypeContainer(this, interfaceType, getInstanceFunc, otherTypesWeAreDependingOn);

            bool gotLock = false;
            try
            {
                syncRoot.Enter(ref gotLock);

                registeredTypes[interfaceType] = c;
            }
            finally
            {
                if (gotLock)
                {
                    syncRoot.Exit();
                }
            }

            System.Diagnostics.Debug.WriteLine("{0} registered for {1} with dependencies: {2}", getInstanceFunc.Method.ToString(), interfaceType.ToString(), (otherTypesWeAreDependingOn != null) ? String.Join(", ", otherTypesWeAreDependingOn.ToList().Select(t => t.ToString()).ToArray()) : "-"); //LOCSTR
        }



        public virtual void Unregister<TInterface>()
            where TInterface : class
        {
            InternalUnregister(typeof(TInterface));
        }

        private void InternalUnregister(Type interfaceType)
        {
            bool gotLock = false;
            try
            {
                syncRoot.Enter(ref gotLock);

                registeredTypes.Remove(interfaceType);
            }
            finally
            {
                if (gotLock)
                {
                    syncRoot.Exit();
                }
            }

            System.Diagnostics.Debug.WriteLine("{0} unregistered", interfaceType.ToString()); //LOCSTR
        }

        public virtual void AutoRegisterTypes(bool typesWithCustomRegistrationFuncToo)
        {
            IList<Type> typesFound = AppDomainHelper.GetTypesMarkedWithAttribute(typeof(DIContainerAutoRegisterableTypeAttribute), assemblySelector, (a,t) => true);
            AutoRegisterTypesInternal(typesWithCustomRegistrationFuncToo, typesFound);
        }

        private bool autoRegisterTypesInternalInProgress = false;

        private void AutoRegisterTypesInternal(bool typesWithCustomRegistrationFuncToo, IList<Type> types)
        {
            if (types != null)
            {
                if (!autoRegisterTypesInternalInProgress)
                {
                    try
                    {
                        autoRegisterTypesInternalInProgress = true;

                        typesWithCustomRegistrationFuncEnabled = typesWithCustomRegistrationFuncToo;

                        Type autoRegisterableTypeAttributeType = typeof(DIContainerAutoRegisterableTypeAttribute);
                        Type autoRegisterGetInstanceFuncAttributeType = typeof(DIContainerAutoRegisterGetInstanceFuncAttribute);
                        Type autoRegisterCustomRegistrationFuncAttributeType = typeof(DIContainerAutoRegisterCustomRegistrationFuncAttribute);

                        foreach (Type t in types)
                        {
                            var attr = (DIContainerAutoRegisterableTypeAttribute)t.GetCustomAttributes(autoRegisterableTypeAttributeType, false).FirstOrDefault();

                            bool success = AutoRegisterViaGetInstanceFunc(t, attr, autoRegisterGetInstanceFuncAttributeType);
                            if (!success && typesWithCustomRegistrationFuncToo)
                            {
                                success = AutoRegisterViaCustomRegistrationFunc(t, attr, autoRegisterCustomRegistrationFuncAttributeType);
                            }

                            if (!success && typesWithCustomRegistrationFuncToo)
                            {
                                throw new InvalidOperationException(String.Format("Type: {0} marked with {1} but has no static method marked with {2} or {3}!", t.FullName, autoRegisterableTypeAttributeType.Name, autoRegisterGetInstanceFuncAttributeType.Name, autoRegisterCustomRegistrationFuncAttributeType.Name)); //LOCSTR
                            }
                        }
                    }
                    finally
                    {
                        autoRegisterTypesInternalInProgress = false;
                        
                        if (!typesWithCustomRegistrationFuncToo)
                        {
                            autoRegistrationDoneOnceWithCustomRegistrationFuncDisabled = true;
                        }
                        else
                        {
                            autoRegistrationDoneOnceWithCustomRegistrationFuncEnabled = true;
                        }
                    }

                    OnNewTypesRegistered();
                }
            }
        }

        private bool AutoRegisterViaCustomRegistrationFunc(Type t, DIContainerAutoRegisterableTypeAttribute autoRegisterableTypeAttribute, Type autoRegisterCustomRegistrationFuncAttributeType)
        {
            bool ret = false;

            IList<MethodInfo> customRegistrationMethodsFound = AppDomainHelper.GetMethodsMarkedWithAttribute(t, autoRegisterCustomRegistrationFuncAttributeType, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (customRegistrationMethodsFound.Any())
            {
                var q = from mi in customRegistrationMethodsFound
                        where !mi.IsStatic
                        select mi;
                if (q.Any())
                {
                    throw new InvalidOperationException(String.Format("Type: {0} marked with {1} but has a nonstatic method marked with {2}!", t.FullName, autoRegisterableTypeAttribute.GetType().Name, autoRegisterCustomRegistrationFuncAttributeType.Name)); //LOCSTR
                }

                foreach (var mi in customRegistrationMethodsFound)
                {
                    var piList = mi.GetParameters();
                    if ((piList.Length != 1) || (piList[0].ParameterType != typeof(IDIContainer)))
                    {
                        throw new InvalidOperationException(String.Format("Method {0}.{1} marked with {2}, but declared nos as 'static void method(IDIContainer)'!", mi.DeclaringType.FullName, mi.Name, autoRegisterCustomRegistrationFuncAttributeType));
                    }

                    Action<IDIContainer> customRegFunc = (Action<IDIContainer>)Delegate.CreateDelegate(typeof(Action<IDIContainer>), null, mi);
                    customRegFunc(this);
                }

                ret = true;
            }


            return ret;
        }

        private bool AutoRegisterViaGetInstanceFunc(Type t, DIContainerAutoRegisterableTypeAttribute autoRegisterableTypeAttribute, Type autoRegisterGetInstanceFuncAttributeType)
        {
            bool ret = false;

            IList<MethodInfo> getInstanceMethodsFound = AppDomainHelper.GetMethodsMarkedWithAttribute(t, autoRegisterGetInstanceFuncAttributeType, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (getInstanceMethodsFound.Any())
            {
                var q = from mi in getInstanceMethodsFound
                        where !mi.IsStatic
                        select mi;
                if (q.Any())
                {
                    throw new InvalidOperationException(String.Format("Type: {0} marked with {1} but has a nonstatic method marked with {2}!", t.FullName, autoRegisterableTypeAttribute.GetType().Name, autoRegisterGetInstanceFuncAttributeType.Name)); //LOCSTR
                }

                Type genericFuncType = typeof(Func<>);
                foreach (var mi in getInstanceMethodsFound)
                {
                    Type specificFuncType = genericFuncType.MakeGenericType(mi.ReturnType);
                    Func<object> d = (Func<object>)Delegate.CreateDelegate(specificFuncType, null, mi);

                    InternalRegister(mi.ReturnType, d, autoRegisterableTypeAttribute.OtherTypesWeAreDependingOn);
                }

                ret = true;
            }


            return ret;
        }


        private RegisteredTypeContainer GetRegisteredTypeContainer(Type t)
        {
            RegisteredTypeContainer ret = null;

            bool gotLock = false;
            try
            {
                syncRoot.Enter(ref gotLock);

                registeredTypes.TryGetValue(t, out ret);
            }
            finally
            {
                if (gotLock)
                {
                    syncRoot.Exit();
                }
            }

            return ret;
        }

        /// <summary>
        /// Retrieves an instance of given type by executing func given at registration.
        /// </summary>
        /// <param name="tinterface"></param>
        /// <returns></returns>
        protected object GetInstance(Type interfaceType)
        {
            object instance = null;

            RegisteredTypeContainer c = GetRegisteredTypeContainer(interfaceType);

            if (c != null)
            {
                instance = c.GetInstance();
            }
            else
            {
                // not found registered ones?
                if ((!autoRegisterTypesInternalInProgress) &&
                    ((!typesWithCustomRegistrationFuncEnabled && !autoRegistrationDoneOnceWithCustomRegistrationFuncDisabled) ||
                    (typesWithCustomRegistrationFuncEnabled && !autoRegistrationDoneOnceWithCustomRegistrationFuncEnabled)))
                {
                    // fallback
                    AutoRegisterTypes(typesWithCustomRegistrationFuncEnabled);
                    instance = GetInstance(interfaceType);
                }
                else
                {
                    throw new InvalidOperationException(String.Format("Type '{0}' not registered!", interfaceType)); //LOCSTR
                }
            }

            return instance;
        }

        public virtual bool IsAvailable(Type interfaceType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException("interfaceType");
            }

            bool ret = false;

            var c = GetRegisteredTypeContainer(interfaceType);
            if (c != null)
            {
                ret = c.IsAvailable();
            }

            return ret;
        }

        public virtual bool IsAvailable(List<Type> interfaceTypeList)
        {
            if (interfaceTypeList == null)
            {
                throw new ArgumentNullException("interfaceTypeList");
            }

            bool ret = true;

            foreach (var t in interfaceTypeList)
            {
                if (!IsAvailable(t))
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }



        /// <summary>
        /// Tells whether given interface type is registered or not.
        /// </summary>
        public virtual bool IsRegistered(Type interfaceType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException("interfaceType");
            }

            bool ret = false;

            bool gotLock = false;
            try
            {
                syncRoot.Enter(ref gotLock);

                ret = registeredTypes.ContainsKey(interfaceType);
            }
            finally
            {
                if (gotLock)
                {
                    syncRoot.Exit();
                }
            }

            if (!ret)
            {
                if ((!autoRegisterTypesInternalInProgress) &&
                    ((!typesWithCustomRegistrationFuncEnabled && !autoRegistrationDoneOnceWithCustomRegistrationFuncDisabled) ||
                    (typesWithCustomRegistrationFuncEnabled && !autoRegistrationDoneOnceWithCustomRegistrationFuncEnabled)))
                {
                    // fallback
                    AutoRegisterTypes(typesWithCustomRegistrationFuncEnabled);
                    ret = IsRegistered(interfaceType);
                }
            }

            return ret;
        }

        public virtual bool IsRegistered(List<Type> interfaceTypeList)
        {
            if (interfaceTypeList == null)
            {
                throw new ArgumentNullException("interfaceTypeList");
            }

            bool ret = true;

            foreach (var t in interfaceTypeList)
            {
                if (!IsRegistered(t))
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="itemExecuterAction">runOnTypeAvailableOrRegisteredActionExecuterAction(isRunningSynchronouslyRequested, itemName, itemAction)</param>
        public DIContainer(Func<Assembly,bool> assemblySelector, Action<bool, string, Action> runOnTypeAvailableOrRegisteredActionExecuterAction)
		{
            if (assemblySelector == null)
            {
                throw new ArgumentNullException("assemblySelector");
            }
            if (runOnTypeAvailableOrRegisteredActionExecuterAction == null)
            {
                throw new ArgumentNullException("runOnTypeAvailableOrRegisteredActionExecuterAction");
            }
            this.assemblySelector = assemblySelector;
            this.runOnTypeAvailableOrRegisteredActionExecuterAction = runOnTypeAvailableOrRegisteredActionExecuterAction;

            ownAppDomain = AppDomain.CurrentDomain;
            AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);
		}

        public event EventHandler<DIContainerAssemblyLoadedHookEventArgs> AssemblyLoadedHook;

        private bool OnDIContainerAssemblyLoadedHook(Assembly loadedAssembly)
        {
            bool ret = true;

            if (AssemblyLoadedHook != null)
            {
                var ea = new DIContainerAssemblyLoadedHookEventArgs(loadedAssembly);
                AssemblyLoadedHook(this, ea);
                ret = ea.AutoRegisterTypesInLoadedAssembly;
            }

            return ret;
        }

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (sender == ownAppDomain)
            {
                try
                {
                    if (!args.LoadedAssembly.ReflectionOnly)
                    {
                        if (OnDIContainerAssemblyLoadedHook(args.LoadedAssembly))
                        {
                            if (assemblySelector(args.LoadedAssembly))
                            {
                                IList<Type> typesFound = AppDomainHelper.GetTypesMarkedWithAttribute(args.LoadedAssembly, typeof(DIContainerAutoRegisterableTypeAttribute), (a, t) => true);
                                AutoRegisterTypesInternal(typesWithCustomRegistrationFuncEnabled, typesFound);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("DIContainerAssemblyLoadedHook disallowed autoregistration of types in assembly: ", args.LoadedAssembly.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot auto register types in assembly {0}: {1}", args.LoadedAssembly.FullName, ex); //LOCSTR
                }
            }
        }


        public event EventHandler NewTypesRegistered;
        private void OnNewTypesRegistered()
        {
            // raise event
            if (NewTypesRegistered != null)
            {
                NewTypesRegistered(this, EventArgs.Empty);
            }
        }


        public virtual IDIContainerLazyBoundObject<TInterface> GetLazyBoundInstance<TInterface>()
            where TInterface : class
        {
            IDIContainerLazyBoundObject<TInterface> ret = new LazyBoundObject<TInterface>(this, typeof(TInterface), GetInstance);
            return ret;
        }

        public virtual IDIContainerLazyBoundObject<object> GetLazyBoundInstance(Type interfaceType)
        {
            IDIContainerLazyBoundObject<object> ret = new LazyBoundObject<object>(this, interfaceType, GetInstance);
            return ret;
        }


        public virtual void RunOnTypeAvailable(Type interfaceType, Action action)
        {
            RunOnTypeAvailable(interfaceType, action, false);
        }

        public virtual void RunOnTypeAvailable(Type interfaceType, Action action, bool isAsyncRunForced)
        {
            RunOnTypesAvailable(new List<Type>() { interfaceType }, action, isAsyncRunForced);
        }

        public virtual void RunOnTypesAvailable(List<Type> interfaceTypeList, Action action)
        {
            RunOnTypesAvailable(interfaceTypeList, action, false);
        }

        public virtual void RunOnTypesAvailable(List<Type> interfaceTypeList, Action action, bool isAsyncRunForced)
        {
            runOnTypeRegisteredOrAvailableQueue.AddWorkItem(true, interfaceTypeList, action, isAsyncRunForced);
        }

        public virtual void RunOnTypeRegistered(Type interfaceType, Action action)
        {
            RunOnTypeRegistered(interfaceType, action, false);
        }

        public virtual void RunOnTypeRegistered(Type interfaceType, Action action, bool isAsyncRunForced)
        {
            RunOnTypesRegistered(new List<Type>() { interfaceType }, action);
        }

        public virtual void RunOnTypesRegistered(List<Type> interfaceTypeList, Action action)
        {
            RunOnTypesRegistered(interfaceTypeList, action, false);
        }

        public virtual void RunOnTypesRegistered(List<Type> interfaceTypeList, Action action, bool isAsyncRunForced)
        {
            runOnTypeRegisteredOrAvailableQueue.AddWorkItem(false, interfaceTypeList, action, isAsyncRunForced);
        }

    }

}
