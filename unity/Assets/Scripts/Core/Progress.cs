using System;
using System.Collections.Generic;

namespace Numeria.Core
{
    /// <summary>
    /// 玩家养成进度:经验/等级、收服图鉴、已开宝箱、装备加成。
    /// 纯数据类,可被 JsonUtility 序列化(字段必须 public)。
    /// </summary>
    [Serializable]
    public class Progress
    {
        public int Level = 1;
        public int Xp;
        public int AttackBonus;
        public bool BossBeaten;
        public bool VoiceEnabled = true;
        public bool HasEvoStone;
        public bool Evolved;
        public string CurrentMap = "forest";
        public List<string> CaughtIds = new List<string>();
        public List<string> OpenedChests = new List<string>();
        public List<string> ClearedGates = new List<string>();

        /// <summary>升到下一级所需经验:等级 × 10,数字小,孩子能心算。</summary>
        public int XpToNext => Level * 10;

        /// <summary>获得经验,返回本次升了几级。每升一级攻击 +1。</summary>
        public int GainXp(int amount)
        {
            Xp += amount;
            int levelsGained = 0;
            while (Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                AttackBonus++;
                levelsGained++;
            }
            return levelsGained;
        }

        /// <summary>收服数灵。已收服过返回 false。</summary>
        public bool Catch(string mathmonId)
        {
            if (CaughtIds.Contains(mathmonId)) return false;
            CaughtIds.Add(mathmonId);
            return true;
        }

        /// <summary>开宝箱。已开过返回 false。</summary>
        public bool OpenChest(string chestId)
        {
            if (OpenedChests.Contains(chestId)) return false;
            OpenedChests.Add(chestId);
            return true;
        }
    }
}
