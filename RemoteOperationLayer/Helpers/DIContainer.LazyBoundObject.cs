using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinDIContainer
{
    public partial class DIContainer
    {
        public class LazyBoundObject<T> : IDIContainerLazyBoundObject<T>
            where T : class
        {
            private IDIContainer diContainer = null;
            private Func<Type, object> getInstanceFunc = null;
            private Type type = null;

            public virtual bool IsAvailable
            {
                get
                {
                    bool ret = diContainer.IsAvailable(typeof(T));
                    return ret;
                }
            }

            private T value = null;
            public virtual T Value
            {
                get
                {
                    if (value == null)
                    {
                        lock (this)
                        {
                            if (value == null)
                            {
                                value = (T)getInstanceFunc(type);
                            }
                        }
                    }
                    return value;
                }
            }

            internal LazyBoundObject(IDIContainer diContainer, Type type, Func<Type, object> getInstanceFunc)
            {
                if (diContainer == null)
                {
                    throw new ArgumentNullException("diContainer");
                }
                if (type == null)
                {
                    throw new ArgumentNullException("type");
                }
                if (getInstanceFunc == null)
                {
                    throw new ArgumentNullException("getInstanceFunc");
                }

                this.diContainer = diContainer;
                this.type = type;
                this.getInstanceFunc = getInstanceFunc;
            }
        }
    }
}
