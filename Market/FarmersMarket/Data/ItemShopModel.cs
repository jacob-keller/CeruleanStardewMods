﻿using System;
using FarmersMarket.ItemPriceAndStock;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FarmersMarket.Data
{
    public abstract class ItemShopModel
    {
        public string ShopName { get; set; }
        public string StoreCurrency { get; set; } = "Money";
        public List<int> CategoriesToSellHere { get; set; } = null;
        public string PortraitPath { get; set; } = null;
        public string Quote { get; set; } = null;
        public int ShopPrice { get; set; } = -1;
        public int MaxNumItemsSoldInStore { get; set; } = int.MaxValue;
        public double DefaultSellPriceMultipler { set => DefaultSellPriceMultiplier = value; }
        public double DefaultSellPriceMultiplier { get; set; } = 1;
        public Dictionary<double, string[]> PriceMultiplierWhen { get; set; } = null;
        public ItemStock[] ItemStocks { get; set; } = Array.Empty<ItemStock>();
        public string[] When { get; set; } = null;
        public string ClosedMessage { get; set; } = null;
        public Dictionary<string, string> LocalizedQuote { get; set; }
        public Dictionary<string, string> LocalizedClosedMessage { get; set; }
        
        public int SignObject { get; set; }
        public Color Color { get; set; }
    }
}
