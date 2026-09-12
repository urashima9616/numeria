using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>Keep the visible sprite mesh on its foot baseline, including differently shaped Mega sprites.</summary>
    public sealed class GroundedBattleImage : Image
    {
        public static Image Create(Transform parent, string name, Sprite sprite)
        {
            var image = Ui.Node(parent, name).gameObject.AddComponent<GroundedBattleImage>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.useSpriteMesh = true;
            image.raycastTarget = false;
            return image;
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            base.OnPopulateMesh(helper);
            if (helper.currentVertCount == 0) return;
            var vertex = new UIVertex();
            float bottom = float.PositiveInfinity;
            for (int i = 0; i < helper.currentVertCount; i++)
            { helper.PopulateUIVertex(ref vertex, i); bottom = Mathf.Min(bottom, vertex.position.y); }
            float shift = GetPixelAdjustedRect().yMin - bottom;
            for (int i = 0; i < helper.currentVertCount; i++)
            { helper.PopulateUIVertex(ref vertex, i); vertex.position.y += shift; helper.SetUIVertex(vertex, i); }
        }
    }
}
