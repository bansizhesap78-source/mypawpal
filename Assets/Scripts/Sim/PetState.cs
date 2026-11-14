using System;
using System.Collections.Generic;

namespace PatiDostum
{
    /// <summary>
    /// Bir hayvanın kaydedilen durumu. Ölçümlerin hepsi 0–100 ve "yüksek iyi": tokluk, su, enerji, temizlik,
    /// tuvalet rahatlığı (0 olunca kaza olur), eğlence, sağlık, sevgi. Kilo 0–100, 50 idealdir.
    /// </summary>
    [Serializable]
    public class PetState
    {
        public string id;
        public string species;
        public string name;
        public int color;
        /// <summary>SpeedMode değeri (1 ya da 5).</summary>
        public int mode = (int)SpeedMode.Real;
        public long adoptedUtcTicks;
        /// <summary>Hayvanın yaşı, oyun saniyesi (hızlı modda gerçek sürenin 5 katı).</summary>
        public double ageSeconds;
        /// <summary>Bu hayvanın ömrü (yıl): tür ortalamasının %90–110'u, sahiplenirken belirlenir.</summary>
        public double lifespanYears;

        public double fullness = 80;
        public double thirst = 80;
        public double energy = 80;
        public double hygiene = 90;
        public double toilet = 80;
        public double fun = 70;
        public double health = 100;
        public double love = 30;
        public double weight = 50;

        public bool asleep;
        /// <summary>Çocuk uyandırdıysa bu kadar gerçek saniye gece kendiliğinden uyumaz (oynamak engellenmesin).</summary>
        public double stayAwakeSeconds;
        /// <summary>Tuvalet kazası: temizlenene kadar yerde durur, temizlik ve mutluluğu düşürür.</summary>
        public bool poopOnFloor;

        // Sağlık (M2)
        /// <summary>Hastalık id'si (DiseaseCatalog); boşsa sağlıklı.</summary>
        public string disease = "";
        /// <summary>Aşının geçerli olduğu yaş (oyun saniyesi); yaş bunun altındaysa aşılı.</summary>
        public double vaccineUntilAge;
        /// <summary>Sağlık kritik çizginin altındayken geçen gerçek süre (sn); düzelince sıfırlanır.</summary>
        public double criticalSeconds;
        /// <summary>Şeker yükü: her ödül +1, saatte azalır; yüksekken ödül diş ağrıtabilir.</summary>
        public double candy;
        /// <summary>Mucize Tedavi ile kaç kez kurtarıldı (ilki bedava, sonrakiler pahalanır).</summary>
        public int rescues;
        /// <summary>Bedava acil müdahalenin en son kullanıldığı gün (yerel tarih, gün sayısı).</summary>
        public long freeTreatDay;
        /// <summary>Hastalık zarları için hayvana özel rastgele sayı durumu (testlerde tekrarlanabilir).</summary>
        public uint rng = 2463534242;

        // Kıyafetler (ClothingCatalog id; boşsa o yuva boş) ve öğrenilen komutlar (M3)
        public string hat = "";
        public string glasses = "";
        public string neck = "";
        public string outfit = "";
        public string feet = "";
        public List<CommandProgress> commands = new List<CommandProgress>();

        // Soy ağacı (M5)
        /// <summary>Anne-babasının adları ("Pamuk & Boncuk"); sahiplenilmişse boş.</summary>
        public string parents = "";
        /// <summary>Kuşak: sahiplenilen 1, yavrusu 2...</summary>
        public int generation = 1;
        /// <summary>En son yavrusunun olduğu yaş (oyun sn); 0 ise hiç olmadı.</summary>
        public double bredAtAge;

        public bool dead;
        /// <summary>"natural" (ömrünü tamamladı) ya da "neglect" (tedavi edilmedi).</summary>
        public string deathCause = "";

        public SpeedMode Mode => (SpeedMode)mode;
        public double AgeDays => ageSeconds / 86400.0;
        public bool IsSick => !string.IsNullOrEmpty(disease);

        public static PetState Adopt(Species species, string name, int color, SpeedMode mode, DateTime nowUtc)
        {
            var pet = new PetState
            {
                id = Guid.NewGuid().ToString("N"),
                species = species.id,
                name = name,
                color = color,
                mode = (int)mode,
                adoptedUtcTicks = nowUtc.Ticks,
                ageSeconds = species.adoptAgeDays * 86400.0
            };
            pet.rng = (uint)pet.id.GetHashCode() | 1u;
            pet.lifespanYears = species.lifespanYears * (0.9 + 0.2 * pet.NextRandom());
            // Suda yaşayan susamaz, tuvaleti olmayanın tuvalet ölçümü hep dolu kalır
            if (!species.drinks) pet.thirst = 100;
            if (!species.hasToilet) pet.toilet = 100;
            return pet;
        }

        /// <summary>Kopya (bildirim planlayıcı geleceği bunun üzerinde dener; komut listesi paylaşılır, değiştirilmez).</summary>
        public PetState Clone() => (PetState)MemberwiseClone();

        /// <summary>0 ile 1 arasında rastgele sayı (xorshift32, durumu kayıtta).</summary>
        public double NextRandom()
        {
            uint x = rng;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            rng = x == 0 ? 1u : x;
            return rng / 4294967296.0;
        }
    }
}
