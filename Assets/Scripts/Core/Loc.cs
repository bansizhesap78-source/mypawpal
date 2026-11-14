using System;
using System.Collections.Generic;
using UnityEngine;

namespace PatiDostum
{
    public enum Language { Turkish, English }

    /// <summary>
    /// Basit dil sistemi: tüm metinler tek sözlükte durur. Seçim kaydedilir;
    /// kayıt yoksa telefonun diline göre Türkçe ya da İngilizce başlar.
    /// </summary>
    public static class Loc
    {
        private const string PrefsKey = "lang";

        // anahtar → { Türkçe, İngilizce }
        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            { "title", new[] { "Alpi's Paw Pal", "Alpi's Paw Pal" } },
            // Ana sayfa ve gezinme
            { "nav_home", new[] { "Ana Sayfa", "Home" } },
            { "nav_shop", new[] { "Mağaza", "Shop" } },
            { "nav_tasks", new[] { "Görevler", "Tasks" } },
            { "nav_memories", new[] { "Anılar", "Memories" } },
            { "nav_settings", new[] { "Ayarlar", "Settings" } },
            { "nav_baby", new[] { "Yavru", "Baby" } },
            { "home_new_pal", new[] { "Yeni Dost", "New Pal" } },
            { "home_open_slot", new[] { "Yer Aç", "Unlock" } },
            { "home_asleep", new[] { "Uyuyor", "Sleeping" } },
            // Dolap
            { "closet_open", new[] { "Dolabı Aç", "Open Closet" } },
            { "closet_remove_all", new[] { "Hepsini Çıkar", "Take All Off" } },
            { "closet_mine", new[] { "Dolabım", "My Closet" } },
            { "closet_empty", new[] { "Dolabın boş. Şapka, gözlük ya da elbise al, hepsi burada durur!", "Your closet is empty. Buy a hat, glasses or a dress and they all stay here!" } },
            { "closet_count", new[] { "Koleksiyon: {0}/{1}", "Collection: {0}/{1}" } },
            { "closet_no_outfit", new[] { "Bu dost elbise giymez, ama şapka ve gözlük çok yakışır!", "This pal does not wear clothes, but hats and glasses look great!" } },
            // Gezinti
            { "act_walk", new[] { "Gezdir", "Walk" } },
            { "walk_done", new[] { "Eve Dön", "Go Home" } },
            { "product_slot_4", new[] { "4. Dost Slotu", "4th Pal Slot" } },
            { "product_slot_4_desc", new[] { "Aynı anda dört dosta bak", "Care for four pals at once" } },
            { "product_slot_5", new[] { "5. Dost Slotu", "5th Pal Slot" } },
            { "product_slot_5_desc", new[] { "Aynı anda beş dosta bak", "Care for five pals at once" } },
            { "adopt", new[] { "SAHİPLEN", "ADOPT" } },
            { "soon", new[] { "Çok yakında!", "Coming soon!" } },
            { "away", new[] { "Son ziyaretten beri: {0}", "Since your last visit: {0}" } },
            { "away_first", new[] { "Hoş geldin!", "Welcome!" } },
            { "duration_min", new[] { "{0} dk", "{0} min" } },
            { "duration_hour", new[] { "{0} sa {1} dk", "{0} h {1} min" } },
            { "duration_day", new[] { "{0} gün {1} sa", "{0} d {1} h" } },
            { "tip_1", new[] { "Dostunu her gün beslemeyi unutma!", "Don't forget to feed your pal every day!" } },
            { "tip_2", new[] { "Gece dostun uyur, ihtiyaçları yavaşlar.", "At night your pal sleeps and needs less." } },
            { "tip_3", new[] { "Hasta dostunu veterinere götür.", "Take your sick pal to the vet." } },
            { "tip_4", new[] { "Mavi elmaslar çok değerlidir!", "Blue diamonds are very precious!" } },
            { "tip_5", new[] { "Mutlu dost, sağlıklı dost!", "A happy pal is a healthy pal!" } },

            // Türler
            { "species_hamster", new[] { "Hamster", "Hamster" } },
            { "species_hedgehog", new[] { "Kirpi", "Hedgehog" } },
            { "species_guinea_pig", new[] { "Kobay", "Guinea Pig" } },
            { "species_budgie", new[] { "Muhabbet Kuşu", "Budgie" } },
            { "species_rabbit", new[] { "Tavşan", "Rabbit" } },
            { "species_duck", new[] { "Ördek", "Duck" } },
            { "species_goldfish", new[] { "Japon Balığı", "Goldfish" } },
            { "species_dog", new[] { "Köpek", "Dog" } },
            { "species_cat", new[] { "Kedi", "Cat" } },
            { "species_turtle", new[] { "Kaplumbağa", "Turtle" } },
            { "species_penguin", new[] { "Penguen", "Penguin" } },
            { "species_lion", new[] { "Aslan", "Lion" } },
            { "species_giraffe", new[] { "Zürafa", "Giraffe" } },
            { "species_panda", new[] { "Panda", "Panda" } },
            { "species_crocodile", new[] { "Timsah", "Crocodile" } },
            { "food_meat", new[] { "Et", "Meat" } },
            { "food_steak", new[] { "Biftek", "Steak" } },
            { "food_bamboo", new[] { "Bambu", "Bamboo" } },
            { "food_acacia", new[] { "Akasya", "Acacia" } },
            { "food_krill", new[] { "Karides", "Krill" } },
            { "food_fern", new[] { "Eğrelti", "Fern" } },
            { "food_corn", new[] { "Mısır", "Corn" } },
            { "food_millet", new[] { "Darı", "Millet" } },
            { "food_sunflower", new[] { "Ay çekirdeği", "Sunflower seeds" } },
            { "food_cricket", new[] { "Cırcır", "Cricket" } },
            { "food_strawberry", new[] { "Çilek", "Strawberry" } },
            { "food_cucumber", new[] { "Salatalık", "Cucumber" } },
            { "food_honey", new[] { "Bal", "Honey" } },
            { "walk_flip", new[] { "TAKLA!", "FLIP!" } },

            // Sahiplenme
            { "adopt_title", new[] { "Bir dost seç", "Choose a pal" } },
            { "adopt_details", new[] { "Dostunu tanıyalım", "Meet your pal" } },
            { "adopt_name", new[] { "Adı", "Name" } },
            { "adopt_color", new[] { "Rengi", "Color" } },
            { "adopt_mode", new[] { "Zaman", "Time" } },
            { "adopt_mode_note", new[] { "Zaman sonradan değiştirilemez!", "Time mode can't be changed later!" } },
            { "mode_real", new[] { "GERÇEK", "REAL" } },
            { "mode_fast", new[] { "HIZLI ×5", "FAST ×5" } },
            { "mode_real_desc", new[] { "Gerçek dünya gibi büyür\n{0} yıl yaşar", "Grows like in real life\nLives {0} years" } },
            { "mode_fast_desc", new[] { "5 kat hızlı büyür\n{0} yıl yaşar", "Grows 5 times faster\nLives {0} years" } },
            { "locked", new[] { "Yakında", "Soon" } },
            { "next", new[] { "İLERİ", "NEXT" } },
            { "back", new[] { "Geri", "Back" } },

