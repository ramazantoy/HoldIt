using Object = UnityEngine.Object;

namespace AF.Core
{
    public static class UnityObjectExtensions
    {
        /// <summary>
        /// Null check that also sees destroyed Unity objects. A destroyed component stored in an interface-typed field
        /// still compares <c>!= null</c> as true, because the C# comparison skips Unity's overloaded operator.
        /// </summary>
        public static bool IsAlive(this object target) => target is Object unityObject ? unityObject != null : target != null;
    }
}
