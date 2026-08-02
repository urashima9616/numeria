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