            // Odalar ve aksiyonlar
            { "room_kitchen", new[] { "Mutfak", "Kitchen" } },
            { "room_bath", new[] { "Banyo", "Bath" } },
            { "room_bed", new[] { "Yatak", "Bed" } },
            { "room_play", new[] { "Oyun", "Play" } },
            { "food_pet_food", new[] { "Mama", "Food" } },
            { "food_fish", new[] { "Balık", "Fish" } },
            { "food_treat", new[] { "Ödül", "Treat" } },
            { "act_water", new[] { "Su", "Water" } },
            { "act_bathe", new[] { "Yıka", "Bath" } },
            { "act_brush", new[] { "Tara", "Brush" } },
            { "act_toilet", new[] { "Tuvalet", "Toilet" } },
            { "act_clean", new[] { "Temizle", "Clean" } },
            { "act_sleep", new[] { "Uyut", "Sleep" } },
            { "act_wake", new[] { "Uyandır", "Wake" } },
            { "act_play", new[] { "Top", "Ball" } },
            { "act_stroke", new[] { "Sev", "Pet" } },

            // Ölçümler
            { "stat_fullness", new[] { "Tokluk", "Food" } },
            { "stat_thirst", new[] { "Su", "Water" } },
            { "stat_energy", new[] { "Enerji", "Energy" } },
            { "stat_hygiene", new[] { "Temizlik", "Clean" } },
            { "stat_toilet", new[] { "Tuvalet", "Toilet" } },
            { "stat_fun", new[] { "Eğlence", "Fun" } },
            { "stat_health", new[] { "Sağlık", "Health" } },
            { "stat_love", new[] { "Sevgi", "Love" } },
            { "stat_happiness", new[] { "Mutluluk", "Happy" } },
            { "weight_thin", new[] { "Zayıf", "Thin" } },
            { "weight_ideal", new[] { "İdeal kilo", "Ideal weight" } },
            { "weight_heavy", new[] { "Kilolu", "Chubby" } },

            // Ruh hali balonu
            { "mood_sleeping", new[] { "Zzz...", "Zzz..." } },
            { "mood_sick", new[] { "Kendimi iyi hissetmiyorum...", "I don't feel well..." } },
            { "mood_needstoilet", new[] { "Tuvaletim geldi!", "I need the toilet!" } },
            { "mood_hungry", new[] { "Acıktım!", "I'm hungry!" } },
            { "mood_thirsty", new[] { "Susadım!", "I'm thirsty!" } },
            { "mood_sleepy", new[] { "Uykum geldi...", "I'm sleepy..." } },
            { "mood_dirty", new[] { "Yıkanmak istiyorum!", "I need a bath!" } },
            { "mood_bored", new[] { "Sıkıldım, oynayalım!", "I'm bored, let's play!" } },
            { "mood_sulky", new[] { "Sana küstüm!", "I'm upset with you!" } },
            { "mood_funny", new[] { "Hihihi!", "Hee hee!" } },
            { "mood_happy", new[] { "Çok mutluyum!", "I'm so happy!" } },
            { "mood_okay", new[] { "İyiyim.", "I'm okay." } },

            // Aksiyon sonuçları
            { "fx_eat", new[] { "Mmm!", "Yum!" } },
            { "fx_drink", new[] { "Lık lık!", "Slurp!" } },
            { "fx_bathe", new[] { "Mis gibi!", "So fresh!" } },
            { "fx_brush", new[] { "Pırıl pırıl!", "Shiny!" } },
            { "fx_toilet", new[] { "Oh be!", "Phew!" } },
            { "fx_clean", new[] { "Tertemiz!", "All clean!" } },
            { "fx_sleep", new[] { "İyi geceler!", "Good night!" } },
            { "fx_wake", new[] { "Günaydın!", "Good morning!" } },
            { "fx_play", new[] { "Yaşasın!", "Yay!" } },
            { "fx_stroke", new[] { "Mırr!", "Purr!" } },
            { "res_asleep", new[] { "Şşş, uyuyor!", "Shh, sleeping!" } },
            { "res_notneeded", new[] { "Şu an gerek yok", "Not needed now" } },
            { "res_overfed", new[] { "Çok yedi, kilo aldı!", "Too much, gained weight!" } },
            { "res_tired", new[] { "Çok yorgun", "Too tired" } },

            // Sağlık ve veteriner (M2)
            { "room_vet", new[] { "Revir", "Vet" } },
            { "mood_critical", new[] { "Çok hastayım, veterinere götür!", "I'm very sick, take me to the vet!" } },
            { "card_mood_critical", new[] { "Revire götür!", "To the vet!" } },
            { "card_mood_sick", new[] { "Hastayım...", "I feel sick..." } },
            { "disease_cold", new[] { "Soğuk algınlığı", "Cold" } },
            { "disease_fleas", new[] { "Pire", "Fleas" } },
            { "disease_stomach", new[] { "Mide bozukluğu", "Upset tummy" } },
            { "disease_toothache", new[] { "Diş ağrısı", "Toothache" } },
            { "disease_obesity", new[] { "Obezite", "Obesity" } },
            { "symptom_cold", new[] { "Hapşu! Burnum akıyor...", "Achoo! My nose is runny..." } },
            { "symptom_fleas", new[] { "Kaşınıyorum!", "So itchy!" } },
            { "symptom_stomach", new[] { "Karnım ağrıyor...", "My tummy hurts..." } },
            { "symptom_toothache", new[] { "Dişim ağrıyor...", "My tooth hurts..." } },
            { "symptom_obesity", new[] { "Çok ağırım, yoruldum...", "I'm too heavy..." } },
            { "status_sick", new[] { "Hasta: {0}", "Sick: {0}" } },
            { "status_vaccinated", new[] { "Aşılı", "Vaccinated" } },
            { "critical_banner", new[] { "Veterinere götür! {0} saat kaldı", "Go to the vet! {0} hours left" } },
            { "critical_banner_safe", new[] { "Çok hasta! Veterinere götür!", "Very sick! Go to the vet!" } },
            { "vet_treat", new[] { "Tedavi\n{0} altın", "Treat\n{0} gold" } },
            { "vet_treat_free", new[] { "Tedavi\nbedava", "Treat\nfree" } },
            { "vet_treat_none", new[] { "Tedavi", "Treat" } },
            { "vet_vaccine_name", new[] { "Aşı", "Vaccine" } },
            { "vet_miracle_name", new[] { "Mucize", "Miracle" } },
            { "vet_vaccine", new[] { "Aşı\n{0} altın", "Vaccine\n{0} gold" } },
            { "vet_miracle", new[] { "Mucize\n{0} elmas", "Miracle\n{0} gems" } },
            { "vet_miracle_free", new[] { "Mucize\nbedava", "Miracle\nfree" } },
            { "vet_done_treat", new[] { "Geçmiş olsun!", "Get well soon!" } },
            { "vet_done_vaccine", new[] { "Aşı yapıldı!", "Vaccinated!" } },
            { "vet_done_miracle", new[] { "Mucize! Kurtuldu!", "A miracle! Saved!" } },
            { "vet_notsick", new[] { "Dostun sağlıklı!", "Your pal is healthy!" } },
            { "vet_nomoney", new[] { "Yeterli paran yok", "Not enough money" } },
            { "vet_already", new[] { "Zaten aşılı", "Already vaccinated" } },
            { "vet_critical", new[] { "Sadece Mucize Tedavi kurtarır!", "Only a Miracle can save it!" } },
            { "vet_notcritical", new[] { "Mucizeye gerek yok", "No miracle needed" } },
            { "vet_usedtoday", new[] { "Bedava tedavi yarın tekrar", "Free treatment again tomorrow" } },

