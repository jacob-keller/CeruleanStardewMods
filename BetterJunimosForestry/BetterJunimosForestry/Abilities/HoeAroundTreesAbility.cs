using System;
using System.Collections.Generic;
using System.Linq;
using BetterJunimos;
using BetterJunimos.Abilities;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewModdingAPI;
using StardewValley.Buildings;
using SObject = StardewValley.Object;
using StardewValley.GameData.Crops;

// bits of this are from Tractor Mod; https://github.com/Pathoschild/StardewMods/blob/68628a40f992288278b724984c0ade200e6e4296/TractorMod/Framework/BaseAttachment.cs#L132

namespace BetterJunimosForestry.Abilities
{
    public class HoeAroundTreesAbility : IJunimoAbility
    {
        private readonly IMonitor Monitor;
        private static readonly List<string> WildTreeSeeds = new() {"292", "309", "310", "311", "891"};
        private static readonly Dictionary<string, Dictionary<string, bool>> cropSeasons = new();
        private const int SunflowerSeeds = 431;

        internal HoeAroundTreesAbility(IMonitor Monitor)
        {
            this.Monitor = Monitor;
            var seasons = new List<string> {"spring", "summer", "fall", "winter"};
            foreach (string season in seasons)
            {
                cropSeasons[season] = new Dictionary<string, bool>();
            }
        }

        public string AbilityName()
        {
            return "HoeAroundTrees";
        }

        public bool IsActionAvailable(GameLocation location, Vector2 pos, Guid guid)
        {
            var hut = Util.GetHutFromId(guid);
            var mode = Util.GetModeForHut(hut);
            if (mode == Modes.Normal) return false;

            var up = new Vector2(pos.X, pos.Y + 1);
            var right = new Vector2(pos.X + 1, pos.Y);
            var down = new Vector2(pos.X, pos.Y - 1);
            var left = new Vector2(pos.X - 1, pos.Y);

            Vector2[] positions = {up, right, down, left};
            foreach (var nextPos in positions)
            {
                // bool avail = ShouldHoeThisTile(location, hut, nextPos, mode);
                // bool inRadius = Util.IsWithinRadius(hut, nextPos);
                // Monitor.Log($" HoeAroundTrees IsActionAvailable around [{pos.X} {pos.Y}]: [{nextPos.X} {nextPos.Y}] should hoe: {avail} in radius: {inRadius}", LogLevel.Debug);

                if (!Util.IsWithinRadius(location, Util.GetHutFromId(guid), nextPos)) continue;
                if (ShouldHoeThisTile(location, hut, nextPos, mode))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldHoeThisTile(GameLocation location, JunimoHut hut, Vector2 pos, string mode)
        {
            if (!CanHoeThisTile(location, pos))
            {
                // Monitor.Log($"    ShouldHoeThisTile [{pos.X} {pos.Y}] mode {mode}: cannot hoe tile", LogLevel.Debug);
                return false;
            }

            if (mode == Modes.Orchard)
            {
                // might we one day want to plant a fruit tree here?
                if (PlantFruitTreesAbility.ShouldPlantFruitTreeOnTile(location, hut, pos)) return false;
                // might we one day want to plant a fruit tree adjacent?
                if (PlantFruitTreesAbility.TileIsNextToAPlantableTile(location, hut, pos)) return false;
            }

            // might we one day want to plant a wild tree here?
            if (mode == Modes.Forest && PlantTreesAbility.ShouldPlantWildTreeHere(location, hut, pos))
            {
                // Monitor.Log($"    ShouldHoeThisTile not hoeing [{pos.X} {pos.Y}] because wild tree plantable", LogLevel.Debug);
                return false;
            }

            // would planting out this tile stop a fruit tree from growing?
            for (var x = -1; x < 2; x++)
            {
                for (var y = -1; y < 2; y++)
                {
                    var v = new Vector2(pos.X + x, pos.Y + y);
                    if (location.terrainFeatures.ContainsKey(v) && IsFruitTreeSapling(location.terrainFeatures[v]))
                    {
                        // Monitor.Log($"    ShouldHoeThisTile not hoeing [{pos.X} {pos.Y}] because fruit tree adjacent", LogLevel.Debug);
                        return false;
                    }
                }
            }

            // is there something to plant here if we hoe it?
            if (!SeedsAvailableToPlantThisTile(location, hut, pos, Util.GetHutIdFromHut(hut)))
            {
                // Monitor.Log($"    ShouldHoeThisTile not hoeing [{pos.X} {pos.Y}] because no seeds", LogLevel.Debug);
                return false;
            }

            if (mode != Modes.Orchard)
            {
                // Monitor.Log($"    ShouldHoeThisTile: hoeing [{pos.X} {pos.Y}] because not orchard", LogLevel.Debug);
                return true;
            }

            // is this tile next to a grown tree, which is the only situation we'll hoe ground in an orchard?
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    Vector2 v = new Vector2(pos.X + x, pos.Y + y);
                    // Monitor.Log($"    ShouldHoeThisTile: hoeing [{pos.X} {pos.Y}] because adjacent full grown orchard tree", LogLevel.Debug);
                    if (location.terrainFeatures.ContainsKey(v) && IsMatureFruitTree(location.terrainFeatures[v]))
                        return true;
                }
            }

            // Monitor.Log($"    ShouldHoeThisTile: not hoeing [{pos.X} {pos.Y}] because not adjacent full grown orchard tree", LogLevel.Debug);
            return false;
        }

