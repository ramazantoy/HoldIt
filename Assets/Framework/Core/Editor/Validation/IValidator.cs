using UnityEngine.SceneManagement;

namespace AF.Core.Editor
{
    /// <summary>
    /// A check run by "AF/Validate All". Implementations are found automatically and need a parameterless constructor;
    /// a game adds its own by implementing this interface in an editor assembly.
    /// </summary>
    public interface IValidator
    {
        /// <summary>Project-wide checks: settings, the root prefab, assets.</summary>
        void ValidateProject(ValidationReport report)
        {
        }

        /// <summary>Called once for every enabled build scene, and for the open scenes when entering Play Mode.</summary>
        void ValidateScene(Scene scene, ValidationReport report)
        {
        }

        /// <summary>
        /// Checks too slow to run every time Play is pressed, such as loading every prefab. Run by "AF/Validate All" and the
        /// build check only.
        /// </summary>
        void ValidateAssets(ValidationReport report)
        {
        }
    }
}
