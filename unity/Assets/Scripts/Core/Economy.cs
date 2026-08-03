using System;

namespace Numeria.Core
{
    public enum ShopItemType
    {
        HealthPotion,
        GemSnack,
        EvolutionStone,
        Accessory,
    }

    public enum PurchaseResult { Purchased, SoldOut, NotEnoughCoins }

    [Serializable]
    public sealed class ShopItemDef
    {
        public string Id;
        public string Name;
        public string Description;
        public ShopItemType Type;
        public int Price;
        public int StockLimit;
        public int Amount = 1;
        public int AttackBonus;
        public int DefenseBonus;
    }

    /// <summary>金币掉落和限量商店的纯逻辑层，数值保持小而可心算。</summary>
    public static class EconomySystem
    {
        public static int VictoryCoins(int tier, bool boss, bool merchant, Rng rng)
        {
            int safeTier = Math.Max(1, tier);
            if (merchant) return 10 + safeTier * 6;   // 16 / 22 / 28
            if (boss) return 20 + safeTier * 10;     // 30 / 40 / 50
            return rng.Pick(2 + safeTier, 4 + safeTier * 2); // 3–6 / 4–8 / 5–10
        }

        public static PurchaseResult Buy(Progress progress, ShopItemDef item)
        {
            if (progress.PurchaseCount(item.Id) >= item.StockLimit) return PurchaseResult.SoldOut;
            if (!progress.TrySpendCoins(item.Price)) return PurchaseResult.NotEnoughCoins;

            int purchaseNumber = progress.RecordPurchase(item.Id);
            switch (item.Type)
            {
                case ShopItemType.HealthPotion:
                    progress.AddConsumable(ConsumableType.HealthPotion, item.Amount);
                    break;
                case ShopItemType.GemSnack:
                    progress.AddConsumable(ConsumableType.GemSnack, item.Amount);
                    break;
                case ShopItemType.EvolutionStone:
                    for (int i = 0; i < item.Amount; i++) progress.AddEvolutionStone();
                    break;
                default:
                    progress.AddAccessory($"shop-{item.Id}-{purchaseNumber}", item.Name,
                        item.AttackBonus, item.DefenseBonus);
                    break;
            }
            return PurchaseResult.Purchased;
        }
    }
}
