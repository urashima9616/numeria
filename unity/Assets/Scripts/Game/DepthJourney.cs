using System;
using System.Linq;
using Numeria.Core;

namespace Numeria.Game
{
    /// <summary>Optional layers never rewrite a parent map or its stable treasure IDs.</summary>
    public static class DepthJourney
    {
        public static bool Ready(MapDef def, Progress p) =>
            def.Discoveries.All(d => p.CollectedDiscoveries.Contains(d.Id));

        public static int RiverX(int index,int biome,int y)
        {
            int[] bend={0,0,1,1,2,2,1,1,0,0,-1,-1};
            return 9+index*9+bend[(y/2+biome+index*3)%bend.Length];
        }
        public static (int x, int y) Gate(int index, int biome)
        {
            int y=5+(biome*3+index*7)%14;return (RiverX(index,biome,y),y);
        }

        public static void Apply(GridMap map, MapDef def, Progress p)
        {
            if (!def.IsDepth) return;
            int biome = Array.FindIndex(DepthCatalog.Worlds, d => d.Id == def.Id);
            for (int i = 0; i < 3; i++)
            {
                var gate = Gate(i, biome);
                map.SetTile(gate.x, gate.y,
                    p.CollectedDiscoveries.Contains(def.Discoveries[i].Id) ? Tile.Bridge : Tile.Cliff);
            }
        }

        public static (int x, int y)? Entrance(MapDef def)
        {
            if (def.IsDepth) return (4,20);
            if (DepthCatalog.ForParent(def.Id) == null) return null;
            var map = GridMap.Parse(def.Rows);
            // A reachable free tile close to spawn, excluding all authored interactions.
            for (int distance = 2; distance < 12; distance++)
                for (int y = 1; y < map.Height - 1; y++)
                    for (int x = 1; x < map.Width - 1; x++)
                    {
                        if (Math.Abs(x-map.Spawn.x)+Math.Abs(y-map.Spawn.y) != distance) continue;
                        var t = map.At(x,y);
                        if ((t != Tile.Grass && t != Tile.Path) || def.DiscoveryAt(x,y) != null || def.MerchantAt(x,y)) continue;
                        if (map.FindPath(map.Spawn,(x,y)).Count > 0) return (x,y);
                    }
            return null;
        }
    }

    public static partial class Maps
    {
        public static MapDef[] AllPlayable() => All().Concat(DepthCatalog.Worlds.Select(Expansion)).ToArray();

        public static MapDef Expansion(DepthWorld world)
        {
            var parent = Get(world.ParentId);
            int biome = Array.IndexOf(DepthCatalog.Worlds, world);
            const int width = 38, height = 24;
            var cells = new char[height][];
            for (int y = 0; y < height; y++)
            {
                cells[y] = new char[width];
                for (int x = 0; x < width; x++)
                {
                    bool edge = x == 0 || y == 0 || x == width-1 || y == height-1;
                    bool patch = x % 9 >= 4 && x % 9 <= 6 && (y + biome * 2) % 9 >= 3 && (y + biome * 2) % 9 <= 5;
                    cells[y][x] = edge ? '#' : patch ? 'b' : '.';
                }
            }
            var discoveries = new DiscoveryDef[3];
            for (int i=0;i<3;i++)
            {
                var gate = DepthJourney.Gate(i,biome);
                for (int y=1;y<height-1;y++) cells[y][DepthJourney.RiverX(i,biome,y)] = '~';
                cells[gate.y][gate.x] = '#';
                cells[gate.y][gate.x-2] = '=';
                cells[3+(biome+i*5)%17][4+i*9] = 'C';
                discoveries[i] = D(world.Id+"-mechanism-"+i, world.Mechanic+" "+(i+1),gate.x-2,gate.y,20+parent.Tier*5);
                cells[2+(biome+i*3)%4][4+i*9] = 'L';
            }
            cells[20][3]='S';cells[20][4]='=';cells[4][34]='P';
            // Carve an actual journey between crossings, then place clustered scenery around it.
            // The grid is authoritative: props never conceal an unmarked blocking tile.
            for(int zone=0;zone<4;zone++)
            {
                int center=4+zone*9;
                int fromY=zone==0?20:DepthJourney.Gate(zone-1,biome).y;
                int toY=zone==3?4:DepthJourney.Gate(zone,biome).y;
                void Path(int x,int y){if(cells[y][x]=='.'||cells[y][x]=='b')cells[y][x]='=';}
                for(int y=Math.Min(fromY,toY);y<=Math.Max(fromY,toY);y++)Path(center,y);
                int fromX=zone==0?3:DepthJourney.Gate(zone-1,biome).x+1;
                int toX=zone==3?34:DepthJourney.Gate(zone,biome).x-1;
                for(int x=Math.Min(fromX,center);x<=Math.Max(fromX,center);x++)Path(x,fromY);
                for(int x=Math.Min(toX,center);x<=Math.Max(toX,center);x++)Path(x,toY);
                for(int cluster=0;cluster<3;cluster++)
                {
                    int cy=3+(biome*3+zone*5+cluster*7)%18;
                    int cx=center+(cluster%2==0?-1:1);
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int x=cx+dx,y=cy+dy;if(x<1||x>=width-1||y<1||y>=height-1)continue;
                        if(Math.Abs(dx)+Math.Abs(dy)>1||cells[y][x]!='.')continue;
                        if(discoveries.Any(d=>Math.Abs(d.X-x)+Math.Abs(d.Y-y)<3))continue;
                        if(Math.Abs(x-4)+Math.Abs(y-20)<4||Math.Abs(x-34)+Math.Abs(y-4)<4)continue;
                        cells[y][x]=world.ParentId=="forest"?'T':'#';
                    }
                }
            }
            return new MapDef
            {
                Id=world.Id,ParentWorld=world.ParentId,DisplayName=world.Name,
                WelcomeLine="Welcome to "+world.Name+"! "+world.Objective,
                Rows=cells.Select(row=>new string(row)).ToArray(),Tier=parent.Tier,
                WildMaxLevel=world.Level-2,Theme=parent.Theme,CameraBg=parent.CameraBg,BattleBg=parent.BattleBg,
                Encounters=world.Species.Where(s=>!s.Giant).Select(s=>E(s.Id,10,Math.Max(1,world.Level-8),world.Level-2)).ToArray(),
                BossSpeciesId=world.Giant.Id,BossDisplayName="Giant "+world.Giant.Name,BossMinLevel=world.Level,
                BossLine=world.Giant.Name+" is ready for your challenge!",
                GuardianName=world.Giant.Name,GuardianSpriteResource="generated/"+world.Giant.Id+"_large_icon",
                GuardianChallengeLines=new[]{"You restored my home. Now show me your team's courage!"},
                GateClearLine=world.Giant.Name+" is now your friend!",GuardianVictoryLine="Our adventure begins together!",
                GateCleared=p=>p.DefeatedGiants.Contains(world.Giant.Id),ClearGate=p=>p.AwardGiant(world.Giant.Id),
                PortalTargetMap=world.ParentId,NextName=parent.DisplayName,
                Discoveries=discoveries,ChestRewards=new System.Collections.Generic.Dictionary<string,ChestRewardDef>()
            };
        }
    }
}