            // Anı Bahçesi
            { "memorial", new[] { "Anı Bahçesi", "Memory Garden" } },
            { "memorial_natural", new[] { "{0} huzurla uyudu.\n{1} yaşadı. Ömür Boyu Dost!", "{0} fell asleep peacefully.\nLived {1}. Friend for Life!" } },
            { "memorial_neglect", new[] { "{0} çok hastaydı ve aramızdan ayrıldı.\nHasta dostlarını veterinere götürmeyi unutma.", "{0} was very sick and passed away.\nAlways take sick pals to the vet." } },
            { "memorial_card_natural", new[] { "Ömür Boyu Dost", "Friend for Life" } },
            { "memorial_card_neglect", new[] { "Hastalıktan", "Illness" } },
            { "memorial_empty", new[] { "Burası henüz boş.", "Nothing here yet." } },
            { "new_pal", new[] { "YENİ DOST", "NEW PAL" } },

            // Ayarlar ve ebeveyn kapısı
            { "settings", new[] { "Ayarlar", "Settings" } },
            { "settings_notifications", new[] { "Bildirimler", "Notifications" } },
            { "settings_sound", new[] { "Sesler", "Sounds" } },
            { "settings_music", new[] { "Müzik", "Music" } },
            { "settings_nodeath", new[] { "Hayvanlar ölmesin", "Pets never die" } },
            { "settings_language", new[] { "Dil: Türkçe", "Language: English" } },
            { "on", new[] { "AÇIK", "ON" } },
            { "off", new[] { "KAPALI", "OFF" } },
            { "close", new[] { "Kapat", "Close" } },
            { "gate_title", new[] { "Ebeveyn sorusu", "Grown-ups only" } },
            { "gate_question", new[] { "{0} × {1} = ?", "{0} × {1} = ?" } },
            { "gate_wrong", new[] { "Yanlış, tekrar dene", "Wrong, try again" } },

            // Bildirimler
            { "notify_channel", new[] { "Dostun", "Your pal" } },
            { "notify_channel_desc", new[] { "Dostunun ihtiyaçlarını hatırlatır", "Reminds you of your pal's needs" } },
            { "notify_hungry", new[] { "{0} acıktı! Mamasını vermeye ne dersin?", "{0} is hungry! Time for food?" } },
            { "notify_toilet", new[] { "{0} tuvalete gitmek istiyor!", "{0} needs the toilet!" } },
            { "notify_sick", new[] { "{0} hasta görünüyor, veterinere götür.", "{0} looks sick, visit the vet." } },
            { "notify_critical", new[] { "{0} çok hasta! Hemen veterinere götür!", "{0} is very sick! Go to the vet now!" } },

            // M3: yiyecekler, fiyatlar
            { "food_seeds", new[] { "Tohum", "Seeds" } },
            { "food_hay", new[] { "Kuru ot", "Hay" } },
            { "food_flakes", new[] { "Balık yemi", "Fish flakes" } },
            { "food_greens", new[] { "Marul", "Lettuce" } },
            { "food_carrot", new[] { "Havuç", "Carrot" } },
            { "food_bone", new[] { "Kemik", "Bone" } },
            { "food_worm", new[] { "Solucan", "Worm" } },
            { "food_fruit", new[] { "Meyve", "Fruit" } },
            { "price_gold", new[] { "{0}\n{1} altın", "{0}\n{1} gold" } },
            { "price_gold_short", new[] { "{0} altın", "{0} gold" } },

            // Okul
            { "room_school", new[] { "Okul", "School" } },
            { "room_closet", new[] { "Dolap", "Closet" } },
            { "act_book", new[] { "Kitap Oku", "Read" } },
            { "fx_read", new[] { "Bir varmış, bir yokmuş...", "Once upon a time..." } },
            { "fx_trick", new[] { "Bravo!", "Bravo!" } },
            { "cmd_progress", new[] { "{0}\n%{1}", "{0}\n{1}%" } },
            { "cmd_show", new[] { "{0}\nGöster!", "{0}\nShow!" } },
            { "cmd_sit", new[] { "Otur", "Sit" } },
            { "cmd_paw", new[] { "Pati ver", "Paw" } },
            { "cmd_roll", new[] { "Takla", "Roll" } },
            { "cmd_fetch", new[] { "Getir", "Fetch" } },
            { "cmd_come", new[] { "Gel", "Come" } },
            { "cmd_spin", new[] { "Dön", "Spin" } },
            { "cmd_stand", new[] { "Kalk", "Stand" } },
            { "cmd_jump", new[] { "Zıpla", "Jump" } },
            { "cmd_speak", new[] { "Konuş", "Speak" } },
            { "cmd_ball", new[] { "Top ol", "Curl up" } },
            { "cmd_flap", new[] { "Kanat çırp", "Flap" } },
            { "cmd_hoop", new[] { "Halka", "Hoop" } },
            { "cmd_peek", new[] { "Kafanı çıkar", "Peek" } },
            { "simon_watch", new[] { "İzle...", "Watch..." } },
            { "simon_go", new[] { "Sıra sende!", "Your turn!" } },
            { "school_good", new[] { "Aferin, öğreniyor!", "Good, learning!" } },
            { "school_again", new[] { "Olmadı, tekrar dene", "Not quite, try again" } },
            { "school_learned", new[] { "Öğrendi! Artık gösterebilir!", "Learned it! Show it off!" } },

