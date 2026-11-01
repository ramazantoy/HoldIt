using System;

namespace AF.Core
{
    /// <summary>
    /// Marks a serialized object reference that must be assigned. "AF/Validate All" reports it when empty.
    /// Only top-level fields of a component are checked.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RequiredAttribute : Attribute
    {
    }
}