        private static bool IsMatureFruitTree(TerrainFeature tf)
        {
            return tf is FruitTree tree && tree.growthStage.Value >= 4;
        }

        private static bool IsFruitTreeSapling(TerrainFeature tf)
        {
            return tf is FruitTree tree && tree.growthStage.Value < 4;
        }

        private bool SeedsAvailableToPlantThisTile(GameLocation location, JunimoHut hut, Vector2 pos, Guid guid)
        {
            Item foundItem;

            // todo: this section is potentially slow and might be refined
            if (ModEntry.BJApi is null)
            {
                Monitor.Log($"SeedsAvailableToPlantThisTile: Better Junimos API not available", LogLevel.Error);
                return false;
            }

            Chest chest = hut.GetOutputChest();
            if (ModEntry.BJApi.GetCropMapForHut(guid) is null)
            {
                foundItem = PlantableSeed(location, chest);
                return (foundItem is not null);
            }

            var cropType = ModEntry.BJApi.GetCropMapForHut(guid).CropTypeAt(hut, pos);
            foundItem = PlantableSeed(location, chest, cropType);
            return foundItem is not null;
        }

        /// <summary>Get an item from the chest that is a crop seed, plantable in this season</summary>
        private Item PlantableSeed(GameLocation location, Chest chest, string cropType = null)
        {
            List<Item> foundItems = chest.Items.ToList().FindAll(item =>
                item is {Category: SObject.SeedsCategory} && !WildTreeSeeds.Contains(item.ItemId)
            );

            if (cropType == CropTypes.Trellis)
            {
                foundItems = foundItems.FindAll(IsTrellisCrop);
            }
            else if (cropType == CropTypes.Ground)
            {
                foundItems = foundItems.FindAll(item => !IsTrellisCrop(item));
            }

            if (foundItems.Count == 0) return null;
            if (location.IsGreenhouse) return foundItems.First();

            foreach (Item foundItem in foundItems)
            {
                // TODO: check if item can grow to harvest before end of season
                if (foundItem.ParentSheetIndex == SunflowerSeeds && Game1.IsFall && Game1.dayOfMonth >= 25)
                {
                    // there is no way that a sunflower planted on Fall 25 will grow to harvest
                    continue;
                }

                var key = foundItem.ItemId;
                try
                {
                    if (cropSeasons[Game1.currentSeason][key])
                    {
                        return foundItem;
                    }
                }
                catch (KeyNotFoundException)
                {
                    Monitor.Log($"Cache miss: {foundItem.ItemId} {Game1.currentSeason}", LogLevel.Trace);
                    Game1.cropData.TryGetValue(foundItem.ItemId, out CropData cropData);
                    cropSeasons[Game1.currentSeason][key] = cropData.Seasons.Contains(location.GetSeason());
                    if (cropSeasons[Game1.currentSeason][key])
                    {
                        return foundItem;
                    }
                }
            }

            return null;
        }

        private static bool IsTrellisCrop(Item item)
        {
            Game1.cropData.TryGetValue(item.ItemId, out CropData cropData);
            return cropData is not null && cropData.IsRaised;
        }

        private static bool CanHoeThisTile(GameLocation farm, Vector2 pos)
        {
            // is this tile plain dirt?
            if (farm.IsTileOccupiedBy(pos)) return false;
            if (Util.IsOccupied(farm, pos)) return false;
            if (!Util.CanBeHoed(farm, pos)) return false;
            if (farm.doesTileHaveProperty((int) pos.X, (int) pos.Y, "Diggable", "Back") != null) return true;
            return false;
        }

        public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid)
        {
            var hut = Util.GetHutFromId(guid);
            var mode = Util.GetModeForHut(Util.GetHutFromId(guid));
            if (mode == Modes.Normal) return false;

            var (x, y) = pos;
            var up = new Vector2(x, y + 1);
            var right = new Vector2(x + 1, y);
            var down = new Vector2(x, y - 1);
            var left = new Vector2(x - 1, y);

            var direction = 0;
            Vector2[] positions = {up, right, down, left};
            foreach (var nextPos in positions)
            {
                if (!Util.IsWithinRadius(location, hut, nextPos)) continue;
                if (!ShouldHoeThisTile(location, hut, nextPos, mode)) continue;
                junimo.faceDirection(direction);
                if (UseToolOnTileManual(location, nextPos)) return true;
                direction++;
            }

            return false;
        }

        private static bool UseToolOnTileManual(GameLocation location, Vector2 tileLocation)
        {
            location.makeHoeDirt(tileLocation);
            if (Utility.isOnScreen(Utility.Vector2ToPoint(tileLocation), 64, location))
            {
                location.playSound("hoeHit");
            }

            location.checkForBuriedItem((int) tileLocation.X, (int) tileLocation.Y, false, false, Game1.player);
            return true;
        }

        public List<string> RequiredItems()
        {
            return new();
        }


        /* older API compat */
        public bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid)
        {
            return IsActionAvailable((GameLocation) farm, pos, guid);
        }

        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Guid guid)
        {
            return PerformAction((GameLocation) farm, pos, junimo, guid);
        }
    }
}