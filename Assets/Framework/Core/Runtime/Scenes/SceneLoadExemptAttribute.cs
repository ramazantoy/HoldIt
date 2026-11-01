using System;

namespace AF.Core
{
    /// <summary>
    /// Allows code to load or unload scenes through Unity's <c>SceneManager</c> itself, bypassing <see cref="ISceneLoader"/>.
    /// Validation reports such calls anywhere else. On a type it covers every member, nested types and lambdas included.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    public sealed class SceneLoadExemptAttribute : Attribute
    {
        public SceneLoadExemptAttribute(string reason) => Reason = reason;

        public string Reason { get; }
    }
}
