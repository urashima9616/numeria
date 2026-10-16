using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Numeria.Core;
using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    /// <summary>Mechanical atlas slicing only. Preserve ImageGen alpha, never redraw or recolor a creature.</summary>
    public static class HalloweenAssetImporter
    {
        private static bool _reslice;
        private static readonly HashSet<string> Independent = new HashSet<string>{"pipkin","vinejack","crownkin","tickaboo","penduloom","chronophantom","bellbit"};
        public static void ResliceProductionAtlases()
        {
            // Only the 266 newly authored portraits belong to this importer. Legacy files are never targets.
            _reslice=true;try{Import();}finally{_reslice=false;}
        }
        private sealed class Island
        {
            public readonly List<int> Pixels=new List<int>();
            public int Left=int.MaxValue,Right,Bottom=int.MaxValue,Top;
            public void Add(int p,int width){Pixels.Add(p);int x=p%width,y=p/width;Left=Math.Min(Left,x);Right=Math.Max(Right,x);Bottom=Math.Min(Bottom,y);Top=Math.Max(Top,y);}
        }
        private static Island[] Islands(Color32[] pixels,int w,int h,int count,int columns)
        {
            var visited=new bool[pixels.Length];var islands=new List<Island>();var queue=new Queue<int>();
            for(int seed=0;seed<pixels.Length;seed++)
            {
                if(visited[seed]||pixels[seed].a<32)continue;
                var island=new Island();visited[seed]=true;queue.Enqueue(seed);
                while(queue.Count>0)
                {
                    int p=queue.Dequeue();island.Add(p,w);int x=p%w,y=p/w;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;
                        int next=yy*w+xx;if(visited[next]||pixels[next].a<32)continue;
                        visited[next]=true;queue.Enqueue(next);
                    }
                }
                islands.Add(island);
            }
            var main=islands.OrderByDescending(i=>i.Pixels.Count).Take(count).ToArray();
            if(main.Length!=count||main.Any(i=>i.Pixels.Count<200))throw new InvalidDataException("Atlas does not contain enough separate characters.");
            // Production atlases have orderly rows, but artwork crosses nominal equal-cell boundaries.
            // Sort whole silhouettes, not clipped cell rectangles; keep detached accents with their nearest body.
            main=main.OrderByDescending(i=>(i.Top+i.Bottom)*.5f).ToArray();
            for(int row=0;row<main.Length;row+=columns)
            {var ordered=main.Skip(row).Take(columns).OrderBy(i=>(i.Left+i.Right)*.5f).ToArray();Array.Copy(ordered,0,main,row,ordered.Length);}
            foreach(var extra in islands.Except(main))
            {
                float cx=(extra.Left+extra.Right)*.5f,cy=(extra.Bottom+extra.Top)*.5f;
                var owner=main.OrderBy(i=>Math.Pow(Math.Max(0,Math.Max(i.Left-cx,cx-i.Right)),2)+Math.Pow(Math.Max(0,Math.Max(i.Bottom-cy,cy-i.Top)),2)).First();
                foreach(int p in extra.Pixels)owner.Add(p,w);
            }
            return main;
        }
        public static void Import()
        {
            ImportSpellMotifs();
            ImportDepthSpellMotifs();
            for(int group=0;group<6;group++)
            {
                string path="../art/halloween-atlases/halloween-families-"+group+".png";
                if(File.Exists(path))Split(path,ExpansionCatalog.Families.Skip(group*5).Take(5).SelectMany(f=>f.Ids).ToArray(),3,5);
            }
            foreach(var world in DepthCatalog.Worlds)
            {
                string path="../art/halloween-atlases/depth-evolutions-"+world.Id+".png";
                if(File.Exists(path))Split(path,world.Species.SelectMany(s=>ExpansionCatalog.DepthStages(s.Id).Skip(1)).ToArray(),4,6);
            }
            foreach(string id in new[]{"spooky_pumpkin_patch","haunted_house"})
            {
                string directory="Assets/Resources/generated/Exploration/";
                Split(directory+id+"_props.png",Enumerable.Range(0,4).Select(i=>id+"-prop-"+i).ToArray(),2,2,directory,"");
                Split("../art/halloween-atlases/"+(id=="haunted_house"?"house":"pumpkin")+"-details.png",
                    Enumerable.Range(0,6).Select(i=>id+"-detail-"+i).ToArray(),2,3,directory,"");
            }
            // The shared spell atlas supplies actual inventory icons, not Unicode fallback symbols.
            string spell="Assets/Resources/generated/Exploration/spell_halloween.png";
            if(File.Exists(spell))Split(spell,new[]{"pumpkin_magic","pumpkin_lantern","ghost_magic","ghost_clock"},2,2,"Assets/Resources/generated/Skills/","");
            AssetDatabase.Refresh();
        }
        public static void ImportSpellMotifs()
        {
            foreach(string theme in new[]{"pumpkin","ghost"})
                Split("../art/halloween-atlases/"+theme+"-spells.png",
                    Enumerable.Range(0,16).Select(i=>theme+"-spell-"+i).ToArray(),4,4,
                    "Assets/Resources/generated/Exploration/","",true);
            AssetDatabase.Refresh();
        }
        public static void ImportDepthSpellMotifs()
        {
            foreach(var world in DepthCatalog.Worlds)
            {
                bool landscape=world.Id=="resonant_foundry";
                Split("../art/halloween-atlases/depth-spells-"+world.Id+".png",
                    world.Species.Select(s=>"depth-spell-"+s.Id).Concat(new[]{"depth-spell-"+world.Id+"-accent"}).ToArray(),
                    landscape?4:3,landscape?3:4,"Assets/Resources/generated/Exploration/","",true);
            }
            AssetDatabase.Refresh();
            foreach(string guid in AssetDatabase.FindAssets("depth-spell- t:Texture2D",new[]{"Assets/Resources/generated/Exploration"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer.maxTextureSize!=256){importer.maxTextureSize=256;importer.SaveAndReimport();}
            }
        }
        private static Island CellSilhouette(Color32[] pixels,int w,int left,int bottom,int cw,int ch)
        {
            var seen=new bool[cw*ch];var parts=new List<Island>();var queue=new Queue<int>();
            for(int start=0;start<seen.Length;start++)
            {
                int source=(bottom+start/cw)*w+left+start%cw;
                if(seen[start]||pixels[source].a<32)continue;
                var part=new Island();seen[start]=true;queue.Enqueue(start);
                while(queue.Count>0)
                {
                    int p=queue.Dequeue(),x=p%cw,y=p/cw;part.Add((bottom+y)*w+left+x,w);
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int xx=x+dx,yy=y+dy;if(xx<0||xx>=cw||yy<0||yy>=ch)continue;
                        int next=yy*cw+xx;if(seen[next]||pixels[(bottom+yy)*w+left+xx].a<32)continue;
                        seen[next]=true;queue.Enqueue(next);
                    }
                }
                parts.Add(part);
            }
            if(parts.Count==0)throw new InvalidDataException("Empty spell cell");
            int largest=parts.Max(p=>p.Pixels.Count);var result=new Island();
            // Ignore tiny fragments cut from adjacent cells; preserve every substantial ring contour.
            foreach(var part in parts.Where(p=>p.Pixels.Count>=Math.Max(12,largest*.02f)))
                foreach(int pixel in part.Pixels)result.Add(pixel,w);
            return result;
        }
        private static void Split(string path,string[] ids,int columns,int rows,string directory="Assets/Resources/generated/",string suffix="_large_icon",bool strictCells=false)
        {
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            if(!source.LoadImage(File.ReadAllBytes(path)))throw new InvalidDataException(path);
            try
            {
                var pixels=source.GetPixels32();int w=source.width,h=source.height;
                if(pixels.Count(p=>p.a==0)<pixels.Length/10)throw new InvalidDataException("Atlas lacks transparent negative space: "+path);
                Directory.CreateDirectory(directory);
                var silhouettes=strictCells?new Island[ids.Length]:Islands(pixels,w,h,ids.Length,columns);
                if(strictCells)for(int i=0;i<ids.Length;i++)
                {
                    // Spell sheets have strict equal cells; rings can contain multiple disconnected contours.
                    // Component ranking would mistake those contours for separate motifs and reorder later rows.
                    int left=i%columns*w/columns,bottom=(rows-1-i/columns)*h/rows;
                    var cell=CellSilhouette(pixels,w,left,bottom,w/columns,h/rows);
                    if(cell.Pixels.Count<200)throw new InvalidDataException("Empty spell cell: "+path+" #"+i);
                    silhouettes[i]=cell;
                }
                for(int i=0;i<ids.Length;i++)
                {
                    string output=directory+ids[i]+suffix+".png";
                    // Never replace legacy portraits or the six separately authored hero portraits.
                    if(File.Exists(output)&&!strictCells&&(!_reslice||Independent.Contains(ids[i])))continue;
                    var silhouette=silhouettes[i];var included=new HashSet<int>(silhouette.Pixels);
                    int minx=silhouette.Left,miny=silhouette.Bottom,maxx=silhouette.Right,maxy=silhouette.Top;
                    int bw=maxx-minx+1,bh=maxy-miny+1,side=Mathf.CeilToInt(Math.Max(bw,bh)*1.16f);
                    var icon=new Texture2D(512,512,TextureFormat.RGBA32,false);var target=new Color32[512*512];
                    for(int y=0;y<512;y++)for(int x=0;x<512;x++)
                    {
                        int sx=minx+Mathf.FloorToInt(x*side/512f-(side-bw)*.5f);
                        int sy=miny+Mathf.FloorToInt(y*side/512f-(side-bh)*.5f);
                        if(sx>=minx&&sx<=maxx&&sy>=miny&&sy<=maxy&&included.Contains(sy*w+sx))target[y*512+x]=pixels[sy*w+sx];
                    }
                    icon.SetPixels32(target);icon.Apply();File.WriteAllBytes(output,icon.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(icon);
                }
            }
            finally {UnityEngine.Object.DestroyImmediate(source);}
        }
    }
}
