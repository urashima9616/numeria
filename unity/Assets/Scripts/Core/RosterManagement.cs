using System;
using System.Collections.Generic;
using System.Linq;

namespace Numeria.Core
{
    public enum TeamSort { Species, Newest, Level, Defense, Attack }

    public partial class Progress
    {
        public TeamSort TeamSortOrder = TeamSort.Newest;
        public MathSettings Learning = new MathSettings();
        private void NormalizeSystemSettings()
        {
            if (Learning == null) Learning = new MathSettings();
            Learning.Normalize();
            if (!Enum.IsDefined(typeof(TeamSort), TeamSortOrder)) TeamSortOrder = TeamSort.Newest;
        }

        public List<string> SortedTeam()
        {
            var ids = new List<string> { "addmander" }; ids.AddRange(CaughtIds);
            string Name(string id) => GameData.ById(CurrentFormId(id)).Name;
            switch (TeamSortOrder)
            {
                case TeamSort.Species: return ids.OrderBy(id => GameData.LineFor(id)?.Element, StringComparer.Ordinal)
                    .ThenBy(Name, StringComparer.Ordinal).ToList();
                case TeamSort.Level: return ids.OrderByDescending(id => EnsureGrowth(id).Level).ThenBy(Name, StringComparer.Ordinal).ToList();
                case TeamSort.Defense: return ids.OrderByDescending(id => PlayerCombatant(id).DefensePower + TotalDefenseBonus(id)).ThenBy(Name, StringComparer.Ordinal).ToList();
                case TeamSort.Attack: return ids.OrderByDescending(id => PlayerCombatant(id).AttackPower + TotalAttackBonus(id)).ThenBy(Name, StringComparer.Ordinal).ToList();
                default: ids.Reverse(); return ids;
            }
        }

        public bool TransferAccessory(string instanceId, string targetId, string expectedOwner, string replaceId = null)
        {
            string target = GameData.BaseId(targetId);
            if (!Owns(target)) return false;
            var item = Accessories.Find(a => a.InstanceId == instanceId);
            if (item == null || (item.EquippedToBaseId ?? "") != (expectedOwner ?? "")) return false;
            if (item.EquippedToBaseId == target) return false;
            var replacement = string.IsNullOrEmpty(replaceId) ? null : Accessories.Find(a => a.InstanceId == replaceId);
            if (!string.IsNullOrEmpty(replaceId) && (replacement == null || replacement.EquippedToBaseId != target)) return false;
            if (EquippedAccessories(target).Count - (replacement == null ? 0 : 1) >= AccessorySlotCount(target)) return false;
            // All preconditions checked before either owner is changed.
            if (replacement != null) replacement.EquippedToBaseId = "";
            item.EquippedToBaseId = target;
            return true;
        }
    }
}
