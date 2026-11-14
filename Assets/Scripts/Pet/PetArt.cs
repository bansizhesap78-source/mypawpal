using System;
using System.Collections.Generic;
using UnityEngine;

namespace PatiDostum
{
    /// <summary>
    /// Gerçek çizimlerin kütüphanesi. Dost pozları Resources/Pets'ten gerektikçe yüklenir (renk çeşidi: cat_c2_happy);
    /// odalar, ikonlar ve çerçeveler Resources/PetArt.asset'te (SceneBuilder Assets/Art/Rooms ve Icons'tan
    /// doldurur). Anahtar dosya adıdır: "cat_normal", "cat_happy", "cat_sleep", "cat_sick", "room_kitchen".
    /// Çizimi olmayan tür ve oda şekillerden yer tutucuyla gösterilir; görseller parça parça eklenebilir.
    /// </summary>
    public class PetArt : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            public Sprite sprite;
        }

        public Entry[] entries = Array.Empty<Entry>();

        private static PetArt instance;
        private Dictionary<string, Sprite> map;

        private static PetArt Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<PetArt>("PetArt");
                return instance;
            }
        }

        /// <summary>Dost pozları (Resources/Pets) gerektikçe yüklenir; bulunamayanlar da hatırlanır (null).</summary>
        private static readonly Dictionary<string, Sprite> pets = new Dictionary<string, Sprite>();

        public static Sprite Get(string key)
        {
            PetArt art = Instance;
            if (art != null)
            {
                if (art.map == null)
                {
                    art.map = new Dictionary<string, Sprite>();
                    foreach (Entry e in art.entries)
                        if (e.sprite != null) art.map[e.key] = e.sprite;
                }
                if (art.map.TryGetValue(key, out Sprite sprite)) return sprite;
            }
            if (!pets.TryGetValue(key, out Sprite pet))
            {
                pet = Resources.Load<Sprite>("Pets/" + key);
                pets[key] = pet;
            }
            return pet;
        }

        /// <summary>Türün ortamındaki oda ya da arka plan (Resources/Rooms); çizimi yoksa evdeki oda, o da yoksa null.</summary>
        public static Sprite RoomOf(Species species, string room)
        {
            Sprite own = species.habitat != "home" ? Resources.Load<Sprite>("Rooms/" + species.habitat + "_" + room) : null;
            return own != null ? own : Get("room_" + room);
        }

        /// <summary>Tek başına yüklenen arka plan (bg_swim, bg_fly); yoksa null.</summary>
        public static Sprite Backdrop(string key) => Resources.Load<Sprite>("Rooms/" + key);

        /// <summary>Gezinti manzarası türe göre: balık denizde, kuş gökte, penguen karda; ev dostları parkta (null).</summary>
        public static Sprite WalkScene(Species species)
        {
            if (species.moves == MoveKind.Swim) return Backdrop("bg_swim");
            if (species.moves == MoveKind.Fly) return Backdrop("bg_fly");
            if (species.habitat == "home") return null;
            return Backdrop("bg_walk_" + (species.habitat == "burrow" ? "garden" : species.habitat));
        }

        /// <summary>Dostun o anki pozu: uyurken sleep, hastayken sick, mutluyken happy, yoksa normal.</summary>
        public static string Pose(PetState pet)
        {
            if (pet.asleep) return "sleep";
            if (pet.IsSick || PetSimulator.IsCritical(pet)) return "sick";
            MoodKind mood = Mood.Of(pet);
            return mood == MoodKind.Happy || mood == MoodKind.Funny ? "happy" : "normal";
        }

        /// <summary>
        /// Türün pozdaki çizimi. Bebek ve yavru "cat_happy", genç, yetişkin ve yaşlı "cat_adult_happy" kullanır
        /// (yetişkin çizimi yoksa yavrununki). O poz yoksa normal pozu; hiç yoksa null.
        /// </summary>
        public static Sprite ForPet(PetState pet) => ForPet(pet, Pose(pet));

        /// <summary>Türün ve yaşının istenen pozdaki çizimi (mini oyunlarda hep "happy").</summary>
        public static Sprite ForPet(PetState pet, string pose)
        {
            LifeStage stage = Mood.Stage(pet, SpeciesCatalog.Get(pet.species));
            bool adult = stage != LifeStage.Baby && stage != LifeStage.Young;
            // Renk çeşidi varsa o, yoksa asıl renk (0)
            if (pet.color > 0)
            {
                Sprite colored = Pose(pet.species, adult, "_c" + pet.color, pose);
                if (colored != null) return colored;
            }
            return Pose(pet.species, adult, "", pose);
        }

        /// <summary>Sahiplenme ekranı: türün o renkteki yavru çizimi (yoksa asıl renk).</summary>
        public static Sprite Preview(string species, int color, string pose)
        {
            return (color > 0 ? Pose(species, false, "_c" + color, pose) : null) ?? Pose(species, false, "", pose);
        }

        private static Sprite Pose(string species, bool adult, string color, string pose)
        {
            if (adult)
            {
                Sprite grown = Get(species + "_adult" + color + "_" + pose) ?? Get(species + "_adult" + color + "_normal");
                if (grown != null) return grown;
            }
            return Get(species + color + "_" + pose) ?? Get(species + color + "_normal");
        }
    }
}
