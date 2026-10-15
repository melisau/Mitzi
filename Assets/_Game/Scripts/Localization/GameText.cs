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
        English = 1,
        German = 2
    }

    /// <summary>
    /// Oyuncu yüzü metinler (TR + EN + DE). Arayüz kodu metni <see cref="L"/> ile ister:
    /// <c>GameText.L("Kapat", "Close", "Schließen")</c>. Dil, ilk açılışta cihaz dilinden
    /// seçilir (Türkçe → Türkçe, Almanca → Almanca, diğerleri → İngilizce) ve Ayarlar'dan
    /// değiştirilebilir.
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
        public static bool IsGerman => Language == GameLanguage.German;

        /// <summary>Etkin dile göre Türkçe, İngilizce veya Almanca metni döndürür.</summary>
        public static string L(string tr, string en, string de)
        {
            switch (Language)
            {
                case GameLanguage.English: return en;
                case GameLanguage.German: return de;
                default: return tr;
            }
        }

        /// <summary>Sahne yüklenmeden önce dili okur (PlayerPrefs ana iş parçacığında çağrılmalı).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureLoaded()
        {
            if (loaded)
                return;
            loaded = true;
            if (PlayerPrefs.HasKey(LanguagePrefKey))
            {
                int saved = PlayerPrefs.GetInt(LanguagePrefKey);
                language = System.Enum.IsDefined(typeof(GameLanguage), saved)
                    ? (GameLanguage)saved
                    : GameLanguage.English;
                return;
            }

            switch (Application.systemLanguage)
            {
                case SystemLanguage.Turkish: language = GameLanguage.Turkish; break;
                case SystemLanguage.German: language = GameLanguage.German; break;
                default: language = GameLanguage.English; break;
            }
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

        public static string Title => L("Yol Çizen Pati", "Paw Path", "Pfotenpfad");
        public static string Play => L("Oyna", "Play", "Spielen");
        public static string House => L("Kedi Evi", "Cat House", "Katzenhaus");
        public static string Shop => L("Dükkan", "Shop", "Laden");
        public static string Continue => L("Eve Dön", "Go Home", "Nach Hause");
        public static string BackToCatHouse => L("Kedi Evine Dön", "Back to Cat House", "Zurück zum Katzenhaus");
        public static string InviteHome => L("Eve davet et", "Invite home", "Nach Hause einladen");
        public static string Love => L("Sevgi", "Love", "Liebe");
        public static string Ink => L("Fırça", "Ink", "Tinte");
        public static string Close => L("Kapat", "Close", "Schließen");
        public static string Settings => L("Ayarlar", "Settings", "Einstellungen");
        public static string LevelCompleted => L("Bölüm Tamamlandı!", "Level Complete!", "Level geschafft!");

        public static string LevelLabel(int n) => L($"Bölüm {n}", $"Level {n}", $"Level {n}");
        public static string LevelCompletedLabel(int n) =>
            L($"Bölüm {n} Tamamlandı!", $"Level {n} Complete!", $"Level {n} geschafft!");
        public static string ShopBalance(int total) =>
            L($"Dükkan Bakiyesi: {total}", $"Shop Balance: {total}", $"Guthaben: {total}");
        public static string Checked(string label, bool selected) => selected ? "✓ " + label : label;

        public static string PettingLove(CatDefinition cat)
        {
            var name = cat != null ? cat.displayName : L("Pati", "Kitty", "Kätzchen");
            return L($"{name} mırıldanarak sana bir kalp verdi!",
                $"{name} purred and gave you a heart!",
                $"{name} schnurrt und schenkt dir ein Herz!");
        }

        public static string MissYou(CatDefinition cat)
        {
            var name = cat != null ? cat.displayName : L("Kedin", "Your cat", "Deine Katze");
            return L($"{name} seni çok özledi!", $"{name} really missed you!", $"{name} hat dich sehr vermisst!");
        }

        public static string EncounterTitle(CatDefinition cat) =>
            cat != null
                ? L($"{cat.displayName} ile tanıştın", $"You met {cat.displayName}", $"Du hast {cat.displayName} kennengelernt")
                : L("Yeni bir pati", "A new paw", "Eine neue Pfote");

        public static string AffectionLabel(CatDefinition cat, int affection) =>
            L($"{cat.displayName} sevgisi: {affection}/100",
              $"{cat.displayName}'s love: {affection}/100",
              $"Liebe von {cat.displayName}: {affection}/100");

        public static string TutorialDrawing => L(
            "YOLU TAMAMLAMA\n\nÖnceki/Sonraki ile bölümün tamamını gezip yolları çiz.\nMürekkebin yarısı sonraki ekranlar için korunur.\nBitince Oyunu Başlat'a bas veya mürekkebi tüket.\n\nSiyah: Yol   Mavi: Zıpla   Beyaz: Buz   Silgi: Sil",
            "FINISH THE PATH\n\nUse Previous/Next to look over the whole level and draw your paths.\nHalf of your ink is saved for the next screens.\nWhen you're done, press Start Game or use up your ink.\n\nBlack: Path   Blue: Bounce   White: Ice   Eraser: Erase",
            "DEN WEG VOLLENDEN\n\nSieh dir mit Zurück/Weiter das ganze Level an und zeichne deine Wege.\nDie Hälfte der Tinte bleibt für die nächsten Bildschirme.\nWenn du fertig bist, tippe auf Spiel starten oder verbrauche die Tinte.\n\nSchwarz: Weg   Blau: Sprung   Weiß: Eis   Radierer: Löschen");

        public static string TutorialCity => L(
            "CADDE YOLU\n\nArabaların üzerinden atlamak için zıpla.\nKonteynırların bulunduğu çukurlara düşmemek için zamanında zıpla.\n\nDikkatli ilerle ve yolun sonundaki kapıya ulaş!",
            "CITY ROAD\n\nJump to hop over the cars.\nTime your jumps so you don't fall into the gaps by the containers.\n\nGo carefully and reach the gate at the end of the road!",
            "STADTSTRASSE\n\nSpring, um über die Autos zu hüpfen.\nSpring rechtzeitig, damit du nicht in die Lücken bei den Containern fällst.\n\nGeh vorsichtig und erreiche das Tor am Ende der Straße!");

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

        public static readonly string[] PushNotificationsDe =
        {
            "Deine Katze hat dich sehr vermisst!",
            "Pamuk schnurrt und schenkt dir ein Herz!",
            "Mitzi wartet am Fenster auf dich.",
            "Kömür hat dir einen Platz zum Kuscheln freigehalten.",
            "Tarçın träumt von einem neuen Weg. Zeichnest du ihn?",
            "Die Katzen wollen das sonnige Kissen mit dir teilen.",
            "Ada hat ihren Blumenkranz gerichtet und schaut zur Tür.",
            "Mokas Strickschal ist etwas verrutscht. Ein Streicheln würde helfen."
        };

        public static string[] PushNotifications =>
            IsGerman ? PushNotificationsDe : IsEnglish ? PushNotificationsEn : PushNotificationsTr;

        // ---- Katalog metinleri -------------------------------------------------------
        // Türkçe metin asset'teki alandan gelir; İngilizce ve Almancası id ile bu tablolardan
        // okunur. Bazı asset'lerde Türkçe alanı İngilizce girilmiş; onlar için düzeltme tablosu var.

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

        static readonly Dictionary<string, string> ItemNameDe = new Dictionary<string, string>
        {
            { "food_bowl", "Holz-Futternapf" },
            { "item_rug", "Teppich" },
            { "item_cat_bed", "Katzenbett" },
            { "cat_poster", "Katzenposter" },
            { "cat_sand", "Katzenklo" },
            { "cat_toy_mouse", "Spielzeugmaus" },
            { "cat_tree", "Kratzbaum" },
            { "cat_water", "Wassernapf" },
            { "tree", "Himmelhoher Kletterbaum" },
            { "cushion", "Wolken-Plüschkissen" },
            { "wand", "Federangel" },
            { "sweater", "Handgestrickter Katzenpulli" },
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

        static readonly Dictionary<string, string> ItemDescriptionDe = new Dictionary<string, string>
        {
            { "food_bowl", "Ein schicker, handgemachter Holznapf für das Lieblingsfutter deiner Katze." },
            { "item_rug", "Ein superweicher Teppich für deine Katze." },
            { "item_cat_bed", "Ein warmes, gemütliches Bett zum Einkuscheln." },
            { "cat_poster", "Ein fröhliches Katzenposter für die Wand." },
            { "cat_sand", "Ein sauberes, bequemes Katzenklo für deine Katzen." },
            { "cat_toy_mouse", "Eine weiche Maus, die Mitzi jagen und mit den Pfoten anstupsen kann." },
            { "cat_tree", "Zum Klettern, Ausruhen und um das ganze Haus im Blick zu haben." },
            { "cat_water", "Ein Wassernapf, aus dem deine Katzen trinken können." },
            { "tree", "Holzetagen, mit Seil umwickelte Pfosten und oben eine Aussichtsplattform. Von hier erklärt Mitzi das ganze Haus zu ihrem." },
            { "cushion", "Wer einmal einsinkt, kommt nicht mehr raus. Pamuks offizielles Schlafbüro. Ein Haar darauf gilt als Kompliment." },
            { "wand", "Vorne eine Feder, am Griff Geduld. Für Tarçın ist das kein Spielzeug, sondern ein Abenteuer. Du schwingst, sie fliegt." },
            { "sweater", "Warme Wolle, kleine Pfotenlöcher. Wenn Moka ihn trägt, wird sogar der Winter weich. Ausziehen? Da verhandelt sie schnurrend." },
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

        static readonly Dictionary<string, string> CatBioDe = new Dictionary<string, string>
        {
            { "mitzi", "Die erste Pfote im Haus. Sie jagt gern Sonnenflecken und macht kleine Schritte auf den Brücken, die du zeichnest." },
            { "pamuk", "Weich wie eine Wolke, leise wie ein Flüstern. Sie rollt sich auf dem weichsten Kissen zusammen und schnurrt." },
            { "komur", "Still wie die Nacht, warm wie eine Umarmung. Trägt ihren Blumenkranz leicht schief." },
            { "tarcin", "Dreifarbig und dreimal so neugierig. Wenn sie die Federangel sieht, steht die Welt still." },
            { "ada", "Mit Blumenkranz und voller Liebe zum Wind. Selbst von hoch oben bleibt sie ganz ruhig." },
            { "moka", "Strickschal und Kaffeeduft. Sie weiß, welches Kissen dem Kamin am nächsten ist." },
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

        static readonly Dictionary<string, string> CatEncounterDe = new Dictionary<string, string>
        {
            { "mitzi", "Mitzi ist schon bei dir. Sie ringelt am Fenster den Schwanz, als wollte sie sagen: 'Los, zeichne einen Weg.'" },
            { "pamuk", "Unter einer Parkbank hast du ein zitterndes Fellknäuel gefunden. Pamuk stupst mit der Nase deinen Finger an, als würde sie fragen: 'Nimmst du mich mit?' Zu Hause bekommt sie ein Stück Sonnenschein." },
            { "komur", "Kömür sitzt allein im Schatten einer Straßenlaterne. Als du dich bückst, kommt sie einen Schritt näher, als kenne sie den Duft von Zuhause schon." },
            { "tarcin", "Tarçın hüpft dir durch das fallende Laub entgegen und schnurrt, als wollte sie sagen: 'Der Weg hat Spaß gemacht.' Wir lassen die Tür offen." },
            { "ada", "Ada tanzte oben auf dem Zaun mit dem Wind. Als sie dich sieht, springt sie herunter und lehnt den Kopf an dein Knie. 'Lass mich auch zur Familie gehören', sagt sie leise." },
            { "moka", "Moka wartet am Winterfenster auf dich, ihr Schal ist verrutscht. Ein Streicheln richtet das Garn, beim zweiten lernt sie den Weg nach Hause." },
        };

        public static string ItemName(ShopItemDefinition item)
        {
            if (item == null)
                return "";
            switch (Language)
            {
                case GameLanguage.English: return Lookup(ItemNameEn, item.id, item.displayName);
                case GameLanguage.German: return Lookup(ItemNameDe, item.id, Lookup(ItemNameEn, item.id, item.displayName));
                default: return Lookup(ItemNameTrFix, item.id, item.displayName);
            }
        }

        public static string ItemDescription(ShopItemDefinition item)
        {
            if (item == null)
                return "";
            switch (Language)
            {
                case GameLanguage.English: return Lookup(ItemDescriptionEn, item.id, item.description);
                case GameLanguage.German: return Lookup(ItemDescriptionDe, item.id, Lookup(ItemDescriptionEn, item.id, item.description));
                default: return item.description;
            }
        }

        public static string CatBio(CatDefinition cat)
        {
            if (cat == null)
                return "";
            switch (Language)
            {
                case GameLanguage.English: return Lookup(CatBioEn, cat.id, cat.bio);
                case GameLanguage.German: return Lookup(CatBioDe, cat.id, Lookup(CatBioEn, cat.id, cat.bio));
                default: return cat.bio;
            }
        }

        public static string CatEncounter(CatDefinition cat)
        {
            if (cat == null)
                return "";
            switch (Language)
            {
                case GameLanguage.English: return Lookup(CatEncounterEn, cat.id, cat.encounterDialogue);
                case GameLanguage.German: return Lookup(CatEncounterDe, cat.id, Lookup(CatEncounterEn, cat.id, cat.encounterDialogue));
                default: return cat.encounterDialogue;
            }
        }

        static string Lookup(Dictionary<string, string> table, string id, string fallback) =>
            id != null && table.TryGetValue(id, out var value) ? value : fallback;
    }
}
