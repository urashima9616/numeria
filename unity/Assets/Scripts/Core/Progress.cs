using System;
using System.Collections.Generic;

namespace Numeria.Core
{
    [Serializable]
    public class MonGrowth
    {
        public string BaseId;
        public int Level = 1;
        public int Xp;
        public int AttackBonus;
        public int DefenseBonus;
        public int Stage;

        public int XpToNext => Level * 10;

        public int GainXp(int amount)
        {
            Xp += amount;
            int levelsGained = 0;
            while (Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                AttackBonus++;
                if (Level % 2 == 0) DefenseBonus++;
                levelsGained++;
            }
            return levelsGained;
        }
    }

    /// <summary>
    /// 玩家养成进度:经验/等级、收服图鉴、已开宝箱、装备加成。
    /// 纯数据类,可被 JsonUtility 序列化(字段必须 public)。
    /// </summary>
    [Serializable]
    public class Progress
    {
        public const int CurrentSaveVersion = 4;

        public int SaveVersion = CurrentSaveVersion;
        public int Level = 1;
        public int Xp;
        public int AttackBonus;
        public int DefenseBonus;
        public bool BossBeaten;
        public bool VoiceEnabled = true;
        public bool SfxEnabled = true;
        public bool MusicEnabled = true;
        public bool HasEvoStone;
        public bool Evolved;
        public int EvolutionStones;
        public string CurrentMap = "forest";
        public string ActiveMonId = "addmander";
        public List<string> CaughtIds = new List<string>();
        public List<string> OpenedChests = new List<string>();
        public List<string> ClearedGates = new List<string>();
        public List<string> Items = new List<string>();
        public List<MonGrowth> MonGrowth = new List<MonGrowth>();

        /// <summary>补齐旧 JSON 中不存在的字段。每次新增持久化字段时在这里按版本迁移。</summary>
        public void ApplyMigrations()
        {
            if (SaveVersion < 2)
            {
                SfxEnabled = true;
                MusicEnabled = true;
            }
            if (MonGrowth == null) MonGrowth = new List<MonGrowth>();
            if (CaughtIds == null) CaughtIds = new List<string>();
            if (OpenedChests == null) OpenedChests = new List<string>();
            if (ClearedGates == null) ClearedGates = new List<string>();
            if (Items == null) Items = new List<string>();

            if (SaveVersion < 3)
            {
                if (HasEvoStone && EvolutionStones == 0) EvolutionStones = 1;
                var starter = FindGrowth("addmander");
                if (starter == null)
                {
                    MonGrowth.Add(new MonGrowth
                    {
                        BaseId = "addmander",
                        Level = Math.Max(1, Level),
                        Xp = Xp,
                        AttackBonus = AttackBonus,
                        DefenseBonus = DefenseBonus,
                        Stage = Evolved ? 1 : 0,
                    });
                }
                foreach (string id in CaughtIds) EnsureGrowth(id);
            }
            if (SaveVersion < 4)
            {
                foreach (var growth in MonGrowth)
                    growth.DefenseBonus = Math.Max(growth.DefenseBonus, growth.Level / 2);
            }
            SaveVersion = CurrentSaveVersion;
            SyncLegacyFields();
        }

        /// <summary>升到下一级所需经验:等级 × 10,数字小,孩子能心算。</summary>
        public int XpToNext => ActiveGrowth.XpToNext;

        public MonGrowth ActiveGrowth => EnsureGrowth(ActiveMonId);

        public MonGrowth EnsureGrowth(string id)
        {
            if (MonGrowth == null) MonGrowth = new List<MonGrowth>();
            string baseId = GameData.BaseId(id);
            var found = FindGrowth(baseId);
            if (found != null) return found;

            // 旧版把唯一队员的成长存在顶层字段。第一次创建其他家族前先冻结这份 starter 数据，
            // 否则切换 ActiveMonId 后，顶层镜像会被误当成新家族的等级。
            if (baseId != "addmander" && FindGrowth("addmander") == null)
            {
                MonGrowth.Add(new MonGrowth
                {
                    BaseId = "addmander",
                    Level = Math.Max(1, Level),
                    Xp = Xp,
                    AttackBonus = AttackBonus,
                    DefenseBonus = DefenseBonus,
                    Stage = Evolved ? 1 : 0,
                });
            }

            found = new MonGrowth { BaseId = baseId };
            // 新存档第一次访问 Addmander 时沿用历史字段，保证旧测试和旧 JSON 都不丢成长。
            if (baseId == "addmander")
            {
                found.Level = Math.Max(1, Level);
                found.Xp = Xp;
                found.AttackBonus = AttackBonus;
                found.DefenseBonus = DefenseBonus;
                found.Stage = Evolved ? 1 : 0;
            }
            MonGrowth.Add(found);
            return found;
        }

        public MonGrowth FindGrowth(string id)
        {
            if (MonGrowth == null) return null;
            string baseId = GameData.BaseId(id);
            return MonGrowth.Find(g => g.BaseId == baseId);
        }

        public string CurrentFormId(string id)
        {
            var growth = EnsureGrowth(id);
            return GameData.FormId(growth.BaseId, growth.Stage);
        }

        public bool CanEvolve(string id)
        {
            var growth = EnsureGrowth(id);
            int requiredLevel = GameData.NextEvolutionLevel(growth.BaseId, growth.Stage);
            int requiredStones = growth.Stage + 1;
            return requiredLevel > 0 && growth.Level >= requiredLevel && EvolutionStones >= requiredStones;
        }

        public bool AdvanceEvolution(string id)
        {
            if (!CanEvolve(id)) return false;
            var growth = EnsureGrowth(id);
            growth.Stage++;
            SyncLegacyFields();
            return true;
        }

        public void AddEvolutionStone()
        {
            EvolutionStones++;
            HasEvoStone = EvolutionStones > 0;
        }

        /// <summary>获得经验,返回本次升了几级。每升一级攻击 +1。</summary>
        public int GainXp(int amount)
        {
            int levelsGained = ActiveGrowth.GainXp(amount);
            SyncLegacyFields();
            return levelsGained;
        }

        /// <summary>收服数灵。已收服过返回 false。</summary>
        public bool Catch(string mathmonId)
        {
            string baseId = GameData.BaseId(mathmonId);
            if (CaughtIds.Contains(baseId)) return false;
            CaughtIds.Add(baseId);
            EnsureGrowth(baseId);
            return true;
        }

        /// <summary>开宝箱。已开过返回 false。</summary>
        public bool OpenChest(string chestId)
        {
            if (OpenedChests.Contains(chestId)) return false;
            OpenedChests.Add(chestId);
            return true;
        }

        private void SyncLegacyFields()
        {
            var active = EnsureGrowth(ActiveMonId);
            Level = active.Level;
            Xp = active.Xp;
            AttackBonus = active.AttackBonus;
            DefenseBonus = active.DefenseBonus;

            var starter = FindGrowth("addmander");
            Evolved = starter != null && starter.Stage > 0;
            HasEvoStone = EvolutionStones > 0;
        }
    }
}
