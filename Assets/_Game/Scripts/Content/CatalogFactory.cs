using System.Collections.Generic;
using UnityEngine;
using PawPath.Data;

namespace PawPath.Content
{
    /// <summary>
    /// Asset yokken bile oynanabilir yedek katalog. Editor menüsü aynı veriyi ScriptableObject'e yazar.
    /// </summary>
    public static class CatalogFactory
    {
        public static PawPathCatalog CreateRuntime()
        {
            var catalog = ScriptableObject.CreateInstance<PawPathCatalog>();
            catalog.cats = new List<CatDefinition>
            {
                Cat("mitzi", "Mitzi", 0, new Color(0.86f, 0.62f, 0.38f), CatPersonality.Curious,
                    "Evin ilk patisi. Güneş lekelerini takip etmeyi ve senin çizdiğin köprülerde minik adımlar atmayı sever.",
                    "Mitzi zaten seninle. Pencerenin önünde kuyruğunu kıvırıp 'hadi yol çiz' diyor."),
                Cat("pamuk", "Pamuk", 5, new Color(0.96f, 0.95f, 0.93f), CatPersonality.Sleepy,
                    "Bulut gibi, usul usul. En yumuşak mindere kıvrılıp mırıldanır.",
                    "Parkın bankının altında titreyen bir yumak buldun. Pamuk, burnunu parmağına değdirip 'beni de götürür müsün?' diye bakıyor. Evde ona bir güneş dilimi ayıracağız."),
                Cat("komur", "Kömür", 10, new Color(0.18f, 0.16f, 0.20f), CatPersonality.Shy,
                    "Gece kadar sessiz, kucak kadar sıcak. Çiçek tacını eğik takar.",
                    "Kömür, sokak lambasının gölgesinde tek başına oturuyor. Sen eğilince bir adım yaklaşıyor; sanki evinin kokusunu çoktan tanıyor."),
                Cat("tarcin", "Tarçın", 15, new Color(0.90f, 0.55f, 0.32f), CatPersonality.Playful,
                    "Üç renkli, üç kat meraklı. Tüy oltayı görünce dünya durur.",
                    "Tarçın, düşen yaprakların arasında zıplaya zıplaya sana geliyor. 'Bu yol biraz eğlenceliydi,' der gibi mırıldanıyor. Kapıyı açık bırakıyoruz."),
                Cat("ada", "Ada", 20, new Color(0.72f, 0.74f, 0.78f), CatPersonality.Brave,
                    "Çiçek taçlı, rüzgarı seven. Yüksek yerlerden bile sakin bakar.",
                    "Ada, çitlerin üstünde rüzgarla dans ediyordu. Seni görünce aşağı atlıyor ve alnını dizine yaslıyor. 'Ben de ailenin parçası olayım' diyor sessizce."),
                Cat("moka", "Moka", 25, new Color(0.45f, 0.32f, 0.24f), CatPersonality.Sleepy,
                    "Örgü atkılı, kahve kokulu. Şömineye en yakın yastığı bilir.",
                    "Moka, kış penceresinin önünde atkısı kaymış halde seni bekliyor. Bir okşamada ipliği düzelir, ikinci okşamada evinin yolunu öğrenir.")
            };

            catalog.shopItems = new List<ShopItemDefinition>
            {
                Item("tree", "Göğe Uzanan Tırmanma Ağacı", 80,
                    "Kat kat tahta, ip sarılı direkler ve en tepede bir bakış terası. Mitzi buradan bütün evi 'benim' diye ilan eder."),
                Item("cushion", "Bulut Peluş Minder", 35,
                    "İçine gömüldün mü çıkılmaz. Pamuk'un resmî uyku ofisi. Üzerine bir kıl bırakması iltifat sayılır."),
                Item("wand", "Tüy Olta", 45,
                    "Ucu tüy, sapı sabır. Tarçın için bu bir oyuncak değil, destandır. Sen savur, o uçsun."),
                Item("sweater", "El Örgüsü Kedi Kazağı", 60,
                    "Ilık yün, küçük pati delikleri. Moka giyince kış bile utançtan yumuşar. Çıkarmak isteyince mırıldanarak pazarlık eder.")
            };

            catalog.levels = new List<LevelDefinition>();
            for (int i = 1; i <= 30; i++)
            {
                var level = ScriptableObject.CreateInstance<LevelDefinition>();
                level.levelNumber = i;
                level.season = (SeasonId)((i - 1) / 5 % 3);
                level.spawnPoint = new Vector2(-6.2f, -1.15f);
                level.goalPoint = new Vector2(6.25f, -0.4f + (i % 5) * 0.35f);
                level.inkBudget = 16f + (i % 6) * 1.2f;
                level.fallY = -7.5f;
                catalog.levels.Add(level);
            }

            return catalog;
        }

        static CatDefinition Cat(string id, string name, int unlock, Color tint, CatPersonality p, string bio, string encounter)
        {
            var c = ScriptableObject.CreateInstance<CatDefinition>();
            c.id = id;
            c.displayName = name;
            c.unlockAfterLevel = unlock;
            c.furTint = tint;
            c.personality = p;
            c.bio = bio;
            c.encounterDialogue = encounter;
            return c;
        }

        static ShopItemDefinition Item(string id, string name, int cost, string desc)
        {
            var i = ScriptableObject.CreateInstance<ShopItemDefinition>();
            i.id = id;
            i.displayName = name;
            i.lovePointCost = cost;
            i.description = desc;
            return i;
        }
    }
}