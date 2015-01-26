using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinDIContainer
{
    /// <summary>
    /// Mark class with this attribute to indicate class has a method tagged with DIContainerAutoRegisterFuncAttribute.
    /// Dependenies may be listed; registration will occur, but instance will returned only if all dependencies are present.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    public class DIContainerAutoRegisterableTypeAttribute : Attribute
    {
        public List<Type> OtherTypesWeAreDependingOn { get; private set; }

        public DIContainerAutoRegisterableTypeAttribute()
        {
            this.OtherTypesWeAreDependingOn = null;
        }

        public DIContainerAutoRegisterableTypeAttribute(params Type[] otherTypesWeAreDependingOn) : this()
        {
            if ((otherTypesWeAreDependingOn != null) && (otherTypesWeAreDependingOn.Any()))
            {
                this.OtherTypesWeAreDependingOn = new List<Type>();
                this.OtherTypesWeAreDependingOn.AddRange(otherTypesWeAreDependingOn);
            }
        }
    }

    /// <summary>
    /// Mark method with this attribute if it can be a subject of DIContainer auto registration feature.
    /// 
    /// This method will be treated as if DIContainer.Register(ThisMethod) would be called.
    /// 
    /// Method should be casted to Func'TInterface and should be static, but also may be private!
    /// Dont forget to mark containing this method with DIContainerAutoRegisterableTypeAttribute!
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DIContainerAutoRegisterGetInstanceFuncAttribute : Attribute
    {
    }

    /// <summary>
    /// Mark method with this attribute if it can be a subject of DIContainer auto registration feature.
    /// 
    /// DIContainer will simply call this method and implementation may call DIContainer.Register() 
    /// or do anything else.
    /// 
    /// Method should be casted to Action<IDIContainter> and should be static, but also may be private!
    /// Dont forget to mark containing this method with DIContainerAutoRegisterableTypeAttribute!
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DIContainerAutoRegisterCustomRegistrationFuncAttribute : Attribute
    {
    }
}
