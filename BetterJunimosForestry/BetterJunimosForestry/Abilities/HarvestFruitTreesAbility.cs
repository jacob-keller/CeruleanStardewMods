using System;
using System.Collections.Generic;
using System.Linq;
using BetterJunimos.Abilities;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using StardewModdingAPI;
using SObject = StardewValley.Object;

namespace BetterJunimosForestry.Abilities {
    public class HarvestFruitTreesAbility : IJunimoAbility {
        private readonly IMonitor Monitor;

        internal HarvestFruitTreesAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
        }
        
        public string AbilityName() {
            return "HarvestFruitTrees";
        }

        private static bool IsHarvestableFruitTree(TerrainFeature tf) {
            return tf is FruitTree tree && tree.fruit.Any();
        }

        public bool IsActionAvailable(GameLocation location, Vector2 pos, Guid guid) {
            var up = new Vector2(pos.X, pos.Y + 1);
            var right = new Vector2(pos.X + 1, pos.Y);
            var down = new Vector2(pos.X, pos.Y - 1);
            var left = new Vector2(pos.X - 1, pos.Y);

            Vector2[] positions = { up, right, down, left };
            return positions
                .Where(nextPos => Util.IsWithinRadius(location, Util.GetHutFromId(guid), nextPos))
                .Any(nextPos => location.terrainFeatures.ContainsKey(nextPos) 
                                && IsHarvestableFruitTree(location.terrainFeatures[nextPos]));
        }

        public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            var up = new Vector2(pos.X, pos.Y + 1);
            var right = new Vector2(pos.X + 1, pos.Y);
            var down = new Vector2(pos.X, pos.Y - 1);
            var left = new Vector2(pos.X - 1, pos.Y);

            var direction = 0;
            Vector2[] positions = { up, right, down, left };
            foreach (var nextPos in positions) {
                if (!Util.IsWithinRadius(location, Util.GetHutFromId(guid), nextPos)) continue;
                if (location.terrainFeatures.ContainsKey(nextPos) && IsHarvestableFruitTree(location.terrainFeatures[nextPos])) {
                    var tree = location.terrainFeatures[nextPos] as FruitTree;

                    junimo.faceDirection(direction);

                    return HarvestFromTree(pos, junimo, tree);
                }
                direction++;
            }

            return false;
        }

        private static bool HarvestFromTree(Vector2 pos, JunimoHarvester junimo, FruitTree tree) {
            // do nothing if the tree has no item
            if (!tree.fruit.Any())
                return false;

            // take all the item first
            foreach (var item in tree.fruit)
                junimo.tryToAddItemToHut(item);
            tree.fruit.Clear();
            // shake the tree after
            tree.performUseAction(pos);
            return true;
        }

        public List<string> RequiredItems() {
            return new();
        }
        
        
        /* older API compat */
        public bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid) {
            return IsActionAvailable((GameLocation) farm, pos, guid);
        }
        
        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            return PerformAction((GameLocation) farm, pos, junimo, guid);
        }
    }
}