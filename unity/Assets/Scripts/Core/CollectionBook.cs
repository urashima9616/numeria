using System;
using System.Collections.Generic;
using System.Linq;

namespace Numeria.Core
{
    public sealed class CollectionEntry
    {
        public int Number;
        public string Id,Name,Element,Description,PortraitId;
        public bool Collected;
    }
    public static class CollectionBook
    {
        public static CollectionEntry FamilyEntry(Progress p,string id)
        {
            var line=GameData.LineFor(id);if(line==null)return null;
            bool found=p.HasCollectedFamily(id);
            string portrait=line.StageIds.FirstOrDefault(p.HasCollected);
            return new CollectionEntry {Id=line.BaseId,Number=GameData.Lines.ToList().FindIndex(l=>l.BaseId==line.BaseId)+1,
                Collected=found,Name=found?GameData.SpeciesById(line.BaseId).Name:"???",
                Element=found?line.Element:null,PortraitId=portrait};
        }
        public static CollectionEntry[] Forms(Progress p,string id) =>
            GameData.LineFor(id)?.StageIds.Select(form=>Entry(p,form)).ToArray()??Array.Empty<CollectionEntry>();
        public static int FirstCollectedForm(Progress p,string id)
        {
            var line=GameData.LineFor(id);
            return line==null?0:Math.Max(0,Array.FindIndex(line.StageIds,p.HasCollected));
        }
        public static string[] Narration(string id) => Description(id).Split('.')
            .Select(s=>s.Trim()).Where(s=>s.Length>0).Select(s=>s+".").ToArray();
        public static CollectionEntry Entry(Progress p,string id)
        {
            var mon=GameData.SpeciesById(id);if(mon==null)return null;
            bool found=p.HasCollected(id);
            return new CollectionEntry {Id=id,Number=GameData.Lines.ToList().FindIndex(l=>l.BaseId==GameData.BaseId(id))+1,Collected=found,
                Name=found?mon.Name:"???",Element=found?GameData.LineFor(id)?.Element:null,
                Description=found?Description(id):null,PortraitId=found?id:null};
        }
        public static string Description(string id)
        {
            var expansion=ExpansionCatalog.Description(id);if(expansion!=null)return expansion;
            var species=GameData.SpeciesById(id);var line=GameData.LineFor(id);if(species==null||line==null)return "";
            var habitats=new Dictionary<string,string> {
                {"FIRE","warm volcanic paths"},{"WATER","clear streams and quiet pools"},{"GRASS","sunlit forest gardens"},
                {"BUG","leafy trails and flower beds"},{"ROCK","rocky ledges and crystal caves"},{"SKY","high peaks and floating ruins"},
                {"EARTH","sandy trails and hidden burrows"},{"MIND","ancient halls and peaceful clearings"},{"FAIRY","moonlit groves"},
                {"DRAGON","remote valleys and forgotten ruins"},{"ELECTRIC","glowing caves and stormy heights"},{"FLYING","open skies and windy cliffs"}};
            int stage=GameData.StageIndex(id);
            string nature=stage==0?"Curious and playful, it learns by exploring with friends.":stage==1?
                "With practice, it has learned to guide younger friends through difficult paths.":
                "A patient protector, it uses its strongest magic to keep its companions safe.";
            return $"{species.Name} feels at home around {habitats[line.Element]}. {nature} Its favorite magic is {line.AffinityLabel}, expressed through {species.SkillName}.";
        }
    }

    public partial class Progress
    {
        public List<string> CollectedForms=new List<string>();
        public List<string> PendingGiants=new List<string>();
        public List<string> DefeatedGiants=new List<string>();
        public bool HasCollected(string id)=>id=="addmander"||(CollectedForms!=null&&CollectedForms.Contains(id));
        public bool HasCollectedFamily(string id)=>GameData.LineFor(id)?.StageIds.Any(HasCollected)??false;
        public int CollectionCount=>GameData.Lines.Count(line=>HasCollectedFamily(line.BaseId));
        public void RecordCollected(string id)
        {
            if(GameData.SpeciesById(id)==null)return;
            if(CollectedForms==null)CollectedForms=new List<string>();
            if(!CollectedForms.Contains(id))CollectedForms.Add(id);
        }
        private void MigrateCollection()
        {
            if(CollectedForms==null)CollectedForms=new List<string>();
            if(PendingGiants==null)PendingGiants=new List<string>();
            if(DefeatedGiants==null)DefeatedGiants=new List<string>();
            RecordCollected("addmander");
            // Old saves kept only each family's current form, not capture/evolution history.
            // Seed only supported evidence; do not invent ownership of earlier forms.
            foreach(string id in CaughtIds.Prepend("addmander"))
            {var growth=FindGrowth(GameData.BaseId(id));RecordCollected(GameData.FormId(id,growth?.Stage??GameData.StageIndex(id)));}
        }
        public bool AwardGiant(string id)
        {
            if(DepthCatalog.Species(id)?.Giant!=true||ExpansionCatalog.DepthBase(id)!=id||DefeatedGiants.Contains(id))return false;
            DefeatedGiants.Add(id);RecordCollected(id);PendingGiants.Add(id);ClaimGiant(id);return true;
        }
        public bool ClaimGiant(string id)
        {
            var world=DepthCatalog.Worlds.FirstOrDefault(w=>w.Giant.Id==id);
            if(world==null||!PendingGiants.Contains(id)||TeamIsFull)return false;
            var result=AddCaught(id,world.Level);
            if(result!=CatchRosterResult.Added && !Owns(id))return false;
            PendingGiants.Remove(id);return true;
        }
    }
}
