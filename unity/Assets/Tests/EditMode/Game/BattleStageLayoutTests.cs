using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Numeria.Core;

namespace Numeria.Game.Tests
{
    public class BattleStageLayoutTests
    {
        [Test]
        public void NormalAndMegaVisibleMeshesStayOnTheFootBaseline()
        {
            var root = new GameObject("GroundedImageTest", typeof(RectTransform));
            try
            {
                foreach (var id in new[] { "addmander", "mirrorwyrm" })
                {
                    var image = GroundedBattleImage.Create(root.transform, id, SpriteLib.PlayerBattleSprite(id));
                    image.rectTransform.sizeDelta = new Vector2(440, 440);
                    foreach (var sprite in new[] { SpriteLib.PlayerBattleSprite(id), SpriteLib.MegaBattleSprite(id) })
                    {
                        Assert.NotNull(sprite, id);
                        image.sprite = sprite;
                        using (var helper = new VertexHelper())
                        {
                            typeof(GroundedBattleImage).GetMethod("OnPopulateMesh",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
                                .Invoke(image, new object[] { helper });
                            Assert.Greater(helper.currentVertCount, 0);
                            float bottom = float.PositiveInfinity;
                            var vertex = new UIVertex();
                            for (int i = 0; i < helper.currentVertCount; i++)
                            { helper.PopulateUIVertex(ref vertex, i); bottom = Mathf.Min(bottom, vertex.position.y); }
                            Assert.AreEqual(image.rectTransform.rect.yMin, bottom, .1f, id);
                        }
                    }
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(1920, 1080)]
        [TestCase(1440, 1080)]
        [TestCase(2048, 992)]
        public void EveryBackdropCoversViewportWithoutStretchAndActorsClearUi(int width, int height)
        {
            bool spoken = Voice.Enabled;
            Voice.Enabled = false;
            try
            {
                foreach (var map in Maps.All())
                {
                    var host = new GameObject("BattleLayoutTest");
                    var rt = new RenderTexture(width, height, 24);
                    try
                    {
                        var camera = new GameObject("TestCamera").AddComponent<Camera>();
                        camera.transform.SetParent(host.transform, false);
                        camera.orthographic = true; camera.targetTexture = rt; camera.aspect = width / (float)height;
                        var battle = host.AddComponent<BattleController>();
                        battle.Init(GameData.CreateWild("pebblit", 5, new Rng(7)), new Progress(), map.Tier, map.BattleBg, _ => { });
                        foreach (var canvas in host.GetComponentsInChildren<Canvas>())
                        {
                            canvas.renderMode = RenderMode.ScreenSpaceCamera;
                            canvas.worldCamera = camera; canvas.planeDistance = 1;
                        }
                        Canvas.ForceUpdateCanvases();
                        var stage = host.GetComponentInChildren<BattleStageLayout>();
                        stage.Apply();
                        var root = (RectTransform)stage.transform;
                        var bg = root.Find("Background").GetComponent<Image>();
                        Assert.GreaterOrEqual(bg.rectTransform.rect.width + .1f, root.rect.width, map.Id);
                        Assert.GreaterOrEqual(bg.rectTransform.rect.height + .1f, root.rect.height, map.Id);
                        Assert.AreEqual(bg.sprite.rect.width / bg.sprite.rect.height,
                            bg.rectTransform.rect.width / bg.rectTransform.rect.height, .001f, map.Id);
                        var enemy = (RectTransform)root.Find("EnemySprite");
                        var player = (RectTransform)root.Find("PlayerSprite");
                        var plate = (RectTransform)root.Find("PlayerPlate");
                        Rect Bounds(RectTransform t)
                        {
                            var corners = new Vector3[4]; t.GetWorldCorners(corners);
                            var a = root.InverseTransformPoint(corners[0]); var b = root.InverseTransformPoint(corners[2]);
                            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
                        }
                        Assert.False(Bounds(enemy).Overlaps(Bounds(plate)), map.Id + " enemy overlaps status");
                        Assert.Greater(Bounds(player).yMin, -root.rect.height * .5f + 312, map.Id + " player overlaps dock");
                        Assert.Greater(Bounds(enemy).yMin, -root.rect.height * .5f + 312, map.Id + " enemy overlaps dock");
                        Assert.NotNull(root.Find("PlayerContactShadow"));
                        Assert.NotNull(root.Find("EnemyContactShadow"));
                        // A layout resize pass is idempotent; ordinary renders must not interrupt spell motion.
                        var position = enemy.anchoredPosition;
                        stage.Apply(); Assert.AreEqual(position, enemy.anchoredPosition);
                    }
                    finally { Object.DestroyImmediate(host); rt.Release(); Object.DestroyImmediate(rt); }
                }
            }
            finally { Voice.Enabled = spoken; }
        }

        [Test]
        public void ForestAndUndergroundSelectTheNewBiomeArt()
        {
            foreach (var id in new[] { "forest", "underground" })
            {
                var map = Maps.Get(id);
                StringAssert.Contains("/Painted/", map.BattleBg);
                var sprite = Resources.Load<Sprite>(map.BattleBg);
                Assert.NotNull(sprite);
                Assert.AreEqual(FilterMode.Bilinear, sprite.texture.filterMode);
                Assert.GreaterOrEqual(sprite.texture.width, 1600);
            }
        }
    }
}
