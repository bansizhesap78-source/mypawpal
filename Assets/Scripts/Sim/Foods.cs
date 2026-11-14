namespace PatiDostum
{
    /// <summary>
    /// Bir yiyeceğin ölçümlere etkisi (puan) ve fiyatı (altın). Her türün ilk yiyeceği bedava temel yemdir:
    /// çocuk altını bittiği için dostunu aç bırakmak zorunda kalmaz (plan §7b).
    /// </summary>
    public sealed class Food
    {
        public string id;
        public double fullness;
        public double fun;
        public double weight;
        public double love;
        /// <summary>Yemekten sonra tuvalet ihtiyacı (rahatlık bu kadar düşer).</summary>
        public double toilet;
        /// <summary>Şeker: PetState.candy'ye eklenir; çok olursa diş ağrıtabilir.</summary>
        public double sugar;
        public int price;

        public string NameKey => "food_" + id;
    }

    public static class FoodCatalog
    {
        public static readonly Food[] All =
        {
            // Temel yemler (bedava)
            new Food { id = "pet_food", fullness = 35, toilet = 10, love = 1 },
            new Food { id = "seeds", fullness = 30, toilet = 8, love = 1 },
            new Food { id = "hay", fullness = 30, toilet = 9, love = 1 },
            new Food { id = "flakes", fullness = 35, love = 1 },
            new Food { id = "greens", fullness = 28, fun = 2, toilet = 8, love = 1 },
            new Food { id = "meat", fullness = 35, toilet = 10, love = 1 },
            new Food { id = "bamboo", fullness = 32, toilet = 9, love = 1 },
            new Food { id = "acacia", fullness = 30, fun = 2, toilet = 8, love = 1 },
            new Food { id = "krill", fullness = 32, toilet = 8, love = 1 },
            new Food { id = "fern", fullness = 30, fun = 2, toilet = 8, love = 1 },
            new Food { id = "corn", fullness = 30, toilet = 8, love = 1 },
            new Food { id = "millet", fullness = 28, toilet = 7, love = 1 },
            new Food { id = "sunflower", fullness = 30, toilet = 8, love = 1 },
            new Food { id = "cricket", fullness = 30, toilet = 8, love = 1 },
            // Lezzetliler (altınla)
            new Food { id = "fish", fullness = 25, fun = 6, toilet = 8, love = 2, price = 8 },
            new Food { id = "carrot", fullness = 20, fun = 6, toilet = 6, love = 2, price = 5 },
            new Food { id = "bone", fullness = 10, fun = 12, weight = 1, toilet = 3, love = 2, price = 6 },
            new Food { id = "worm", fullness = 22, fun = 6, toilet = 6, love = 2, price = 6 },
            new Food { id = "fruit", fullness = 15, fun = 8, toilet = 5, love = 2, sugar = 0.5, price = 5 },
            new Food { id = "steak", fullness = 30, fun = 8, toilet = 8, love = 2, price = 9 },
            new Food { id = "strawberry", fullness = 15, fun = 8, toilet = 5, love = 2, sugar = 0.5, price = 5 },
            new Food { id = "cucumber", fullness = 18, fun = 6, toilet = 5, love = 2, price = 4 },
            new Food { id = "honey", fullness = 12, fun = 10, weight = 2, toilet = 3, love = 3, sugar = 1, price = 7 },
            new Food { id = "treat", fullness = 8, fun = 10, weight = 2, toilet = 3, love = 3, sugar = 1, price = 3 },
        };

        public static Food Get(string id)
        {
            foreach (Food food in All)
                if (food.id == id) return food;
            return null;
        }
    }
}
