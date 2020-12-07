using System;

namespace OpenBlam.Serialization.Materialization
{
    /// <summary>
    /// Used to indicate that a method is to be registered to use when reading primitive values when deserializing.
    /// Intended for synthesizing structs from primitive values, ie. an ID type from a uint or ulong.
    /// More complex types can use <see cref="Layout.InPlaceObjectAttribute"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PrimitiveValueMaterializerAttribute : Attribute
    {
    }
}