            // Dolap
            { "slot_hat", new[] { "Şapka", "Hat" } },
            { "slot_glasses", new[] { "Gözlük", "Glasses" } },
            { "slot_neck", new[] { "Boyun", "Neck" } },
            { "slot_outfit", new[] { "Kıyafet", "Outfit" } },
            { "slot_feet", new[] { "Ayak", "Feet" } },
            { "item_party_hat", new[] { "Parti şapkası", "Party hat" } },
            { "item_cap", new[] { "Kep", "Cap" } },
            { "item_crown", new[] { "Taç", "Crown" } },
            { "item_round_glasses", new[] { "Yuvarlak gözlük", "Round glasses" } },
            { "item_sunglasses", new[] { "Güneş gözlüğü", "Sunglasses" } },
            { "item_bow_tie", new[] { "Papyon", "Bow tie" } },
            { "item_scarf", new[] { "Fular", "Scarf" } },
            { "item_bunny_ears", new[] { "Tavşan kulağı", "Bunny ears" } },
            { "item_bear_ears", new[] { "Ayı kulağı", "Bear ears" } },
            { "item_beret", new[] { "Bere", "Beret" } },
            { "item_chef_hat", new[] { "Aşçı şapkası", "Chef hat" } },
            { "item_cowboy_hat", new[] { "Kovboy şapkası", "Cowboy hat" } },
            { "item_pirate_hat", new[] { "Korsan şapkası", "Pirate hat" } },
            { "item_top_hat", new[] { "Silindir şapka", "Top hat" } },
            { "item_tiara", new[] { "Prenses tacı", "Tiara" } },
            { "item_heart_glasses", new[] { "Kalp gözlük", "Heart glasses" } },
            { "item_rainbow_glasses", new[] { "Gökkuşağı gözlük", "Rainbow glasses" } },
            { "item_party_mask", new[] { "Parti maskesi", "Party mask" } },
            { "item_bell_collar", new[] { "Zilli tasma", "Bell collar" } },
            { "item_bandana", new[] { "Bandana", "Bandana" } },
            { "item_flower_lei", new[] { "Çiçek kolye", "Flower lei" } },
            { "item_pearl_necklace", new[] { "İnci kolye", "Pearl necklace" } },
            { "item_medal", new[] { "Madalya", "Medal" } },
            { "item_sweater", new[] { "Kazak", "Sweater" } },
            { "item_raincoat", new[] { "Yağmurluk", "Raincoat" } },
            { "item_booties", new[] { "Patik", "Booties" } },
            { "wardrobe_wear", new[] { "Giy", "Wear" } },
            { "wardrobe_remove", new[] { "Çıkar", "Remove" } },
            { "wardrobe_bought", new[] { "Çok yakıştı!", "Looks great!" } },
            { "wardrobe_worn", new[] { "Şık oldu!", "So stylish!" } },
            { "wardrobe_removed", new[] { "Çıkardı", "Taken off" } },

            // Mini oyunlar
            { "act_games", new[] { "Oyunlar", "Games" } },
            { "games_title", new[] { "Mini oyunlar", "Mini games" } },
            { "game_fetch", new[] { "Top getir", "Fetch" } },
            { "game_catch", new[] { "Yiyecek yakala", "Catch the food" } },
            { "game_memory", new[] { "Hafıza kartları", "Memory cards" } },
            { "game_hide", new[] { "Saklambaç", "Hide and seek" } },
            { "game_bubble", new[] { "Kabarcık patlat", "Bubble pop" } },
            { "game_bubble_desc", new[] { "Kabarcıklara dokun, yıldızlıları kaçırma", "Tap bubbles, catch the stars" } },
            { "game_level", new[] { "Seviye {0}", "Level {0}" } },
            { "game_level_again", new[] { "Seviye {0}: 2 yıldız al, sonraki açılsın!", "Level {0}: get 2 stars to unlock the next!" } },
            { "game_level_up", new[] { "Seviye {0} açıldı! +{1} altın", "Level {0} unlocked! +{1} gold" } },
            { "game_level_up_gem", new[] { "Seviye {0} açıldı! +{1} altın, +{2} elmas", "Level {0} unlocked! +{1} gold, +{2} gems" } },
            { "act_swim", new[] { "Yüz", "Swim" } },
            { "act_fly", new[] { "Uç", "Fly" } },
            { "act_bubbles", new[] { "Kabarcık", "Bubbles" } },
            { "act_clean_tank", new[] { "Temizle", "Clean" } },
            { "breed_ready", new[] { "Hazırlar!", "They're ready!" } },
            { "breed_born", new[] { "Yavru geldi!", "A baby arrived!" } },
            { "breed_name_ok", new[] { "TAMAM", "DONE" } },
            { "store_saving", new[] { "Tek tek almaktan %{0} ucuz", "{0}% cheaper than buying one by one" } },
            { "product_family_pack", new[] { "Aile Paketi", "Family Pack" } },
            { "product_family_pack_desc", new[] { "Bütün dost yerleri bir arada: 5 dosta bak", "All pal slots together: care for 5 pals" } },
            { "game_fetch_desc", new[] { "Topu tam zamanında at", "Throw at the right time" } },
            { "game_catch_desc", new[] { "Yiyeceği yakala, çöpten kaç", "Catch food, avoid trash" } },
            { "game_memory_desc", new[] { "Aynı resimleri bul", "Find matching pictures" } },
            { "game_hide_desc", new[] { "Dostun hangi kutuda?", "Which box is your pal in?" } },
            { "game_result", new[] { "Harika!\n+{0} altın", "Great!\n+{0} gold" } },
            { "game_result_zero", new[] { "Bir daha dene!\n+{0} altın", "Try again!\n+{0} gold" } },
            { "game_again", new[] { "TEKRAR", "AGAIN" } },
            { "game_back", new[] { "ODAYA DÖN", "BACK" } },
            { "fetch_hint", new[] { "Çizgi yeşildeyken AT!", "THROW when it's green!" } },
            { "fetch_throw", new[] { "AT!", "THROW!" } },
            { "fetch_stars_0", new[] { "Iska!", "Missed!" } },
            { "fetch_stars_1", new[] { "Fena değil! +1", "Not bad! +1" } },
            { "fetch_stars_2", new[] { "Güzel! +2", "Nice! +2" } },
            { "fetch_stars_3", new[] { "Mükemmel! +3", "Perfect! +3" } },
            { "fetch_score", new[] { "Puan {0} · Atış {1}/{2}", "Score {0} · Throw {1}/{2}" } },
            { "catch_score", new[] { "Yakalanan {0} · {1} sn", "Caught {0} · {1} s" } },
            { "memory_score", new[] { "Çift {0}/{1} · Hamle {2}", "Pairs {0}/{1} · Moves {2}" } },
            { "hide_score", new[] { "Bulunan {0} · Tur {1}/{2}", "Found {0} · Round {1}/{2}" } },
            { "hide_watch", new[] { "Dostun nereye saklanıyor?", "Where does your pal hide?" } },
            { "hide_pick", new[] { "Hangi kutuda?", "Which box?" } },
            { "hide_pick_shell", new[] { "Hangi kabukta?", "Which shell?" } },
            { "bubble_score", new[] { "Patlatılan {0} · {1} sn", "Popped {0} · {1} s" } },
            { "hide_right", new[] { "Buldun!", "Found it!" } },
            { "hide_wrong", new[] { "Burada değil!", "Not here!" } },

