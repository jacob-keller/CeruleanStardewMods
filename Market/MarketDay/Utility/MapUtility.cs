using System.Collections.Generic;
using System.Linq;
using MarketDay.Shop;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace MarketDay.Utility
{
    public class MapUtility
    {
        /// <summary>
        /// Returns the tile property found at the given parameters
        /// </summary>
        /// <param name="map">an instance of the the map location</param>
        /// <param name="layer">the name of the layer</param>
        /// <param name="tile">the coordinates of the tile</param>
        /// <returns>The tile property if there is one, null if there isn't</returns>
        public static List<Vector2> ShopTiles()
        {
            List<Vector2> ShopLocations = new();
            var town = Game1.getLocationFromName("Town");
            if (town is null)
            {
                MarketDay.monitor.Log($"ShopTiles: Town location not available", LogLevel.Error);
                return ShopLocations;
            }

            var layerWidth = town.map.Layers[0].LayerWidth;
            var layerHeight = town.map.Layers[0].LayerHeight;

            // top left corner is z_MarketDay 253
            for (var x = 0; x < layerWidth; x++)
            {
                for (var y = 0; y < layerHeight; y++)
                {
                    var tileSheetIdAt = town.getTileSheetIDAt(x, y, "Buildings");
                    if (tileSheetIdAt != "z_MarketDay") continue;
                    var tileIndexAt = town.getTileIndexAt(x, y, "Buildings");
                    if (tileIndexAt != 253) continue;
                    
                    ShopLocations.Add(new Vector2(x, y));
                }
            }

            // var locs = string.Join(", ", ShopLocations);
            // MarketDay.monitor.Log($"ShopTiles: {locs}", LogLevel.Debug);

            return ShopLocations;
        }

        internal static Dictionary<Vector2, GrangeShop> ShopAtTile()
        {
            var town = Game1.getLocationFromName("Town");
            var shopsAtTiles = new Dictionary<Vector2, GrangeShop>();

            foreach (var tile in ShopTiles())
            {
                // MarketDay.monitor.Log($"ShopAtTile: {tile}", LogLevel.Debug);

                var signTile = tile + new Vector2(3, 3);
                if (!town.objects.TryGetValue(signTile, out var obj) || obj is not Sign sign) continue;
                // MarketDay.monitor.Log($"    {signTile} is Sign", LogLevel.Debug);

                if (sign.modData.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/{GrangeShop.ShopSignKey}", out var signOwner))
                {
                    // MarketDay.monitor.Log($"        signOwner {signOwner}", LogLevel.Debug);

                    shopsAtTiles[tile] = ShopManager.GrangeShops[signOwner];
                }
            }

            return shopsAtTiles;
        }

        internal static string Owner(Item item)
        {
            item.modData.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/{GrangeShop.GrangeChestKey}", out var grangeChestOwner);
            item.modData.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/{GrangeShop.StockChestKey}", out var stockChestOwner);
            item.modData.TryGetValue($"{MarketDay.SMod.ModManifest.UniqueID}/{GrangeShop.ShopSignKey}", out var signOwner);
            string owner = null;
            if (grangeChestOwner is not null) owner = grangeChestOwner;
            if (stockChestOwner is not null) owner = stockChestOwner;
            if (signOwner is not null) owner = signOwner;
            return owner;
        }
    }
}