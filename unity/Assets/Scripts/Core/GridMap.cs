using System.Collections.Generic;

namespace Numeria.Core
{
    public enum Tile
    {
        Grass,
        Tree,
        Bush,
        Chest,
        Portal,
        Water,
        Path,
        Bridge,
        Cliff,
        Landmark,
    }

    /// <summary>
    /// ASCII 网格地图:
    /// '.'=地面, 'T'=主题障碍, 'b'=遭遇草丛, 'C'=宝箱, 'P'=传送门, 'S'=出生点,
    /// '~'=水/云海, '='=道路, 'B'=桥, '#'=悬崖, 'L'=大型地标。
    /// 与像素资产同一哲学:地图就是文本,直接进版本控制。
    /// </summary>
    public class GridMap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public (int x, int y) Spawn { get; private set; }

        private Tile[,] _tiles;

        public static GridMap Parse(string[] rows)
        {
            var map = new GridMap
            {
                Height = rows.Length,
                Width = rows[0].Length,
            };
            map._tiles = new Tile[map.Width, map.Height];
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    char c = rows[y][x];
                    switch (c)
                    {
                        case 'T': map._tiles[x, y] = Tile.Tree; break;
                        case 'b': map._tiles[x, y] = Tile.Bush; break;
                        case 'C': map._tiles[x, y] = Tile.Chest; break;
                        case 'P': map._tiles[x, y] = Tile.Portal; break;
                        case '~': map._tiles[x, y] = Tile.Water; break;
                        case '=': map._tiles[x, y] = Tile.Path; break;
                        case 'B': map._tiles[x, y] = Tile.Bridge; break;
                        case '#': map._tiles[x, y] = Tile.Cliff; break;
                        case 'L': map._tiles[x, y] = Tile.Landmark; break;
                        case 'S':
                            map._tiles[x, y] = Tile.Grass;
                            map.Spawn = (x, y);
                            break;
                        default: map._tiles[x, y] = Tile.Grass; break;
                    }
                }
            return map;
        }

        public Tile At(int x, int y) => _tiles[x, y];

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool Walkable(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            Tile tile = _tiles[x, y];
            return tile != Tile.Tree && tile != Tile.Water &&
                   tile != Tile.Cliff && tile != Tile.Landmark;
        }

        /// <summary>BFS 最短路径(四方向)。返回不含起点的路径;不可达或原地返回空列表。</summary>
        public List<(int x, int y)> FindPath((int x, int y) from, (int x, int y) to)
        {
            var path = new List<(int x, int y)>();
            if (from == to || !Walkable(to.x, to.y)) return path;

            var prev = new Dictionary<(int, int), (int, int)>();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue(from);
            prev[from] = from;
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == to) break;
                for (int i = 0; i < 4; i++)
                {
                    var next = (x: cur.x + dx[i], y: cur.y + dy[i]);
                    if (!Walkable(next.x, next.y) || prev.ContainsKey(next)) continue;
                    prev[next] = cur;
                    queue.Enqueue(next);
                }
            }

            if (!prev.ContainsKey(to)) return path;
            var node = to;
            while (node != from)
            {
                path.Add(node);
                node = prev[node];
            }
            path.Reverse();
            return path;
        }
    }
}