            // M4: Dino paketi
            { "species_t_rex", new[] { "T-Rex", "T-Rex" } },
            { "species_triceratops", new[] { "Triceratops", "Triceratops" } },
            { "species_brachiosaurus", new[] { "Brakiyozor", "Brachiosaurus" } },
            { "cmd_roar", new[] { "Kükre", "Roar" } },
            { "dino_locked", new[] { "Dino Paketi", "Dino Pack" } },

            // Mağaza
            { "shop", new[] { "Mağaza", "Shop" } },
            { "shop_restore", new[] { "Satın alımları geri yükle", "Restore purchases" } },
            // Yalnızca editör ve development build'de görünür (ShopController)
            { "shop_test", new[] { "TEST MAĞAZASI: para çekilmez", "TEST STORE: no real money" } },
            { "shop_note", new[] { "Fiyatlara KDV dahildir. Ödeme Google Play üzerinden yapılır; dijital içerik hemen teslim edildiği için cayma hakkı yoktur, iadeler Google Play'in iade süreciyle yürür.", "Prices include VAT. Payment is handled by Google Play; digital content is delivered immediately, refunds follow Google Play's refund process." } },
            { "store_owned", new[] { "Alındı", "Owned" } },
            { "store_already", new[] { "Bu ürün zaten sende.", "You already own this." } },
            { "store_need_prev", new[] { "Önce bir önceki yeri aç.", "Unlock the previous slot first." } },
            { "store_restore_none", new[] { "Geri yüklenecek yeni bir satın alma yok.", "No new purchases to restore." } },
            { "store_have", new[] { "Elinde: {0}", "You have: {0}" } },
            { "store_slots", new[] { "Slot: {0}/{1}", "Slots: {0}/{1}" } },
            { "store_thanks", new[] { "Teşekkürler! {0} eklendi.", "Thank you! {0} added." } },
            { "store_restored", new[] { "{0} geri yüklendi.", "{0} restored." } },
            { "store_restoring", new[] { "Satın alımlar kontrol ediliyor...", "Checking purchases..." } },
            { "store_pending", new[] { "Ödeme bekleniyor; onaylanınca eklenecek.", "Payment pending; added once approved." } },
            { "store_cancelled", new[] { "Satın alma iptal edildi.", "Purchase cancelled." } },
            { "store_failed", new[] { "Satın alma tamamlanamadı.", "Purchase couldn't be completed." } },
            { "store_unavailable", new[] { "Mağaza şu an kullanılamıyor.", "The store isn't available right now." } },
            { "store_limit", new[] { "Bu ayın harcama limiti ({0} TL) doldu.", "This month's spending limit ({0} TL) is reached." } },
            { "store_exchange", new[] { "1 elmas = {0} altın", "1 gem = {0} gold" } },
            { "store_exchanged", new[] { "Değiştirildi!", "Exchanged!" } },
            { "store_exchange_button", new[] { "Değiştir", "Exchange" } },
            { "product_starter_pack", new[] { "Başlangıç Paketi", "Starter Pack" } },
            { "product_starter_pack_desc", new[] { "2. dost slotu + 20 elmas + zilli tasma ve fular (ilk hafta)", "2nd pal slot + 20 gems + bell collar and scarf (first week)" } },
            { "product_gems_10", new[] { "10 Mavi Elmas", "10 Blue Gems" } },
            { "product_gems_10_desc", new[] { "Mucize Tedavi ve barınak için", "For Miracle Treatment and the shelter" } },
            { "product_gems_30", new[] { "30 Mavi Elmas", "30 Blue Gems" } },
            { "product_gems_30_desc", new[] { "Daha avantajlı", "Better value" } },
            { "product_gems_80", new[] { "80 Mavi Elmas", "80 Blue Gems" } },
            { "product_gems_80_desc", new[] { "En avantajlı", "Best value" } },
            { "product_gold_500", new[] { "500 Altın", "500 Gold" } },
            { "product_gold_500_desc", new[] { "Lezzetli yiyecek ve kıyafetler için", "For tasty food and clothes" } },
            { "product_gold_1500", new[] { "1500 Altın", "1500 Gold" } },
            { "product_gold_1500_desc", new[] { "Daha avantajlı", "Better value" } },
            { "product_gold_4000", new[] { "4000 Altın", "4000 Gold" } },
            { "product_gold_4000_desc", new[] { "En avantajlı", "Best value" } },
            { "product_slot_2", new[] { "2. Dost Slotu", "2nd Pal Slot" } },
            { "product_slot_2_desc", new[] { "Aynı anda iki dosta bak", "Care for two pals at once" } },
            { "product_slot_3", new[] { "3. Dost Slotu", "3rd Pal Slot" } },
            { "product_slot_3_desc", new[] { "Aynı anda üç dosta bak", "Care for three pals at once" } },
            { "product_dino_pack", new[] { "Dino Paketi", "Dino Pack" } },
            { "product_dino_pack_desc", new[] { "Bebek T-Rex, Triceratops ve Brakiyozor", "Baby T-Rex, Triceratops and Brachiosaurus" } },
            { "product_mode_changer", new[] { "Mod Değiştirici", "Mode Changer" } },
            { "product_mode_changer_desc", new[] { "Dostunun zamanını Gerçek / Hızlı arasında çevirir", "Switches your pal's time between Real / Fast" } },
            { "product_shelter_ticket", new[] { "Barınak Bileti", "Shelter Ticket" } },
            { "product_shelter_ticket_desc", new[] { "Barınak dostuna yeni bir aile bulur, slotun boşalır", "The shelter finds your pal a new family" } },
            { "product_boarding_ticket", new[] { "Pansiyon Bileti", "Boarding Ticket" } },
            { "product_boarding_ticket_desc", new[] { "Tatilde 7 gün dostlarının ihtiyaçları donar", "Freezes your pals' needs for 7 days" } },

