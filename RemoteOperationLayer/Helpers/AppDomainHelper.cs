using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization;

namespace ArdinHelpers
{
    public static class AppDomainHelper
    {
        /// <summary>
        /// Retrieves loaded assemblies.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Assembly> GetAssembliesFromAppDomain()
        {
            IEnumerable<Assembly> ret = null;
            
            ret = AppDomain.CurrentDomain.GetAssemblies();

            return ret;
        }

        /// <summary>
        /// Retrieves given assembly if it is loaded.
        /// Lookup using partial matching agains given assembly name 
        /// (e.g. Version may not be set to match any version).
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        public static Assembly GetAssemblyFromAppDomain(AssemblyName assemblyName)
        {
            Assembly ret = (from a in GetAssembliesFromAppDomain()
                            let an = a.GetName()
                            where 
                                assemblyName.Name == an.Name &&
                                ((assemblyName.Version == null) || (assemblyName.Version == an.Version)) &&
                                ((assemblyName.CultureInfo == null) || (assemblyName.CultureInfo.NativeName == an.CultureInfo.NativeName))
                            select a).FirstOrDefault();
            return ret;
        }

        /// <summary>
        /// Scan appdomain's assemblies for types marked with given attribute.
        /// </summary>
        /// <param name="typeAttributeType"></param>
        /// <param name="methodAttributeType"></param>
        /// <param name="bindingFlags"></param>
        /// <returns></returns>
        public static IList<Type> GetTypesMarkedWithAttribute(Type typeAttributeType, Func<Assembly, bool> assemblySelector, Func<Attribute,Type,bool> typeSelector)
        {
            if (typeAttributeType == null)
            {
                throw new ArgumentNullException("typeAttributeType");
            }
            if (assemblySelector == null)
            {
                throw new ArgumentNullException("assemblySelector");
            }
            if (typeSelector == null)
            {
                throw new ArgumentNullException("typeSelector");
            }

            List<Type> ret = new List<Type>();
            foreach (Assembly a in GetAssembliesFromAppDomain())
            {
                if (assemblySelector(a))
                {
                    IList<Type> typesFoundInAssembly = GetTypesMarkedWithAttribute(a, typeAttributeType, typeSelector);
                    if (typesFoundInAssembly != null)
                    {
                        ret.AddRange(typesFoundInAssembly);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Scan given assembly for types marked with given attribute.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="typeAttributeType"></param>
        /// <returns></returns>
        public static IList<Type> GetTypesMarkedWithAttribute(Assembly a, Type typeAttributeType, Func<Attribute, Type, bool> typeSelector)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }
            if (typeAttributeType == null)
            {
                throw new ArgumentNullException("typeAttributeType");
            }

            IList<Type> ret;

            if (!a.IsDynamic)
            {
                ret = (from t in TypeHelper.GetTypes(a)
                       where (t.GetCustomAttributes(typeAttributeType, false).Length == 1) && typeSelector((Attribute)t.GetCustomAttributes(typeAttributeType, false).First(), t)
                       select t).ToList();
            }
            else
            {
                ret = Type.EmptyTypes;
            }

            return ret;
        }


        public static IList<MethodInfo> GetMethodsMarkedWithAttribute(Type typeToInspect, Type methodAttributeType, BindingFlags bindingFlags)
        {
            if (typeToInspect == null)
            {
                throw new ArgumentNullException("typeToInspect");
            }
            if (methodAttributeType == null)
            {
                throw new ArgumentNullException("methodAttributeType");
            }

            IList<MethodInfo> ret = (from mi in typeToInspect.GetMethods(bindingFlags)
                                     where mi.GetCustomAttributes(methodAttributeType, false).Length == 1
                                     select mi).ToList();

            return ret;
        }

        public static IList<PropertyInfo> GetPropertiesMarkedWithAttribute(Type typeToInspect, Type propertyAttributeType, BindingFlags bindingFlags)
        {
            if (typeToInspect == null)
            {
                throw new ArgumentNullException("typeToInspect");
            }
            if (propertyAttributeType == null)
            {
                throw new ArgumentNullException("propertyAttributeType");
            }

            IList<PropertyInfo> ret = (from mi in typeToInspect.GetProperties(bindingFlags)
                                     where mi.GetCustomAttributes(propertyAttributeType, false).Length == 1
                                     select mi).ToList();

            return ret;
        }


        public static void PreloadAssemblies(string basePath, bool diveIntoSubdirs, Func<string,bool> selectorFunc)
        {
            if (String.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException("basePath");
            }
            if (selectorFunc == null)
            {
                throw new ArgumentNullException("selectorFunc");
            }

            var assembliesToLoad = from fn in Directory.GetFiles(basePath, "*", (diveIntoSubdirs) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                    where selectorFunc(fn)
                                    select fn;
            foreach (string fn in assembliesToLoad)
            {
                var loadedAssemblies = GetAssembliesFromAppDomain().Where(a => !a.IsDynamic).Select(a => a.Location);
                if (!loadedAssemblies.Contains(fn))
                {
                    LoadAssembly(fn);
                }
            }
        }

        public static Assembly LoadAssembly(string assemblyNameOrFile)
        {
            if (String.IsNullOrWhiteSpace(assemblyNameOrFile))
            {
                throw new ArgumentNullException("assemblyNameOrFile");
            }

            Assembly ret = null;

            AssemblyName an = null;
            if (File.Exists(assemblyNameOrFile))
            {
                an = AssemblyName.GetAssemblyName(assemblyNameOrFile);
            }
            else
            {
                an = new AssemblyName(assemblyNameOrFile);
            }

            ret = LoadAssembly(an);

            return ret;
        }

        public static Assembly LoadAssembly(AssemblyName an)
        {
            if (an == null)
            {
                throw new ArgumentNullException("an");
            }

            Assembly ret = GetAssemblyFromAppDomain(an);
            if (ret == null)
            {
                ret = Assembly.Load(an);
            }

            return ret;
        }
    

        public static void RunInTemporaryAppDomain(Action testBody)
        {
            RunInTemporaryAppDomain((Delegate)testBody);
        }

        public static void RunInTemporaryAppDomain(Action<object> testBody, object param)
        {
            RunInTemporaryAppDomain((Delegate)testBody, param);
        }

        public static void RunInTemporaryAppDomain(Delegate testBody, params object[] args)
        {
            AppDomain domain = null;
            try
            {
                // create new appdomain with current's setupinformation, otherwise assembly binding redirects will not work!
                domain = AppDomain.CreateDomain(String.Format("{0}'s child: {1}", AppDomain.CurrentDomain.FriendlyName, Guid.NewGuid()), null, AppDomain.CurrentDomain.SetupInformation);
                TemporaryAppDomainProxy proxy = domain.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(TemporaryAppDomainProxy)).FullName, typeof(TemporaryAppDomainProxy).ToString()) as TemporaryAppDomainProxy;
                proxy.RunAction(testBody, args);
            }
            catch (TestAppDomainWappedException ex)
            {
                ex.ThrowOriginalException();
            }
            finally
            {
                if (domain != null)
                {
                    try
                    {
                        AppDomain.Unload(domain);
                    }
                    catch (CannotUnloadAppDomainException)
                    {
                        // no-op
                    }
                }
            }
        }

        private class TemporaryAppDomainProxy : MarshalByRefObject
        {
            public void RunAction(Delegate testBody, params object[] args)
            {
                try
                {
                    if (testBody == null)
                    {
                        throw new ArgumentNullException("testBody");
                    }

                    testBody.DynamicInvoke(args);
                }
                catch (Exception ex)
                {
                    throw new TestAppDomainWappedException(ex.GetType(), ex.ToString());
                }
            }
        }

        [Serializable]
        private class TestAppDomainWappedException : Exception, ISerializable
        {
            public Type OriginalType
            {
                get;
                set;
            }


            // The special constructor is used to deserialize values. 
            public TestAppDomainWappedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
                OriginalType = (Type)info.GetValue("OriginalType", typeof(Type));
            }

            public TestAppDomainWappedException(Type originalType, string message)
                : base(message)
            {
                OriginalType = originalType;
            }

            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);

                info.AddValue("OriginalType", OriginalType);
            }

            public void ThrowOriginalException()
            {
                Exception ex = this;
                try
                {
                    if (TypeHelper.IsSubclassOf(OriginalType, typeof(Exception)))
                    {
                        ConstructorInfo[] cis = OriginalType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        ConstructorInfo pci = cis.FirstOrDefault(ci => ci.GetParameters().Length == 1 && ci.GetParameters().First().ParameterType == typeof(string));
                        if (pci == null)
                        {
                            ex = (Exception)Activator.CreateInstance(OriginalType, true);
                        }
                        else
                        {
                            ex = (Exception)pci.Invoke(new object[] { Message });
                        }
                    }
                }
                catch
                {
                    // nothing can be done
                }
                throw ex;
            }
        }

    }
}
