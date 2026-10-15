using System.Collections.Generic;
using PawPath.Content;
using PawPath.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PawPath.Localization
{
    public enum GameLanguage
    {
        Turkish = 0,
        English = 1
    }

    /// <summary>
    /// Oyuncu yüzü metinler (TR + EN). Arayüz kodu metni <see cref="L"/> ile ister:
    /// <c>GameText.L("Kapat", "Close")</c>. Dil, ilk açılışta cihaz dilinden seçilir
    /// (Türkçe değilse İngilizce) ve Ayarlar'dan değiştirilebilir.
    /// </summary>
    public static class GameText
    {
        const string LanguagePrefKey = "PawPath.Language";

        static bool loaded;
        static GameLanguage language;

        public static GameLanguage Language
        {
            get
            {
                EnsureLoaded();
                return language;
            }
        }

        public static bool IsEnglish => Language == GameLanguage.English;

        /// <summary>Etkin dile göre Türkçe veya İngilizce metni döndürür.</summary>
        public static string L(string tr, string en) => IsEnglish ? en : tr;

        /// <summary>Sahne yüklenmeden önce dili okur (PlayerPrefs ana iş parçacığında çağrılmalı).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureLoaded()
        {
            if (loaded)
                return;
            loaded = true;
            if (PlayerPrefs.HasKey(LanguagePrefKey))
                language = PlayerPrefs.GetInt(LanguagePrefKey) == (int)GameLanguage.English
                    ? GameLanguage.English
                    : GameLanguage.Turkish;
            else
                language = Application.systemLanguage == SystemLanguage.Turkish
                    ? GameLanguage.Turkish
                    : GameLanguage.English;
        }

        /// <summary>
        /// Dili kaydeder ve arayüz yeni dille kurulsun diye sahneyi yeniden yükler.
        /// İlerleme her değişiklikte zaten kaydedildiği için veri kaybı olmaz.
        /// </summary>
        public static void SetLanguage(GameLanguage newLanguage)
        {
            EnsureLoaded();
            PlayerPrefs.SetInt(LanguagePrefKey, (int)newLanguage);
            PlayerPrefs.Save();
            if (newLanguage == language)
                return;
            language = newLanguage;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ---- Sık kullanılan metinler -------------------------------------------------

        public static string Title => L("Yol Çizen Pati", "Paw Path");
        public static string Play => L("Oyna", "Play");
        public static string House => L("Kedi Evi", "Cat House");
        public static string Shop => L("Dükkan", "Shop");
        public static string Continue => L("Eve Dön", "Go Home");
        public static string BackToCatHouse => L("Kedi Evine Dön", "Back to Cat House");
        public static string InviteHome => L("Eve davet et", "Invite home");
        public static string Love => L("Sevgi", "Love");
        public static string Ink => L("Fırça", "Ink");
        public static string Close => L("Kapat", "Close");
        public static string Settings => L("Ayarlar", "Settings");
        public static string LevelCompleted => L("Bölüm Tamamlandı!", "Level Complete!");

        public static string LevelLabel(int n) => L($"Bölüm {n}", $"Level {n}");
        public static string LevelCompletedLabel(int n) => L($"Bölüm {n} Tamamlandı!", $"Level {n} Complete!");
        public static string ShopBalance(int total) => L($"Dükkan Bakiyesi: {total}", $"Shop Balance: {total}");
        public static string Checked(string label, bool selected) => selected ? "✓ " + label : label;

        public static string PettingLove(CatDefinition cat)
        {
            var name = cat != null ? cat.displayName : L("Pati", "Kitty");
            return L($"{name} mırıldanarak sana bir kalp verdi!", $"{name} purred and gave you a heart!");
        }

        public static string MissYou(CatDefinition cat)
        {
            var name = cat != null ? cat.displayName : L("Kedin", "Your cat");
            return L($"{name} seni çok özledi!", $"{name} really missed you!");
        }

        public static string EncounterTitle(CatDefinition cat) =>
            cat != null
                ? L($"{cat.displayName} ile tanıştın", $"You met {cat.displayName}")
                : L("Yeni bir pati", "A new paw");

        public static string AffectionLabel(CatDefinition cat, int affection) =>
            L($"{cat.displayName} sevgisi: {affection}/100", $"{cat.displayName}'s love: {affection}/100");

        public static string TutorialDrawing => L(
            "YOLU TAMAMLAMA\n\nÖnceki/Sonraki ile bölümün tamamını gezip yolları çiz.\nMürekkebin yarısı sonraki ekranlar için korunur.\nBitince Oyunu Başlat'a bas veya mürekkebi tüket.\n\nSiyah: Yol   Mavi: Zıpla   Beyaz: Buz   Silgi: Sil",
            "FINISH THE PATH\n\nUse Previous/Next to look over the whole level and draw your paths.\nHalf of your ink is saved for the next screens.\nWhen you're done, press Start Game or use up your ink.\n\nBlack: Path   Blue: Bounce   White: Ice   Eraser: Erase");

        public static string TutorialCity => L(
            "CADDE YOLU\n\nArabaların üzerinden atlamak için zıpla.\nKonteynırların bulunduğu çukurlara düşmemek için zamanında zıpla.\n\nDikkatli ilerle ve yolun sonundaki kapıya ulaş!",
            "CITY ROAD\n\nJump to hop over the cars.\nTime your jumps so you don't fall into the gaps by the containers.\n\nGo carefully and reach the gate at the end of the road!");

        public static readonly string[] PushNotificationsTr =
        {
            "Kedin seni çok özledi!",
            "Pamuk mırıldanarak sana bir kalp verdi!",
            "Mitzi pencerenin kenarında seni bekliyor.",
            "Kömür, kucağında bir şekerleme yeri ayırdı.",
            "Tarçın yeni bir yol hayali kuruyor. Çizer misin?",
            "Evdeki patiler güneşli yastığı seninle paylaşmak istiyor.",
            "Ada, çiçek tacını düzeltip kapıya bakıyor.",
            "Moka'nın örgüsü biraz kaymış. Bir okşama iyi gelir."
        };

        public static readonly string[] PushNotificationsEn =
        {
            "Your cat really missed you!",
            "Pamuk purred and gave you a heart!",
            "Mitzi is waiting for you by the window.",
            "Kömür saved you a nap spot on your lap.",
            "Tarçın is dreaming of a new path. Will you draw it?",
            "The cats want to share the sunny cushion with you.",
            "Ada fixed her flower crown and is watching the door.",
            "Moka's knitted scarf has slipped a bit. A pet would help."
        };

        public static string[] PushNotifications => IsEnglish ? PushNotificationsEn : PushNotificationsTr;

        // ---- Katalog metinleri -------------------------------------------------------
        // Türkçe metin asset'teki alandan gelir; İngilizcesi id ile bu tablodan okunur.
        // Bazı asset'lerde Türkçe alanı İngilizce girilmiş; onlar için Türkçe düzeltme tablosu var.

        static readonly Dictionary<string, string> ItemNameTrFix = new Dictionary<string, string>
        {
            { "food_bowl", "Ahşap Mama Kabı" },
            { "item_rug", "Kilim" },
            { "item_cat_bed", "Kedi Yatağı" },
        };

        static readonly Dictionary<string, string> ItemNameEn = new Dictionary<string, string>
        {
            { "food_bowl", "Wooden Food Bowl" },
            { "item_rug", "Rug" },
            { "item_cat_bed", "Cat Bed" },
            { "cat_poster", "Cat Poster" },
            { "cat_sand", "Litter Box" },
            { "cat_toy_mouse", "Toy Mouse" },
            { "cat_tree", "Cat Tree" },
            { "cat_water", "Water Bowl" },
            { "tree", "Sky-High Climbing Tree" },
            { "cushion", "Cloud Plush Cushion" },
            { "wand", "Feather Wand" },
            { "sweater", "Hand-Knit Cat Sweater" },
        };

        static readonly Dictionary<string, string> ItemDescriptionEn = new Dictionary<string, string>
        {
            { "food_bowl", "A stylish, handmade wooden bowl for your cat's favorite food." },
            { "item_rug", "A super-soft rug for your cat." },
            { "item_cat_bed", "A warm, cozy bed to curl up in." },
            { "cat_poster", "A cheerful cat poster for the wall." },
            { "cat_sand", "A clean, comfy litter area for your cats." },
            { "cat_toy_mouse", "A soft mouse Mitzi can chase and bat around with her paws." },
            { "cat_tree", "For climbing, resting and watching over the house." },
            { "cat_water", "A water bowl for your cats to drink from." },
            { "tree", "Layers of wood, rope-wrapped posts and a lookout deck at the top. From here, Mitzi declares the whole house 'mine'." },
            { "cushion", "Once you sink in, there's no getting out. Pamuk's official nap office. Leaving a hair on it counts as a compliment." },
            { "wand", "A feather on the tip, patience on the handle. For Tarçın it's not a toy, it's an epic. You swing, she flies." },
            { "sweater", "Warm wool, tiny paw holes. When Moka wears it, even winter softens. Try taking it off and she'll purr-negotiate." },
        };

        static readonly Dictionary<string, string> CatBioEn = new Dictionary<string, string>
        {
            { "mitzi", "The first paw of the house. She loves chasing sunbeams and taking tiny steps on the bridges you draw." },
            { "pamuk", "Soft as a cloud, quiet as a whisper. She curls up on the softest cushion and purrs." },
            { "komur", "Quiet as the night, warm as a hug. Wears her flower crown slightly tilted." },
            { "tarcin", "Three colors, three times as curious. The world stops when she sees the feather wand." },
            { "ada", "Flower-crowned and wind-loving. She stays calm even from up high." },
            { "moka", "Knitted scarf, coffee scent. She knows which cushion is closest to the fireplace." },
        };

        static readonly Dictionary<string, string> CatEncounterEn = new Dictionary<string, string>
        {
            { "mitzi", "Mitzi is already with you. She curls her tail by the window as if to say, 'come on, draw a path.'" },
            { "pamuk", "Under a park bench you found a shivering ball of fluff. Pamuk touches her nose to your finger as if asking, 'will you take me too?' We'll save her a slice of sunshine at home." },
            { "komur", "Kömür sits alone in the shadow of a streetlamp. When you lean down, she steps closer, as if she already knows the scent of home." },
            { "tarcin", "Tarçın comes bouncing toward you through the falling leaves, purring as if to say, 'that path was kind of fun.' We'll leave the door open." },
            { "ada", "Ada was dancing with the wind on top of the fence. When she sees you, she hops down and rests her head on your knee. 'Let me be part of the family too,' she says quietly." },
            { "moka", "Moka is waiting for you by the winter window, her scarf slipping. One pet straightens the yarn; the second, and she learns the way home." },
        };

        public static string ItemName(ShopItemDefinition item)
        {
            if (item == null)
                return "";
            if (IsEnglish)
                return Lookup(ItemNameEn, item.id, item.displayName);
            return Lookup(ItemNameTrFix, item.id, item.displayName);
        }

        public static string ItemDescription(ShopItemDefinition item)
        {
            if (item == null)
                return "";
            return IsEnglish ? Lookup(ItemDescriptionEn, item.id, item.description) : item.description;
        }

        public static string CatBio(CatDefinition cat)
        {
            if (cat == null)
                return "";
            return IsEnglish ? Lookup(CatBioEn, cat.id, cat.bio) : cat.bio;
        }

        public static string CatEncounter(CatDefinition cat)
        {
            if (cat == null)
                return "";
            return IsEnglish ? Lookup(CatEncounterEn, cat.id, cat.encounterDialogue) : cat.encounterDialogue;
        }

        static string Lookup(Dictionary<string, string> table, string id, string fallback) =>
            id != null && table.TryGetValue(id, out var value) ? value : fallback;
    }
}
