using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PatiDostum
{
    /// <summary>
    /// Kaydı klasördeki save.json'a yazar; bir önceki sağlam kayıt save.bak'ta durur.
    /// Önce geçici dosyaya yazılır, sonra yer değiştirilir: yazma yarıda kalırsa eski kayıt bozulmaz.
    /// Okurken save.json bozuksa save.bak, o da yoksa yeni oyun.
    /// </summary>
    public class SaveStore
    {
        public const string FileName = "save.json";
        public const string BackupName = "save.bak";
        private const string TempName = "save.tmp";

        private readonly string directory;

        public SaveStore(string directory)
        {
            this.directory = directory;
        }

        private string MainPath => Path.Combine(directory, FileName);
        private string BackupPath => Path.Combine(directory, BackupName);
        private string TempPath => Path.Combine(directory, TempName);

        public SaveData Load(DateTime nowUtc)
        {
            if (TryRead(MainPath, out SaveData data) || TryRead(BackupPath, out data))
                return Migrate(data);
            return SaveData.CreateNew(nowUtc);
        }

        public void Save(SaveData data)
        {
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(TempPath, JsonUtility.ToJson(data));
                if (File.Exists(MainPath))
                {
                    File.Copy(MainPath, BackupPath, true);
                    File.Delete(MainPath);
                }
                File.Move(TempPath, MainPath);
            }
            catch (Exception error)
            {
                Debug.LogError("Kayıt yazılamadı: " + error.Message);
            }
        }

        private static bool TryRead(string path, out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception error)
            {
                Debug.LogWarning($"Kayıt okunamadı ({Path.GetFileName(path)}): {error.Message}");
                data = null;
            }
            return data != null && data.version > 0;
        }

        /// <summary>Eski sürüm kaydı bugünkü biçime getirir.</summary>
        private static SaveData Migrate(SaveData data)
        {
            if (data.clock == null) data.clock = new ClockState();
            if (data.pets == null) data.pets = new List<PetState>();
            if (data.memorial == null) data.memorial = new List<MemorialEntry>();
            if (data.ownedItems == null) data.ownedItems = new List<string>();
            if (data.ownedProducts == null) data.ownedProducts = new List<string>();
            if (data.grantedTokens == null) data.grantedTokens = new List<string>();
            if (data.selectedPet == null) data.selectedPet = "";
            if (data.taskIds == null) data.taskIds = new List<string>();
            if (data.taskCounts == null) data.taskCounts = new List<int>();
            if (data.taskClaimed == null) data.taskClaimed = new List<bool>();
            if (data.badges == null) data.badges = new List<string>();
            if (data.passClaimedFree == null) data.passClaimedFree = new List<int>();
            if (data.passClaimedPremium == null) data.passClaimedPremium = new List<int>();
            if (data.ownedThemes == null) data.ownedThemes = new List<string>();
            if (string.IsNullOrEmpty(data.theme)) data.theme = "classic";
            // 2 → 3 (ve her yüklemede): kıyafet yuvaları ve komut listesi boş olmasın
            foreach (PetState pet in data.pets)
            {
                if (pet.commands == null) pet.commands = new List<CommandProgress>();
                pet.parents = pet.parents ?? "";
                if (pet.generation < 1) pet.generation = 1;
                pet.hat = pet.hat ?? "";
                pet.glasses = pet.glasses ?? "";
                pet.neck = pet.neck ?? "";
                pet.outfit = pet.outfit ?? "";
                pet.feet = pet.feet ?? "";
            }

            // 1 → 2: hayvanlara ömür, hastalık ve rastgele sayı durumu
            if (data.version < 2)
            {
                foreach (PetState pet in data.pets)
                {
                    if (pet.lifespanYears <= 0.0) pet.lifespanYears = SpeciesCatalog.Get(pet.species).lifespanYears;
                    if (pet.disease == null) pet.disease = "";
                    if (pet.deathCause == null) pet.deathCause = "";
                    if (pet.rng == 0) pet.rng = (uint)pet.id.GetHashCode() | 1u;
                }
                data.adoptions = Math.Max(data.adoptions, data.pets.Count);
            }

            if (data.version < SaveData.CurrentVersion) data.version = SaveData.CurrentVersion;
            return data;
        }
    }
}