            // Dostlarım ve ayarlar (M4)
            { "pets_title", new[] { "Dostlarım", "My Pals" } },
            { "pets_new", new[] { "YENİ DOST", "NEW PAL" } },
            { "pets_need_slot", new[] { "SLOT AL ({0}/{1})", "GET A SLOT ({0}/{1})" } },
            { "settings_limit", new[] { "Aylık harcama", "Monthly spending" } },
            { "limit_none", new[] { "Limitsiz", "No limit" } },
            { "limit_tl", new[] { "{0} TL", "{0} TL" } },
            { "settings_mode", new[] { "Zaman modu", "Time mode" } },
            { "settings_count", new[] { "{0} ({1})", "{0} ({1})" } },
            { "settings_boarding", new[] { "Pansiyon", "Boarding" } },
            { "settings_tickets", new[] { "{0} bilet", "{0} tickets" } },
            { "boarding_days", new[] { "Pansiyonda: {0} gün", "Boarding: {0} days" } },
            { "settings_shelter", new[] { "Barınağa gönder", "Send to shelter" } },
            { "shelter_diamonds", new[] { "{0} elmas", "{0} gems" } },
            { "memorial_shelter", new[] { "{0} barınakta yeni ailesine kavuşacak.\nOnu çok sevdin, teşekkürler!", "{0} will meet a new family at the shelter.\nThank you for loving them!" } },
            { "memorial_card_shelter", new[] { "Yeni yuvasında", "In a new home" } },

            // M5: Merkez
            { "hub_button", new[] { "Merkez", "Hub" } },
            { "hub_tasks", new[] { "Görevler", "Tasks" } },
            { "hub_pass", new[] { "Pass", "Pass" } },
            { "hub_badges", new[] { "Rozetler", "Badges" } },
            { "hub_show", new[] { "Şov", "Show" } },
            { "hub_home", new[] { "Ev", "Home" } },
            { "hub_club", new[] { "Kulüp", "Club" } },
            { "hub_streak", new[] { "Bakım serisi: {0} gün", "Care streak: {0} days" } },
            { "hub_streak_hint", new[] { "Her gün dostuna bak: her gün 10 altın, her 7 günde 1 elmas!", "Care every day: 10 gold daily, 1 gem every 7 days!" } },
            { "hub_today", new[] { "Bugünün görevleri", "Today's tasks" } },
            { "hub_claim", new[] { "AL", "CLAIM" } },
            { "hub_claimed", new[] { "Alındı", "Claimed" } },
            { "hub_task_claimed", new[] { "+15 altın!", "+15 gold!" } },
            { "hub_reward_claimed", new[] { "Ödül alındı!", "Reward claimed!" } },
            { "hub_theme_set", new[] { "Tema değişti!", "Theme changed!" } },
            { "task_feed", new[] { "Besle", "Feed" } },
            { "task_drink", new[] { "Su ver", "Give water" } },
            { "task_bathe", new[] { "Yıka", "Bathe" } },
            { "task_play", new[] { "Top oyna", "Play ball" } },
            { "task_stroke", new[] { "Sev", "Pet" } },
            { "task_minigame", new[] { "Mini oyun oyna", "Play a mini game" } },
            { "task_lesson", new[] { "Okulda ders yap", "Do a lesson" } },
            { "task_read", new[] { "Kitap oku", "Read a book" } },
            { "task_wear", new[] { "Kıyafet giydir", "Dress up" } },
            { "streak_up", new[] { "Bakım serisi: {0} gün! +{1} altın", "Care streak: {0} days! +{1} gold" } },
            { "badge_new", new[] { "Yeni rozet: {0}!", "New badge: {0}!" } },

            // Rozetler
            { "badge_first_pal", new[] { "İlk Dost", "First Pal" } },
            { "badge_first_pal_desc", new[] { "İlk dostunu sahiplen", "Adopt your first pal" } },
            { "badge_three_pals", new[] { "Kalabalık Ev", "Full House" } },
            { "badge_three_pals_desc", new[] { "Aynı anda 3 dosta bak", "Care for 3 pals at once" } },
            { "badge_streak_7", new[] { "Bir Hafta", "One Week" } },
            { "badge_streak_7_desc", new[] { "7 gün aralıksız bakım", "7-day care streak" } },
            { "badge_streak_30", new[] { "Bir Ay", "One Month" } },
            { "badge_streak_30_desc", new[] { "30 gün aralıksız bakım", "30-day care streak" } },
            { "badge_first_trick", new[] { "İlk Numara", "First Trick" } },
            { "badge_first_trick_desc", new[] { "Bir komut öğret", "Teach a command" } },
            { "badge_smart_pal", new[] { "Çok Akıllı", "Smart Pal" } },
            { "badge_smart_pal_desc", new[] { "Bir dosta 3 komut öğret", "Teach a pal 3 commands" } },
            { "badge_gamer", new[] { "Oyuncu", "Gamer" } },
            { "badge_gamer_desc", new[] { "20 mini oyun oyna", "Play 20 mini games" } },
            { "badge_show_winner", new[] { "Şov Birincisi", "Show Winner" } },
            { "badge_show_winner_desc", new[] { "Pet Şov'da birinci ol", "Win a Pet Show" } },
            { "badge_star_winner", new[] { "Yıldız", "Star" } },
            { "badge_star_winner_desc", new[] { "Yıldızlar Şovu'nda birinci ol", "Win a Star Show" } },
            { "badge_fashion", new[] { "Moda İkonu", "Fashion Icon" } },
            { "badge_fashion_desc", new[] { "5 kıyafet topla", "Collect 5 outfits" } },
            { "badge_healer", new[] { "Şefkatli", "Caring" } },
            { "badge_healer_desc", new[] { "5 kez veterinerde tedavi", "5 vet treatments" } },
            { "badge_lifelong", new[] { "Ömür Boyu Dost", "Friend for Life" } },
            { "badge_lifelong_desc", new[] { "Bir dostun ömrünü tamamlasın", "A pal lives a full life" } },
            { "badge_family", new[] { "Aile", "Family" } },
            { "badge_family_desc", new[] { "Bir yavru büyüt", "Raise a baby" } },

