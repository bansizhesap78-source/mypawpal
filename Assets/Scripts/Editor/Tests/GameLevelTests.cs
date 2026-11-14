using System;
using NUnit.Framework;

namespace PatiDostum.Tests
{
    /// <summary>Mini oyun seviyeleri, türe uygun oyunlar ve aile paketi.</summary>
    public class GameLevelTests
    {
        private static readonly DateTime Now = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

        private static SaveData NewSave() => SaveData.CreateNew(Now);

        [Test]
        public void Levels_StartAtOne_RiseWithTwoStars_NeverDrop()
        {
            SaveData save = NewSave();
            Assert.AreEqual(1, GameLevels.Get(save, MiniGame.Memory));
            LevelOutcome low = GameLevels.Complete(save, MiniGame.Hide, 1);
            Assert.IsFalse(low.leveledUp);
            Assert.AreEqual(1, GameLevels.Get(save, MiniGame.Hide));
            int gold = save.gold;
            LevelOutcome good = GameLevels.Complete(save, MiniGame.Hide, 5);
            Assert.IsTrue(good.leveledUp);
            Assert.AreEqual(3, good.stars);
            Assert.AreEqual(2, GameLevels.Get(save, MiniGame.Hide));
            Assert.AreEqual(gold + GameLevels.LevelGold(2), save.gold, "seviye atlayınca bedava altın");
            GameLevels.Complete(save, MiniGame.Hide, 0);
            Assert.AreEqual(2, GameLevels.Get(save, MiniGame.Hide), "seviye geri düşmez");
            Assert.AreEqual(1, GameLevels.Get(save, MiniGame.Fetch), "her oyunun seviyesi ayrı");
        }

        [Test]
        public void EveryFifthLevel_GivesADiamond()
        {
            SaveData save = NewSave();
            int diamonds = save.diamonds;
            for (int i = 0; i < 4; i++) GameLevels.Complete(save, MiniGame.Hide, 5);
            Assert.AreEqual(5, GameLevels.Get(save, MiniGame.Hide));
            Assert.AreEqual(diamonds + 1, save.diamonds);
        }

        [Test]
        public void Difficulty_GrowsWithLevel()
        {
            Assert.AreEqual(4, GameLevels.MemoryPairs(1));
            Assert.Greater(GameLevels.MemoryPairs(3), GameLevels.MemoryPairs(1));
            Assert.AreEqual(MiniGameRules.MemoryMaxPairs, GameLevels.MemoryPairs(50));
            Assert.AreEqual(3, GameLevels.HideBoxes(1));
            Assert.AreEqual(5, GameLevels.HideBoxes(20));
            Assert.Greater(GameLevels.FetchSpeed(5), GameLevels.FetchSpeed(1));
            Assert.Less(GameLevels.FetchZone(8), GameLevels.FetchZone(1));
            Assert.AreEqual(8, GameLevels.FetchThrows(50));
            Assert.AreEqual(3, GameLevels.FetchStars(0.4f, 0.4f, 9));
            Assert.AreEqual(0, GameLevels.FetchStars(0.9f, 0.1f, 1));
        }

        [Test]
        public void Rewards_GrowWithLevel()
        {
            Assert.Greater(Economy.Reward(MiniGame.Catch, 40, 0, 6), Economy.Reward(MiniGame.Catch, 40, 0, 1));
        }

        [Test]
        public void EveryScoreHasGold_PlayingIsAlwaysWorthIt()
        {
            foreach (MiniGame game in GameLevels.AllGames)
            {
                Assert.Greater(Economy.Reward(game, GameLevels.GreatScore(game, 1), 0), 0, game.ToString());
                Assert.Greater(Economy.Reward(game, GameLevels.GreatScore(game, 1), Economy.FullPlaysPerDay), 0, game.ToString());
            }
        }

