using System.Collections.Generic;
using UnityEngine;

namespace Numeria.Game
{
    /// <summary>
    /// 精灵加载:自绘资产按路径加载;Cainos 雪碧图按切片名加载(带缓存)。
    /// </summary>
    public static class SpriteLib
    {
        private static readonly Dictionary<string, Dictionary<string, Sprite>> SheetCache =
            new Dictionary<string, Dictionary<string, Sprite>>();

        /// <summary>加载单图精灵(Resources 路径,不带扩展名)。</summary>
        public static Sprite One(string path) => Resources.Load<Sprite>(path);

        /// <summary>
        /// 数灵高清大图(generated/{id}_large_icon),没有就回退到 16px 小立绘。
        /// </summary>
        public static Sprite LargeIcon(string id)
        {
            var large = Resources.Load<Sprite>($"generated/{id}_large_icon");
            return large != null ? large : One($"Art/Sprites/{id}");
        }

        /// <summary>地图/队伍小图优先 16px 素材，缺失时使用同角色高清图并由调用方归一化尺寸。</summary>
        public static Sprite MapSprite(string id)
        {
            var small = One($"Art/Sprites/{id}");
            return small != null ? small : LargeIcon(id);
        }

        /// <summary>世界地图主角固定为 Lucas；素材缺失时由调用方回退到出战数灵。</summary>
        public static Sprite LucasExplorer() => One("generated/Heroes/lucas_explorer");

        private const string PackRoot = "generated/NUMERIA_Unity_Battle_Assets/";

        /// <summary>NUMERIA 战斗素材包内的资源(相对包根路径)。</summary>
        public static Sprite Pack(string relativePath) => Resources.Load<Sprite>(PackRoot + relativePath);

        private static string CapId(string id) => char.ToUpperInvariant(id[0]) + id.Substring(1);

        /// <summary>
        /// 玩家战斗立绘:素材包背面图 → 旧抠图背面 → 高清正面 → 像素图。
        /// </summary>
        public static Sprite PlayerBattleSprite(string id)
        {
            var packBack = Pack($"Characters/{CapId(id)}_Battle_Back");
            if (packBack != null) return packBack;
            var back = Resources.Load<Sprite>($"generated/{id}_battle_back");
            return back != null ? back : LargeIcon(id);
        }

        /// <summary>敌方战斗立绘:素材包正面图 → 高清大图 → 像素图。</summary>
        public static Sprite EnemyBattleSprite(string id)
        {
            var packFront = Pack($"Characters/{CapId(id)}_Battle_Front");
            return packFront != null ? packFront : LargeIcon(id);
        }

        /// <summary>按切片名从 Cainos 雪碧图取子精灵,如 Cainos("TX Props", "TX Props Chest")。</summary>
        public static Sprite Cainos(string sheet, string spriteName)
        {
            if (!SheetCache.TryGetValue(sheet, out var dict))
            {
                dict = new Dictionary<string, Sprite>();
                foreach (var s in Resources.LoadAll<Sprite>("Cainos/Texture/" + sheet))
                    dict[s.name] = s;
                SheetCache[sheet] = dict;
            }
            return dict.TryGetValue(spriteName, out var sprite) ? sprite : null;
        }
    }
}
