using System;
using UnityEngine;

namespace PatiDostum
{
    /// <summary>
    /// Oyunun tek kaydını taşır, zamanı ölçer ve hayvanları işletir. Boot sahnesinde kurulur ve sahneler arasında
    /// yaşar; Boot olmadan açılan sahnelerde (editörde doğrudan oynatma) ilk erişimde kendini kurar.
    ///  - Oyun açıkken her saniye geçen süre kadar işletir, 30 saniyede bir kaydeder.
    ///  - Arka plana giderken ölçüp kaydeder; geri gelince uzakta geçen süreyi (en fazla 72 saat) işletir.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        private const float TickInterval = 1f;
        private const float SaveInterval = 30f;

        private static GameSession instance;

        private SaveStore store;
        private SaveData data;
        private bool suspended;
        private float tickTimer, saveTimer;

        /// <summary>Son ölçümde uygulama dışında geçen gerçek süre (sn). İlk açılışta 0.</summary>
        public static double LastAwaySeconds { get; private set; }

        /// <summary>Uzakta geçen süre işletilince tetiklenir (açılış ve uygulamaya dönüş).</summary>
        public static event Action Advanced;

        /// <summary>Az önce ölen dost (Anı Bahçesi'nde veda mesajı gösterilene kadar); yoksa null.</summary>
        public static MemorialEntry LastDeath { get; private set; }

        /// <summary>Bir dost öldüğünde tetiklenir (oyun açıkken).</summary>
        public static event Action PetDied;

        public static SaveData Data => Instance.data;

        /// <summary>Odada gösterilen dost (seçili olan; yoksa ilki); hiç dost yoksa null.</summary>
        public static PetState CurrentPet
        {
            get
            {
                SaveData data = Data;
                if (data.pets.Count == 0) return null;
                foreach (PetState pet in data.pets)
                    if (pet.id == data.selectedPet) return pet;
                return data.pets[0];
            }
        }

        public static void SelectPet(PetState pet)
        {
            Data.selectedPet = pet.id;
            Save();
        }

        private static GameSession Instance
        {
            get
            {
                if (instance == null) new GameObject("GameSession").AddComponent<GameSession>();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            store = new SaveStore(Application.persistentDataPath);
            data = store.Load(DateTime.UtcNow);
            Resume();
        }

        private void Update()
        {
            if (suspended) return;
            tickTimer += Time.unscaledDeltaTime;
            if (tickTimer >= TickInterval)
            {
                tickTimer = 0f;
                double seconds = GameClock.Measure(data.clock, DateTime.UtcNow, DeviceClock.ElapsedMs());
                Simulate(seconds);
            }

            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer >= SaveInterval)
            {
                saveTimer = 0f;
                store.Save(data);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            // Unity açılışta da OnApplicationPause(false) çağırır; o çağrı uzaktaki süreyi sıfırlamasın
            if (paused == suspended) return;
            if (paused) Suspend();
            else Resume();
        }

        private void OnApplicationQuit()
        {
            Suspend();
        }

        /// <summary>Oyun içinde geçen süreyi işletir ve kaydeder; dönüşte yalnızca uzaktaki süre kalır.</summary>
        private void Suspend()
        {
            suspended = true;
            Simulate(GameClock.Measure(data.clock, DateTime.UtcNow, DeviceClock.ElapsedMs()));
            store.Save(data);
            // Uygulama kapalıyken dostun ihtiyaçlarını yerel bildirimle hatırlat (pansiyondayken gerek yok)
            if (data.notificationsOn && !Shop.IsBoarding(data, DateTime.UtcNow))
                Notifications.Schedule(NotificationPlanner.Plan(data.pets, DateTime.Now, data.noDeath));
        }

        private void Resume()
        {
            suspended = false;
            Notifications.CancelAll();
            LastAwaySeconds = GameClock.Measure(data.clock, DateTime.UtcNow, DeviceClock.ElapsedMs());
            Simulate(GameClock.CatchUpSeconds(LastAwaySeconds));
            store.Save(data);
            Advanced?.Invoke();
        }

        /// <summary>Şu ana kadar geçen <paramref name="realSeconds"/> süreyi bütün hayvanlara uygular.</summary>
        private void Simulate(double realSeconds)
        {
            if (realSeconds <= 0.0) return;
            DateTime nowUtc = DateTime.UtcNow;
            DateTime start = DateTime.Now.AddSeconds(-realSeconds);

            // Aralığın pansiyona denk gelen başı yalnızca yaşlandırır, kalanı normal işler
            double boarded = 0.0;
            if (data.boardingUntilUtcTicks > 0)
            {
                DateTime startUtc = nowUtc.AddSeconds(-realSeconds);
                DateTime until = new DateTime(Math.Min(data.boardingUntilUtcTicks, nowUtc.Ticks), DateTimeKind.Utc);
                boarded = Math.Max(0.0, (until - startUtc).TotalSeconds);
            }

            foreach (PetState pet in data.pets)
            {
                PetSimulator.AgeOnly(pet, boarded);
                PetSimulator.Simulate(pet, start.AddSeconds(boarded), realSeconds - boarded, data.noDeath);
            }
            BuryDead();
        }

        /// <summary>Ölen dostları Anı Bahçesi'ne taşır.</summary>
        private void BuryDead()
        {
            for (int i = data.pets.Count - 1; i >= 0; i--)
            {
                PetState pet = data.pets[i];
                if (!pet.dead) continue;
                var entry = new MemorialEntry
                {
                    name = pet.name,
                    species = pet.species,
                    color = pet.color,
                    mode = pet.mode,
                    ageDays = pet.AgeDays,
                    adoptedUtcTicks = pet.adoptedUtcTicks,
                    diedUtcTicks = DateTime.UtcNow.Ticks,
                    cause = pet.deathCause,
                    rescues = pet.rescues
                };
                data.memorial.Add(entry);
                data.pets.RemoveAt(i);
                LastDeath = entry;
                store.Save(data);
                PetDied?.Invoke();
            }
        }

        public static void Save()
        {
            Instance.store.Save(Instance.data);
        }

        /// <summary>Veda mesajı gösterildi.</summary>
        public static void ClearLastDeath() => LastDeath = null;

        /// <summary>Barınağa gönder (bilet ya da 10 elmas); veda mesajı Anı Bahçesi'nde "yeni ailesine gitti" olur.</summary>
        public static bool SendToShelter(PetState pet)
        {
            MemorialEntry entry = Shop.SendToShelter(Data, pet, DateTime.UtcNow);
            if (entry == null) return false;
            LastDeath = entry;
            Save();
            return true;
        }

        /// <summary>
        /// Hayvanı sahiplenir ve hemen kaydeder. İlkinden sonraki her yeni dostla 3 mavi elmas hediye gelir
        /// (plan §6: normal oynayan oyuncunun kurtarmaya yetecek elması olsun).
        /// </summary>
        public static PetState Adopt(Species species, string name, int color, SpeedMode mode)
        {
            PetState pet = PetState.Adopt(species, name, color, mode, DateTime.UtcNow);
            Data.pets.Add(pet);
            Data.selectedPet = pet.id;
            Data.adoptions++;
            if (Data.adoptions > 1) Data.diamonds += SaveData.NewPetDiamonds;
            Save();
            return pet;
        }

        /// <summary>Ayar değişince bildirimleri hemen iptal eder (açarken bir şey yapmaz; kapanışta planlanır).</summary>
        public static void SetNotifications(bool on)
        {
            Data.notificationsOn = on;
            if (on) Notifications.RequestPermission();
            else Notifications.CancelAll();
            Save();
        }
    }
}