            // Pati Pass
            { "pass_title", new[] { "Pati Pass", "Paw Pass" } },
            { "pass_level", new[] { "Basamak {0}/{1} · sezonun bitmesine {2} gün", "Level {0}/{1} · {2} days left" } },
            { "pass_free", new[] { "Bedava", "Free" } },
            { "pass_premium", new[] { "Premium", "Premium" } },
            { "pass_buy", new[] { "PREMIUM AÇ · {0}", "UNLOCK PREMIUM · {0}" } },
            { "pass_premium_on", new[] { "Premium açık", "Premium unlocked" } },
            { "pass_hint", new[] { "Görev, seri ve şovlar puan verir. Her ay yeni sezon.", "Tasks, streaks and shows give points. New season monthly." } },
            { "reward_diamonds", new[] { "{0} elmas", "{0} gems" } },

            // Şov
            { "show_normal", new[] { "Pet Şov", "Pet Show" } },
            { "show_star", new[] { "Yıldızlar Şovu", "Star Show" } },
            { "show_rivals", new[] { "Bu haftanın rakipleri:", "This week's rivals:" } },
            { "show_enter", new[] { "KATIL", "ENTER" } },
            { "show_enter_star", new[] { "KATIL ({0} elmas)", "ENTER ({0} gems)" } },
            { "show_done", new[] { "Bu hafta katıldın", "Entered this week" } },
            { "show_place", new[] { "{0} şovda {1}. oldu!", "{0} placed #{1}!" } },
            { "show_prize_both", new[] { "{0} altın + {1} elmas", "{0} gold + {1} gems" } },
            { "show_prize_gold", new[] { "{0} altın", "{0} gold" } },
            { "show_hint", new[] { "Puan: mutluluk, sevgi, sağlık, komutlar ve kıyafetler. Rakipler önceden belli.", "Score: happiness, love, health, tricks and outfit. Rivals are known in advance." } },

            // Ev (tema)
            { "theme_title", new[] { "Oda teması", "Room theme" } },
            { "theme_classic", new[] { "Klasik", "Classic" } },
            { "theme_forest", new[] { "Orman", "Forest" } },
            { "theme_ocean", new[] { "Okyanus", "Ocean" } },
            { "theme_space", new[] { "Uzay", "Space" } },
            { "theme_princess", new[] { "Prenses", "Princess" } },
            { "theme_selected", new[] { "Seçili", "Selected" } },
            { "theme_pick", new[] { "Seç", "Pick" } },
            { "theme_pack", new[] { "Ev Paketi", "Home Pack" } },

            // Kulüp
            { "club_title", new[] { "Pati Kulübü", "Paw Club" } },
            { "club_active", new[] { "Kulüp üyesisin!", "You're a club member!" } },
            { "club_inactive", new[] { "Henüz kulüp üyesi değilsin", "Not a club member yet" } },
            { "club_benefits", new[] { "Her gün 1 mavi elmas\nMini oyunlarda +%50 altın\nHer ay 1 pansiyon bileti\nYıldızlar Şovu'na bedava giriş\nKulüp papyonu", "1 blue gem every day\n+50% gold in mini games\n1 boarding ticket every month\nFree Star Show entry\nClub bow tie" } },
            { "club_claim_diamond", new[] { "Bugünün elması", "Today's gem" } },
            { "club_claimed_today", new[] { "Bugün alındı", "Claimed today" } },
            { "club_claim_boarding", new[] { "Ayın bileti", "Monthly ticket" } },
            { "club_claimed_month", new[] { "Bu ay alındı", "Claimed this month" } },
            { "club_members_only", new[] { "Üyelere özel", "Members only" } },
            { "club_manage", new[] { "Aboneliği yönet / iptal et", "Manage / cancel subscription" } },
            { "club_terms", new[] { "Abonelik her dönem sonunda kendiliğinden yenilenir; fiyat KDV dahildir. Google Play > Abonelikler'den istediğin zaman iptal edebilirsin. Ücretsiz deneme bitmeden iptal edersen ücret alınmaz. Abonelik bitince aldığın kıyafetler sende kalır.", "Renews automatically each period; price includes VAT. Cancel anytime in Google Play > Subscriptions. Cancel before the free trial ends and you won't be charged. Items you got stay yours." } },
            { "per_month", new[] { "{0} / ay", "{0} / month" } },
            { "per_year", new[] { "{0} / yıl", "{0} / year" } },

            // Etkinlikler ve yeni kıyafetler
            { "event_newyear", new[] { "Yılbaşı etkinliği", "New Year event" } },
            { "event_children", new[] { "23 Nisan etkinliği", "Children's Day event" } },
            { "event_summer", new[] { "Yaz etkinliği", "Summer event" } },
            { "event_halloween", new[] { "Cadılar Bayramı etkinliği", "Halloween event" } },
            { "item_witch_hat", new[] { "Cadı şapkası", "Witch hat" } },
            { "item_santa_hat", new[] { "Noel şapkası", "Santa hat" } },
            { "item_sun_hat", new[] { "Hasır şapka", "Sun hat" } },
            { "item_star_bow", new[] { "Yıldızlı kurdele", "Star ribbon" } },
            { "item_flower_crown", new[] { "Çiçek taç", "Flower crown" } },
            { "item_star_glasses", new[] { "Yıldız gözlük", "Star glasses" } },
            { "item_cape", new[] { "Pelerin", "Cape" } },
            { "item_club_bow", new[] { "Kulüp papyonu", "Club bow tie" } },
            { "wardrobe_locked", new[] { "Şu an alınamaz", "Not available now" } },
            { "wardrobe_from_pass", new[] { "Pass'te", "In Pass" } },
            { "wardrobe_from_club", new[] { "Kulüpte", "Club only" } },

            // Foto stüdyosu ve yavru
            { "act_photo", new[] { "Foto", "Photo" } },
            { "photo_title", new[] { "Foto stüdyosu", "Photo studio" } },
            { "photo_shoot", new[] { "ÇEK!", "SNAP!" } },
            { "photo_saved", new[] { "Galeriye kaydedildi!", "Saved to gallery!" } },
            { "photo_old", new[] { "Bu telefonda kaydedilemiyor (Android 10+ gerekir)", "Can't save on this phone (needs Android 10+)" } },
            { "photo_error", new[] { "Fotoğraf kaydedilemedi", "Couldn't save the photo" } },
            { "photo_saving", new[] { "Kaydediliyor...", "Saving..." } },
            { "photo_denied", new[] { "Fotoğraflar izni gerekli: Ayarlar > Pati Dostum > Fotoğraflar", "Photos access needed: Settings > My Paw Pal > Photos" } },
            { "photo_locked", new[] { "Foto Paketi", "Photo Pack" } },
            { "pets_breed", new[] { "YAVRU", "BABY" } },
            { "pets_child_of", new[] { "Anne-baba: {0}", "Parents: {0}" } },
            { "breed_title", new[] { "{0} için yavru", "A baby for {0}" } },
            { "breed_cost", new[] { "{0} elmas ya da {1} altın", "{0} gems or {1} gold" } },
            { "breed_notadult", new[] { "İkisi de yetişkin olmalı", "Both must be adults" } },
            { "breed_notloved", new[] { "Sevgileri 70'in üstünde olmalı", "Both need love above 70" } },
            { "breed_sick", new[] { "İkisi de sağlıklı olmalı", "Both must be healthy" } },
            { "breed_differentspecies", new[] { "Aynı türden olmalılar", "Must be the same species" } },
            { "breed_noslot", new[] { "Boş bir dost slotu gerekli", "Needs a free pal slot" } },
            { "breed_toosoon", new[] { "Biraz beklemeleri gerek", "They need to wait a bit" } },
            { "breed_nomoney", new[] { "Yeterli paran yok", "Not enough money" } },
            { "breed_same", new[] { "", "" } },
            { "breed_done", new[] { "", "" } },