        [Test]
        public void EverySpecies_EatsItsOwnFood_AndHasAName()
        {
            foreach (Species species in SpeciesCatalog.All)
            {
                Assert.AreNotEqual(species.NameKey, Loc.Get(species.NameKey), species.id);
                Assert.GreaterOrEqual(species.foods.Length, 2, species.id);
                foreach (string id in species.foods)
                {
                    Food food = FoodCatalog.Get(id);
                    Assert.IsNotNull(food, species.id + ": " + id);
                    Assert.AreNotEqual(food.NameKey, Loc.Get(food.NameKey), id);
                }
                foreach (string command in species.commands)
                    Assert.AreNotEqual("cmd_" + command, Loc.Get("cmd_" + command), command);
            }
            Assert.AreEqual("bamboo", SpeciesCatalog.Get("panda").foods[0]);
            Assert.AreEqual("ice", SpeciesCatalog.Get("penguin").habitat);
        }

        [Test]
        public void WorstGame_StillPaysSomething()
        {
            SaveData save = NewSave();
            PetState pet = PetState.Adopt(SpeciesCatalog.Get("cat"), "Pamuk", 0, SpeedMode.Real, Now);
            int gold = save.gold;
            Assert.AreEqual(Economy.MinPlayGold, Economy.FinishGame(save, pet, MiniGame.Bubble, 0, Now.ToLocalTime()));
            Assert.AreEqual(gold + Economy.MinPlayGold, save.gold);
        }

        [Test]
        public void ZeroScore_GivesNoStar_AndNoLevelUp_ButAnyScoreGivesOne()
        {
            foreach (MiniGame game in GameLevels.AllGames)
            {
                Assert.AreEqual(0, GameLevels.Stars(game, 1, 0), game.ToString());
                Assert.AreEqual(1, GameLevels.Stars(game, 1, 1), game.ToString());
            }
            SaveData save = NewSave();
            LevelOutcome none = GameLevels.Complete(save, MiniGame.Bubble, 0);
            Assert.AreEqual(0, none.stars);
            Assert.IsFalse(none.leveledUp);
        }

        [Test]
        public void Fish_HasNoFetch_ButHasOtherGames_AndItsOwnHome()
        {
            Species fish = SpeciesCatalog.Get("goldfish");
            Assert.AreEqual(MoveKind.Swim, fish.moves);
            Assert.AreEqual("aquarium", fish.habitat);
            Assert.IsFalse(GameLevels.Available(fish, MiniGame.Fetch));
            Assert.IsTrue(GameLevels.Available(fish, MiniGame.Bubble));
            Assert.IsTrue(GameLevels.Available(SpeciesCatalog.Get("dog"), MiniGame.Fetch));
            Assert.AreEqual(MoveKind.Fly, SpeciesCatalog.Get("budgie").moves);
            foreach (Species species in SpeciesCatalog.All) Assert.IsNotEmpty(species.habitat, species.id);
        }

        [Test]
        public void FamilyPack_UnlocksAllSlots_IsCheaperThanOneByOne()
        {
            SaveData save = NewSave();
            Product pack = ProductCatalog.Get(ProductCatalog.FamilyPack);
            Assert.IsTrue(Shop.CanBuy(save, pack, Now));
            Assert.Greater(Shop.BundleSaving(save, pack), 0);
            Shop.Grant(save, pack, "a", Now);
            Assert.AreEqual(Shop.MaxPets, Shop.Slots(save));
            Assert.IsFalse(Shop.CanBuy(save, ProductCatalog.Get(ProductCatalog.Slot2), Now));
            Assert.IsTrue(Shop.Owned(save, ProductCatalog.Get(ProductCatalog.Slot5), Now));
            Assert.IsFalse(Shop.StarterAvailable(save, Now));
        }

        [Test]
        public void FamilyPack_HiddenWhenBuyingOneByOneIsCheaper()
        {
            SaveData save = NewSave();
            Shop.Grant(save, ProductCatalog.Get(ProductCatalog.Slot2), "a", Now);
            Shop.Grant(save, ProductCatalog.Get(ProductCatalog.Slot3), "b", Now);
            Assert.IsFalse(Shop.CanBuy(save, ProductCatalog.Get(ProductCatalog.FamilyPack), Now), "kalan iki yer paketten ucuz");
        }

        [Test]
        public void Starter_ShowsRealSaving()
        {
            Assert.Greater(Shop.BundleSaving(NewSave(), ProductCatalog.Get(ProductCatalog.StarterPack)), 0);
        }
    }
}
