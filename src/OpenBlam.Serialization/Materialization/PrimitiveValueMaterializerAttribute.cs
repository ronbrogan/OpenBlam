using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBlam.Serialization.Materialization
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PrimitiveValueMaterializerAttribute : Attribute
    {
    }
}
