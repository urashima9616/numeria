using System;
using System.Collections.Generic;

namespace Numeria.Core
{
    public enum ConsumableType { HealthPotion, GemSnack }
    public enum CatchRosterResult { Added, Duplicate, Full }

    [Serializable]
    public class AccessoryItem
    {
        public string InstanceId;
        public string Name;
        public int AttackBonus;
        public int DefenseBonus;
        public string EquippedToBaseId = "";
    }

    [Serializable]
    public class AdventureRecords
    {
        public int BattlesStarted;
        public int BattlesWon;
        public int BattlesLost;
        public int MonstersCaught;
        public int BossesDefeated;
        public int ChestsOpened;
        public int PuzzlesCompleted;
        public int PuzzlesSolved;
        public int ConsumablesUsed;
        public int TotalXpEarned;
        public int HighestDamage;
        public int HighestLevel = 1;
        public int CoinsEarned;
        public int CoinsSpent;
        public int DiscoveriesSolved;
        public int MerchantsDefeated;
        public int DigitCrystalsRestored;
    }

    [Serializable]
    public class ShopPurchaseRecord
    {
        public string StockId;
        public int Count;
    }

    [Serializable]
    public class MonGrowth
    {
        public string BaseId;
        public int Level = 1;
        public int Xp;
        public int AttackBonus;
        public int DefenseBonus;
        public int Stage;

        public int XpToNext => GrowthSystem.XpToNext(Level);

