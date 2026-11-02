using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace HoldIt.Tests
{
    /// <summary>
    /// The shipped actions asset has the actions the game code finds by name, with the types its logic relies on. The other
    /// input tests build their own asset, so they cannot see a change made here.
    /// </summary>
    public class ControlsAssetTests
    {
        private const string PATH = "Assets/Game/Input/Controls.inputactions";

        private InputActionAsset _actions;

        [SetUp]
        public void LoadAsset() => _actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(PATH);

        [TestCase("Gameplay/Point", InputActionType.PassThrough, "<Pointer>/position")]
        [TestCase("Gameplay/Press", InputActionType.Button, "<Pointer>/press")]
        public void GameplayAction_HasItsTypeAndBinding(string name, InputActionType type, string path)
        {
            var action = _actions.FindAction(name, throwIfNotFound: true);

            // Point is pass-through: a Value action picks the "most actuated" of several pointers, meaningless for positions.
            Assert.AreEqual(type, action.type);
            Assert.AreEqual(path, action.bindings[0].path);
        }

        [Test]
        public void BackAction_IsTheUiMapsCancelButton() =>
            Assert.AreEqual(InputActionType.Button, _actions.FindAction("UI/Cancel", throwIfNotFound: true).type);
    }
}
