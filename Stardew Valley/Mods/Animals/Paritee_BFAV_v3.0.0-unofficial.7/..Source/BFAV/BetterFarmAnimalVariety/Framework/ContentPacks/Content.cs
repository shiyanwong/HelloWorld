﻿using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BetterFarmAnimalVariety.Framework.ContentPacks
{
    class Content
    {
        public List<ContentPacks.Category> Categories;

        public Content() { }

        public Content(List<ContentPacks.Category> categories)
        {
            this.Categories = categories;
        }

        public void SetUp(IContentPack contentPack)
        {
            // There should never be multiple occurrences of the same category 
            // in a single content pack
            Helpers.Assert.UniqueValues(this.Categories.Select(o => o.Category).ToList());

            // Go through each category
            foreach (ContentPacks.Category category in this.Categories)
            {
                // Handle the actions
                switch (category.Action)
                {
                    case ContentPacks.Category.Actions.Create:
                        this.HandleCreateAction(contentPack, category);
                        break;

                    case ContentPacks.Category.Actions.Update:
                        this.HandleUpdateAction(contentPack, category);
                        break;

                    case ContentPacks.Category.Actions.Remove:
                        this.HandleRemoveAction(contentPack, category);
                        break;

                    default:
                        throw new NotSupportedException($"{category.Action} is not a valid action");
                }
            }
        }

        private Cache.FarmAnimalType CastSpritesToFullPaths(Cache.FarmAnimalType type, string directoryPath)
        {
            if (type.HasAdultSprite())
            {
                type.Sprites.Adult = Path.Combine(directoryPath, ModEntry.Instance.Helper.Content.NormalizeAssetName(type.Sprites.Adult));
            }

            if (type.HasBabySprite())
            {
                type.Sprites.Baby = Path.Combine(directoryPath, ModEntry.Instance.Helper.Content.NormalizeAssetName(type.Sprites.Baby));
            }

            if (type.HasReadyForHarvestSprite())
            {
                type.Sprites.ReadyForHarvest = Path.Combine(directoryPath, ModEntry.Instance.Helper.Content.NormalizeAssetName(type.Sprites.ReadyForHarvest));
            }

            return type;
        }

        public void HandleCreateAction(IContentPack contentPack, ContentPacks.Category category)
        {
            // Assert unique category
            Helpers.Assert.UniqueFarmAnimalCategory(category.Category);

            // Assert no duplicate types
            Helpers.Assert.UniqueValues(category.Types.Select(o => o.Type).ToList());

            // Modify the sprite paths
            category.Types = category.Types
                .Select(o => this.CastSpritesToFullPaths(o, contentPack.DirectoryPath))
                .ToList();

            // Modify the animal shop icon path
            if (category.CanBePurchased())
            {
                category.AnimalShop.Icon = Path.Combine(contentPack.DirectoryPath, ModEntry.Instance.Helper.Content.NormalizeAssetName(category.AnimalShop.Icon));
            }

            Helpers.FarmAnimals.AddOrReplaceCategory(new Cache.FarmAnimalCategory(category));
        }

        public void HandleUpdateAction(IContentPack contentPack, ContentPacks.Category category)
        {
            // Allow updates to add a new category even if it does not exist
            Cache.FarmAnimalCategory cacheCategory = Helpers.FarmAnimals.GetCategory(category.Category) 
                ?? new Cache.FarmAnimalCategory(category);

            // Add the missing types
            if (category.Types != null)
            {
                // Need to modify the sprite paths
                List<Cache.FarmAnimalType> types = category.Types.Select(o => this.CastSpritesToFullPaths(o, contentPack.DirectoryPath)).ToList();

                if (category.ForceOverrideTypes)
                {
                    cacheCategory.Types = types;
                }
                else
                {
                    foreach (Cache.FarmAnimalType type in types)
                    {
                        Cache.FarmAnimalType cacheCategoryType = cacheCategory.Types.FirstOrDefault(o => o.Type == type.Type);

                        if (cacheCategoryType != null)
                        {
                            cacheCategoryType = type;
                        }
                        else
                        {
                            cacheCategory.Types.Add(type);
                        }
                    }
                }
            }

            // Add the missing buildings
            if (category.Buildings != null)
            {
                cacheCategory.Buildings = category.ForceOverrideBuildings 
                    ? category.Buildings
                    : cacheCategory.Buildings.Union(category.Buildings).ToList();
            }

            // Check if the force remove from shop flag was used
            if (category.ForceRemoveFromShop)
            {
                cacheCategory.AnimalShop = null;
            }
            // Only update the animal shop properties if it was explicitly stated
            else if (category.AnimalShop != null)
            {
                // If the category couldn't be purchased before, set it to be purchased
                if (!cacheCategory.CanBePurchased())
                {
                    cacheCategory.AnimalShop = new Cache.FarmAnimalStock();
                }

                if (category.AnimalShop.Name != null)
                {
                    cacheCategory.AnimalShop.Name = category.AnimalShop.Name;
                }

                if (category.AnimalShop.Description != null)
                {
                    cacheCategory.AnimalShop.Description = category.AnimalShop.Description;
                }

                if (category.AnimalShop.Icon != null)
                {
                    cacheCategory.AnimalShop.Icon = Path.Combine(contentPack.DirectoryPath, ModEntry.Instance.Helper.Content.NormalizeAssetName(category.AnimalShop.Icon));
                }

                if (category.AnimalShop.Exclude != null)
                {
                    cacheCategory.AnimalShop.Exclude = category.ForceOverrideExclude || cacheCategory.AnimalShop.Exclude == null
                        ? category.AnimalShop.Exclude
                        : cacheCategory.AnimalShop.Exclude.Union(category.AnimalShop.Exclude).ToList();
                }
            }

            Helpers.FarmAnimals.AddOrReplaceCategory(cacheCategory);
        }

        public void HandleRemoveAction(IContentPack contentPack, ContentPacks.Category category)
        {
            // Assert unique category
            Helpers.Assert.FarmAnimalCategoryExists(category.Category);

            Helpers.FarmAnimals.RemoveCategory(category.Category);
        }
    }
}