        public int GainXp(int amount)
        {
            if (Level >= GrowthSystem.MaxLevel) { Xp = 0; return 0; }
            Xp += Math.Max(0, amount);
            int levelsGained = 0;
            while (Level < GrowthSystem.MaxLevel && Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                levelsGained++;
            }
            if (Level >= GrowthSystem.MaxLevel) Xp = 0;
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
        public const int CurrentSaveVersion = 8;
        public const int TeamCapacity = 15;

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
        public int HealthPotions;
        public int GemSnacks;
        public int Coins;
        public bool StoryIntroSeen;
        public string CurrentMap = "forest";
        public string ActiveMonId = "addmander";
        public List<string> CaughtIds = new List<string>();
        public List<string> OpenedChests = new List<string>();
        public List<string> ClearedGates = new List<string>();
        public List<string> Items = new List<string>();
        public List<AccessoryItem> Accessories = new List<AccessoryItem>();
        public List<MonGrowth> MonGrowth = new List<MonGrowth>();
        public List<string> CollectedDiscoveries = new List<string>();
        public List<string> DefeatedMerchants = new List<string>();
        public List<ShopPurchaseRecord> ShopPurchases = new List<ShopPurchaseRecord>();
        public List<string> DigitCrystals = new List<string>();
        public AdventureRecords Records = new AdventureRecords();

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
            if (Accessories == null) Accessories = new List<AccessoryItem>();
            if (CollectedDiscoveries == null) CollectedDiscoveries = new List<string>();
            if (DefeatedMerchants == null) DefeatedMerchants = new List<string>();
            if (ShopPurchases == null) ShopPurchases = new List<ShopPurchaseRecord>();
            if (DigitCrystals == null) DigitCrystals = new List<string>();
            if (Records == null) Records = new AdventureRecords();

            // Starter 永远占据第一个伙伴位；旧存档若意外重复写入或出现重复家族，在这里无损去重。
            var uniqueCaught = new List<string>();
            foreach (string id in CaughtIds)
            {
                string baseId = GameData.BaseId(id);
                if (baseId != "addmander" && !uniqueCaught.Contains(baseId)) uniqueCaught.Add(baseId);
            }
            CaughtIds = uniqueCaught;

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
            if (SaveVersion < 5)
            {
                // v4 的等级加成继续保留为训练奖励，避免旧存档升级后突然变弱。
                foreach (var growth in MonGrowth)
                    growth.Level = GrowthSystem.ClampLevel(growth.Level);
                Records.HighestLevel = Math.Max(1, Level);
            }
            if (SaveVersion < 6)
            {
                // 旧版 Items 只有名字且加成已直接写入某只数灵，无法可靠反推出归属。
                // 保留旧数值作为 legacy training bonus，同时把饰品转成未装备库存，避免重复加成。
                for (int i = 0; i < Items.Count; i++)
                {
                    string name = Items[i];
                    bool defense = name.Contains("Guard") || name.Contains("Feather");
                    AddAccessory($"legacy-{i}-{name}", name, defense ? 0 : 1, defense ? 1 : 0);
                }
            }
            if (SaveVersion < 7)
            {
                // v7 首次加入金币、发现点和商店限量记录；默认 0/空列表即可无损迁移。
                Coins = Math.Max(0, Coins);
            }
            if (SaveVersion < 8)
            {
                // 旧存档按已击败守卫补发对应主线水晶，避免升级后要求玩家重复打 Boss。
                if (BossBeaten && !DigitCrystals.Contains("forest")) DigitCrystals.Add("forest");
                if (ClearedGates.Contains("mountains") && !DigitCrystals.Contains("mountains"))
                    DigitCrystals.Add("mountains");
                if (ClearedGates.Contains("sky") && !DigitCrystals.Contains("sky")) DigitCrystals.Add("sky");
                Records.DigitCrystalsRestored = DigitCrystals.Count;
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
            Records.TotalXpEarned += Math.Max(0, amount);
            Records.HighestLevel = Math.Max(Records.HighestLevel, ActiveGrowth.Level);
            SyncLegacyFields();
            return levelsGained;
        }

        public int ConsumableCount(ConsumableType type) =>
            type == ConsumableType.HealthPotion ? HealthPotions : GemSnacks;

        public void AddConsumable(ConsumableType type, int amount = 1)
        {
            if (amount <= 0) return;
            if (type == ConsumableType.HealthPotion) HealthPotions += amount;
            else GemSnacks += amount;
        }

        public bool UseConsumable(ConsumableType type)
        {
            if (ConsumableCount(type) <= 0) return false;
            if (type == ConsumableType.HealthPotion) HealthPotions--;
            else GemSnacks--;
            Records.ConsumablesUsed++;
            return true;
        }

        public void RecordPuzzle(bool solved)
        {
            Records.PuzzlesCompleted++;
            if (solved) Records.PuzzlesSolved++;
        }

        public int AccessorySlotCount(string mathmonId) => 2 + EnsureGrowth(mathmonId).Stage;

        public List<AccessoryItem> EquippedAccessories(string mathmonId)
        {
            string baseId = GameData.BaseId(mathmonId);
            return Accessories.FindAll(item => item.EquippedToBaseId == baseId);
        }

        public int AccessoryAttackBonus(string mathmonId)
        {
            int result = 0;
            foreach (var item in EquippedAccessories(mathmonId)) result += item.AttackBonus;
            return result;
        }

        public int AccessoryDefenseBonus(string mathmonId)
        {
            int result = 0;
            foreach (var item in EquippedAccessories(mathmonId)) result += item.DefenseBonus;
            return result;
        }

        public int TotalAttackBonus(string mathmonId) =>
            EnsureGrowth(mathmonId).AttackBonus + AccessoryAttackBonus(mathmonId);

        public int TotalDefenseBonus(string mathmonId) =>
            EnsureGrowth(mathmonId).DefenseBonus + AccessoryDefenseBonus(mathmonId);

        public bool AddAccessory(string instanceId, string name, int attackBonus, int defenseBonus)
        {
            if (string.IsNullOrEmpty(instanceId) || Accessories.Exists(item => item.InstanceId == instanceId))
                return false;
            Accessories.Add(new AccessoryItem
            {
                InstanceId = instanceId,
                Name = name,
                AttackBonus = Math.Max(0, attackBonus),
                DefenseBonus = Math.Max(0, defenseBonus),
            });
            return true;
        }

        public bool EquipAccessory(string instanceId, string mathmonId)
        {
            var item = Accessories.Find(candidate => candidate.InstanceId == instanceId);
            if (item == null) return false;
            string baseId = GameData.BaseId(mathmonId);
            if (item.EquippedToBaseId == baseId) return true;
            if (EquippedAccessories(baseId).Count >= AccessorySlotCount(baseId)) return false;
            item.EquippedToBaseId = baseId;
            return true;
        }

        public bool UnequipAccessory(string instanceId)
        {
            var item = Accessories.Find(candidate => candidate.InstanceId == instanceId);
            if (item == null || string.IsNullOrEmpty(item.EquippedToBaseId)) return false;
            item.EquippedToBaseId = "";
            return true;
        }

        public int TeamCount => 1 + CaughtIds.Count;
        public bool TeamIsFull => TeamCount >= TeamCapacity;

        public bool Owns(string mathmonId)
        {
            string baseId = GameData.BaseId(mathmonId);
            return baseId == "addmander" || CaughtIds.Contains(baseId);
        }

        /// <summary>尝试将新伙伴加入最多 15 只的队伍；满员与重复必须由调用方分别处理。</summary>
        public CatchRosterResult AddCaught(string mathmonId)
        {
            string baseId = GameData.BaseId(mathmonId);
            if (Owns(baseId)) return CatchRosterResult.Duplicate;
            if (TeamIsFull) return CatchRosterResult.Full;
            CaughtIds.Add(baseId);
            EnsureGrowth(baseId);
            return CatchRosterResult.Added;
        }

        /// <summary>旧调用兼容：只有真正加入队伍才返回 true。</summary>
        public bool Catch(string mathmonId) => AddCaught(mathmonId) == CatchRosterResult.Added;

        /// <summary>
        /// 满员时用新伙伴替换一只已捕获伙伴。首只 Addmander 不可释放；被释放伙伴的饰品回到库存。
        /// </summary>
        public bool ReplaceCaught(string releasedMathmonId, string newMathmonId)
        {
            string released = GameData.BaseId(releasedMathmonId);
            string newcomer = GameData.BaseId(newMathmonId);
            int index = CaughtIds.IndexOf(released);
            if (index < 0 || released == "addmander" || Owns(newcomer)) return false;

            foreach (var accessory in Accessories)
                if (accessory.EquippedToBaseId == released) accessory.EquippedToBaseId = "";
            MonGrowth.RemoveAll(growth => growth.BaseId == released);
            CaughtIds[index] = newcomer;
            EnsureGrowth(newcomer);
            if (GameData.BaseId(ActiveMonId) == released) ActiveMonId = newcomer;
            SyncLegacyFields();
            return true;
        }

        public void AddCoins(int amount)
        {
            int safe = Math.Max(0, amount);
            Coins += safe;
            Records.CoinsEarned += safe;
        }

        public bool TrySpendCoins(int amount)
        {
            int safe = Math.Max(0, amount);
            if (Coins < safe) return false;
            Coins -= safe;
            Records.CoinsSpent += safe;
            return true;
        }

        public bool CollectDiscovery(string id)
        {
            if (string.IsNullOrEmpty(id) || CollectedDiscoveries.Contains(id)) return false;
            CollectedDiscoveries.Add(id);
            Records.DiscoveriesSolved++;
            return true;
        }

        public bool DefeatMerchant(string id)
        {
            if (string.IsNullOrEmpty(id) || DefeatedMerchants.Contains(id)) return false;
            DefeatedMerchants.Add(id);
            Records.MerchantsDefeated++;
            return true;
        }

        public int PurchaseCount(string stockId)
        {
            var record = ShopPurchases.Find(item => item.StockId == stockId);
            return record?.Count ?? 0;
        }

        public int RecordPurchase(string stockId)
        {
            var record = ShopPurchases.Find(item => item.StockId == stockId);
            if (record == null)
            {
                record = new ShopPurchaseRecord { StockId = stockId };
                ShopPurchases.Add(record);
            }
            record.Count++;
            return record.Count;
        }

        public int DigitCrystalCount => DigitCrystals.Count;

        public bool CollectDigitCrystal(string mapId)
        {
            if (string.IsNullOrEmpty(mapId) || DigitCrystals.Contains(mapId)) return false;
            DigitCrystals.Add(mapId);
            Records.DigitCrystalsRestored++;
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
