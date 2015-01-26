using System;
using System.Reflection;
using System.Collections.Generic;
namespace ArdinDIContainer
{
	public interface IDIContainer
	{
        /// <summary>
        /// Register given type with DIContainer.
        /// When the type will be requested with GetInstance the oGetInstanceFunc will be executed 
        /// to retrieve an instance.
        /// E.g. that func can be a ()=>{return new MyClassOfTInterface(); } style lambda to always retrieve a new instance,
        /// or a ()=>{ return MySingletonClassOfTInterface.Instance; } style one for singletons.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="oGetInstanceFunc"></param>
        void Register<TInterface>(Func<TInterface> oGetInstanceFunc, List<Type> otherTypesWeAreDependingOn)
			where TInterface : class;
        void Register<TInterface>(Func<TInterface> oGetInstanceFunc)
            where TInterface : class;
        void Register(Type interfaceType, Func<object> oGetInstanceFunc);
        void Register(Type interfaceType, Func<object> oGetInstanceFunc, List<Type> otherTypesWeAreDependingOn);

        void Unregister<TInterface>()
            where TInterface : class;

        /// <summary>
        /// Scans assemblies in AppDomain for methods tagged with DIContainerAutoRegisterAttribute
        /// and registers them.
        /// </summary>
        void AutoRegisterTypes(bool typesWithCustomRegistrationFuncToo);


        /// <summary>
        /// Retrieves an instance of given type by executing func given at registration.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <returns></returns>
        IDIContainerLazyBoundObject<TInterface> GetLazyBoundInstance<TInterface>() where TInterface : class;

        /// <summary>
        /// Retrieves an instance of given type by executing func given at registration.
        /// </summary>
        /// <param name="tinterface"></param>
        /// <returns></returns>
        IDIContainerLazyBoundObject<object> GetLazyBoundInstance(Type interfaceType);

        /// <summary>
        /// Tells whether given interface type is registered or not.
        /// </summary>
        bool IsRegistered(Type interfaceType);

        /// <summary>
        /// Tells whether all the given interface types are registered or not.
        /// </summary>
        bool IsRegistered(List<Type> interfaceTypeList);

        /// <summary>
        /// tells whether type is instantiable (all types registered which types we are depending on)
        /// </summary>
        bool IsAvailable(Type interfaceType);

        /// <summary>
        /// tells whether type is instantiable (all types registered which types we are depending on)
        /// </summary>
        bool IsAvailable(List<Type> interfaceTypeList);

        /// <summary>
        /// Runs action if all types becomes registered, maybe synchronously now.
        /// </summary>
        void RunOnTypesRegistered(List<Type> interfaceTypeList, Action action);

        /// <summary>
        /// Runs action if all types becomes registered.
        /// </summary>
        void RunOnTypesRegistered(List<Type> interfaceTypeList, Action action, bool isAsyncRunForced);

        /// <summary>
        /// Runs action if type becomes registered, maybe synchronously now.
        /// </summary>
        void RunOnTypeRegistered(Type interfaceType, Action action);

        /// <summary>
        /// Runs action if type becomes registered.
        /// </summary>
        void RunOnTypeRegistered(Type interfaceType, Action action, bool isAsyncRunForced);

        /// <summary>
        /// Runs action if all types becomes available, maybe synchronously now.
        /// </summary>
        void RunOnTypesAvailable(List<Type> interfaceTypeList, Action action);

        /// <summary>
        /// Runs action if all types becomes available.
        /// </summary>
        void RunOnTypesAvailable(List<Type> interfaceTypeList, Action action, bool isAsyncRunForced);

        /// <summary>
        /// Runs action if type becomes available, maybe synchronously now.
        /// </summary>
        void RunOnTypeAvailable(Type interfaceType, Action action);

        /// <summary>
        /// Runs action if type becomes available.
        /// </summary>
        void RunOnTypeAvailable(Type interfaceType, Action action, bool isAsyncRunForced);

        event EventHandler NewTypesRegistered;

        /// <summary>
        /// Set AutoRegisterTypesInLoadedAssembly to false to disallow autoregistration.
        /// </summary>
        event EventHandler<DIContainerAssemblyLoadedHookEventArgs> AssemblyLoadedHook;

	}

}
