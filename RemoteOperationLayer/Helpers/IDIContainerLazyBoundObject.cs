using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdinDIContainer
{
    public interface IDIContainerLazyBoundObject<T>
    {
        bool IsAvailable { get; }
        T Value { get; }
    }
}
