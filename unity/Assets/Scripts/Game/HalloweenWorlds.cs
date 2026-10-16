using System;
using System.Collections.Generic;
using System.Linq;
using Numeria.Core;

namespace Numeria.Game
{
    public static class HalloweenWorlds
    {
        public static readonly string[] Ids = {"spooky_pumpkin_patch", "haunted_house"};
        public static bool Contains(string id) => Array.IndexOf(Ids,id)>=0;
        public static bool Ready(MapDef map,Progress p) => map.Discoveries.All(d=>p.CollectedDiscoveries.Contains(d.Id));
        public static string Objective(MapDef map,Progress p)
        {
            if(map.GateCleared(p))return "Your new friends are waiting. Explore and complete your Field Guide!";
            var next=map.Discoveries.FirstOrDefault(d=>!p.CollectedDiscoveries.Contains(d.Id));
            return next==null?"Meet "+map.BossDisplayName+".":"Restore the "+next.Name+".";
        }
        public static (int x,int y) ReturnCell(string id) => id==Ids[0]?(28,36):(26,39);
        public static int Zone(string id,int x,int y) => y<12?4:x<(id==Ids[0]?26:25)?(y<24?1:0):(y<24?3:2);
        public static EncounterEntry[] Ecology(string id,int zone)
        {
            var families=ExpansionCatalog.Families.Skip(id==Ids[0]?0:15).Take(15).ToArray();
            // Five overlapping habitats; every family has a home and adjacent visitors.
            return Enumerable.Range(0,6).Select(i=>new EncounterEntry {
                SpeciesId=families[(zone*3+i)%15].BaseId,Weight=i<3?20:8,MinimumLevel=76,MaximumLevel=82
            }).ToArray();
        }
        public static void Apply(GridMap grid,MapDef map,Progress p)
        {
            if(!map.IsHalloween)return;
            bool Done(int task)=>p.CollectedDiscoveries.Contains(map.Id+"-task-"+task);
            void Gate(int x,int y,int width,int height,int task,Tile closed)
            {for(int yy=y;yy<y+height;yy++)for(int xx=x;xx<x+width;xx++)grid.SetTile(xx,yy,Done(task)?Tile.Path:closed);}
            // Bonus shortcuts open, but the main circulation loops and return door are always safe.
            if(map.Id==Ids[0])
            {
                for(int x=24;x<=31;x++)grid.SetTile(x,25,Done(0)?Tile.Bridge:Tile.Water);
                Gate(6,14,3,1,1,Tile.Tree);Gate(38,15,3,1,2,Tile.Tree);
            }
            else
            {
                Gate(19,22,2,2,0,Tile.Cliff);
                Gate(20,11,3,1,1,Tile.Cliff);
                Gate(30,6,2,2,2,Tile.Cliff);
            }
        }
        public static MapDef Build(string id)
        {
            bool house=id==Ids[1];int w=house?52:56,h=house?44:40;
            var cells=Enumerable.Range(0,h).Select(y=>Enumerable.Repeat(house?'#':'.',w).ToArray()).ToArray();
            void Rect(int x,int y,int width,int height,char tile)
            {for(int yy=y;yy<y+height;yy++)for(int xx=x;xx<x+width;xx++)cells[yy][xx]=tile;}
            void Path(int x,int y,int xx,int yy)
            {Rect(Math.Min(x,xx),y,Math.Abs(xx-x)+2,2,'=');Rect(xx,Math.Min(y,yy),2,Math.Abs(yy-y)+2,'=');}
            if(house)
            {
                // Distinct rooms, a central foyer and two circulation loops, not a reskinned river strip.
                Rect(4,4,16,12,'.');Rect(32,4,16,12,'.');Rect(22,3,8,9,'.');
                Rect(4,20,15,13,'.');Rect(33,20,15,13,'.');Rect(21,19,10,20,'.');
                Path(10,10,25,8);Path(25,8,40,10);
                Path(10,10,10,26);Path(40,10,40,26);
                Path(10,26,25,26);Path(25,26,40,26);
                Path(10,26,10,37);Path(10,37,25,37);Path(25,37,40,37);Path(40,26,40,37);
                // Encounter rugs remain fully walkable; the foyer is encounter-free.
                Rect(7,6,8,6,'b');Rect(36,6,8,6,'b');Rect(7,22,8,7,'b');Rect(36,22,8,7,'b');
                Rect(23,5,5,4,'b');Rect(24,39,5,3,'=');
                Rect(38,4,5,2,'L');Rect(42,20,4,2,'L');Rect(24,20,4,2,'L');
                Rect(4,21,2,2,'L');Rect(12,4,4,2,'L');
                cells[40][25]='S';
            }
            else
            {
                Rect(0,0,w,2,'T');Rect(0,h-2,w,2,'T');Rect(0,0,2,h,'T');Rect(w-2,0,2,h,'T');
                // Meandering irrigation channel, with two permanent crossings and one earned shortcut.
                for(int y=3;y<35;y++)Rect(26+(y/7)%2,y,3,1,'~');
                Rect(6,6,12,8,'b');Rect(37,5,12,9,'b');
                Rect(5,23,14,10,'b');Rect(37,23,13,10,'b');Rect(20,5,7,5,'b');
                Path(9,18,28,18);Path(28,18,44,18);Path(9,18,9,35);Path(9,35,44,35);Path(44,18,44,35);
                Path(9,8,9,18);Path(9,8,27,8);Path(27,8,44,8);Path(44,8,44,18);
                Rect(25,18,6,2,'B');Rect(25,8,6,2,'B');
                Path(27,35,27,36);cells[36][27]='S';
                // Readable landmark footprints: decoration is never placed on an unmarked walkable cell.
                Rect(4,3,4,3,'L');Rect(40,3,4,2,'T');Rect(19,17,3,3,'L');Rect(22,2,9,2,'T');
                Rect(3,16,3,4,'T');Rect(48,17,3,4,'T');
                Rect(19,34,4,2,'L');Rect(3,10,4,3,'L');Rect(3,21,3,2,'L');Rect(47,34,3,2,'L');
                Rect(3,14,6,1,'T');Rect(11,14,8,1,'T');
                Rect(35,15,9,1,'T');Rect(46,15,5,1,'T');
            }
            var positions=house?new[]{(9,26),(10,12),(40,12)}:new[]{(22,20),(9,16),(43,16)};
            var names=house?new[]{"Mirror Guests","Portrait Places","Midnight Melody"}:new[]{"Growing Channels","Harvest Shares","Lantern Pattern"};
            var discoveries=positions.Select((p,i)=>new DiscoveryDef {Id=id+"-task-"+i,Name=names[i],X=p.Item1,Y=p.Item2,Coins=75}).ToArray();
            foreach(var d in discoveries)cells[d.Y][d.X]='=';
            var treasures=house?new[]{(6,14),(45,14),(6,31),(45,31),(25,10)}:new[]{(17,11),(48,11),(7,30),(47,30),(21,6)};
            foreach(var p in treasures)cells[p.Item2][p.Item1]='C';
            int bossX=house?25:28,bossY=house?5:6;cells[bossY][bossX]='P';
            string boss=house?"chronophantom":"crownkin",bossName=house?"Chronophantom":"Crownkin";
            return new MapDef {
                Id=id,DisplayName=house?"Haunted House":"Spooky Pumpkin Patch",IsHalloween=true,Tier=8,WildMaxLevel=82,
                WelcomeLine=house?"Welcome to Haunted House! Help the friendly ghosts restore their midnight party!":
                    "Welcome to Spooky Pumpkin Patch! Bring water and lantern light back to the harvest!",
                Rows=cells.Select(r=>new string(r)).ToArray(),Theme=house?"veilmoon_lake":"forest",
                CameraBg=house?"#161525":"#302133",BattleBg="generated/Backgrounds/Painted/"+id,
                Encounters=ExpansionCatalog.Families.Skip(house?15:0).Take(15).Select(f=>new EncounterEntry{
                    SpeciesId=f.BaseId,Weight=10,MinimumLevel=76,MaximumLevel=82}).ToArray(),
                BossSpeciesId=boss,BossDisplayName=bossName,BossMinLevel=84,BossLine=bossName+" welcomes your challenge!",
                GuardianName=bossName,GuardianSpriteResource="generated/"+boss+"_large_icon",
                GuardianChallengeLines=new[]{"Thank you for helping my friends. Let us celebrate with a friendly challenge!"},
                GateClearLine=house?"The midnight party is ready!":"The harvest lights are shining!",
                GuardianVictoryLine="Your kindness and courage will always be welcome here.",
                GateCleared=p=>p.ClearedGates.Contains(id),
                ClearGate=p=>{if(!p.ClearedGates.Contains(id)){p.ClearedGates.Add(id);p.AddAccessory(id+"-guardian-charm",house?"Midnight Charm":"Harvest Charm",2,2);}},
                PortalTargetMap="veilmoon_lake",NextName="Veilmoon Lake",Discoveries=discoveries,
                ChestRewards=new Dictionary<string,ChestRewardDef>()
            };
        }
    }
}
