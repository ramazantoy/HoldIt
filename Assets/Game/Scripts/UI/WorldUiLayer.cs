using AF.Core;
using HoldIt.Board;
using UnityEngine;
using VContainer;

namespace HoldIt.UI
{
    /// <summary>
    /// The HUD's <see cref="IWorldUiLayer"/> (the WorldUi child of the GameplayHud prefab): a full-screen rect without a
    /// Graphic, so it takes no press, drawn under the rest of the HUD. The HUD is on a Screen Space Overlay canvas, so a
    /// screen point maps straight into this rect.
    /// </summary>
    public sealed class WorldUiLayer : MonoBehaviour, IWorldUiLayer
    {
        [SerializeField, Required] private RectTransform _root;

        private IBoardProjection _projection;

        [Inject]
        public void Construct(IBoardProjection projection) => _projection = projection;

        public RectTransform Root => _root;

        public bool TryPlace(Vector3 world, out Vector2 local)
        {
            local = default;
            return _projection.TryWorldToScreen(world, out var screen) &&
                   RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out local);
        }
    }
}