            // M5 ürünleri
            { "product_pati_club_monthly", new[] { "Pati Kulübü (aylık)", "Paw Club (monthly)" } },
            { "product_pati_club_monthly_desc", new[] { "Her gün elmas, +%50 altın ve daha fazlası. Her ay yenilenir, istediğin an iptal.", "Daily gems, +50% gold and more. Renews monthly, cancel anytime." } },
            { "product_pati_club_yearly", new[] { "Pati Kulübü (yıllık)", "Paw Club (yearly)" } },
            { "product_pati_club_yearly_desc", new[] { "Aylığa göre çok daha avantajlı. Her yıl yenilenir, istediğin an iptal.", "Much better value. Renews yearly, cancel anytime." } },
            { "product_pati_pass", new[] { "Pati Pass (bu ay)", "Paw Pass (this month)" } },
            { "product_pati_pass_desc", new[] { "Premium ödüller: elmas, çiçek taç, yıldız gözlük, prenses tacı", "Premium rewards: gems, flower crown, star glasses, tiara" } },
            { "product_premium_colors", new[] { "Özel Renkler", "Special Colors" } },
            { "product_premium_colors_desc", new[] { "Her türe 2 özel renk (Van kedisi, Husky, gökkuşağı...)", "2 special colors for every species" } },
            { "product_photo_pack", new[] { "Foto Paketi", "Photo Pack" } },
            { "product_photo_pack_desc", new[] { "Foto stüdyosuna 2 özel çerçeve", "2 special photo frames" } },
            { "product_decor_pack", new[] { "Ev Paketi", "Home Pack" } },
            { "product_decor_pack_desc", new[] { "Uzay ve Prenses oda temaları", "Space and Princess room themes" } },

            // Yaş ve büyüme
            { "age_days", new[] { "{0} günlük", "{0} days old" } },
            { "age_months", new[] { "{0} aylık", "{0} months old" } },
            { "age_years", new[] { "{0} yaşında", "{0} years old" } },
            { "stage_baby", new[] { "Bebek", "Baby" } },
            { "stage_young", new[] { "Yavru", "Young" } },
            { "stage_teen", new[] { "Genç", "Teen" } },
            { "stage_adult", new[] { "Yetişkin", "Adult" } },
            { "stage_senior", new[] { "Yaşlı", "Senior" } },
        };

#if UNITY_IOS && !UNITY_EDITOR
        /// <summary>iOS'ta Google Play yerine App Store / Apple Kimliği geçen metinler (Apple incelemesi başka mağaza adı istemez).</summary>
        private static readonly Dictionary<string, string[]> IosTexts = new Dictionary<string, string[]>
        {
            { "shop_note", new[] { "Fiyatlara KDV dahildir. Ödeme Apple Kimliğin üzerinden yapılır; dijital içerik hemen teslim edildiği için cayma hakkı yoktur, iadeler Apple'ın iade süreciyle (reportaproblem.apple.com) yürür.", "Prices include VAT. Payment is charged to your Apple Account; digital content is delivered immediately, refunds follow Apple's refund process (reportaproblem.apple.com)." } },
            { "club_terms", new[] { "Abonelik, dönem bitiminden en az 24 saat önce iptal edilmezse kendiliğinden yenilenir; ücret Apple Kimliğinden alınır, fiyat KDV dahildir. Ayarlar > Apple Kimliğin > Abonelikler'den istediğin zaman iptal edebilirsin. Ücretsiz deneme bitmeden iptal edersen ücret alınmaz. Abonelik bitince aldığın kıyafetler sende kalır.", "Renews automatically unless cancelled at least 24 hours before the period ends; charged to your Apple Account, price includes VAT. Cancel anytime in Settings > your Apple Account > Subscriptions. Cancel before the free trial ends and you won't be charged. Items you got stay yours." } },
            { "photo_saved", new[] { "Fotoğraflar'a kaydedildi!", "Saved to Photos!" } },
        };
#endif

        public const int TipCount = 5;

        private static bool loaded;
        private static Language current;

        /// <summary>Dil değiştiğinde tetiklenir; ekrandaki yazılar kendini yeniler.</summary>
        public static event Action Changed;

        public static Language Current
        {
            get
            {
                if (!loaded)
                {
                    Language fallback = Application.systemLanguage == SystemLanguage.Turkish
                        ? Language.Turkish
                        : Language.English;
                    current = (Language)PlayerPrefs.GetInt(PrefsKey, (int)fallback);
                    loaded = true;
                }
                return current;
            }
        }

        public static void Set(Language language)
        {
            if (loaded && current == language) return;
            current = language;
            loaded = true;
            PlayerPrefs.SetInt(PrefsKey, (int)language);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static string Get(string key)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (IosTexts.TryGetValue(key, out string[] ios)) return ios[(int)Current];
#endif
            return Texts.TryGetValue(key, out string[] values) ? values[(int)Current] : key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        /// <summary>Geçen süreyi kısa yazar: "12 dk", "3 sa 5 dk", "2 gün 4 sa".</summary>
        public static string Duration(double seconds)
        {
            long minutes = (long)(seconds / 60.0);
            if (minutes < 60) return Format("duration_min", minutes);
            long hours = minutes / 60;
            if (hours < 24) return Format("duration_hour", hours, minutes % 60);
            return Format("duration_day", hours / 24, hours % 24);
        }

        /// <summary>Hayvanın yaşı: "45 günlük", "7 aylık", "3 yaşında".</summary>
        public static string Age(double days)
        {
            if (days < 60) return Format("age_days", (int)days);
            if (days < 365.25) return Format("age_months", (int)(days / 30.44));
            return Format("age_years", (int)(days / 365.25));
        }
    }
}
