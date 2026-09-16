using PawPath.Data;

namespace PawPath.Localization
{
    /// <summary>
    /// Tüm oyuncu yüzü metinleri tek yerde (TR). UI bu sınıfı okur.
    /// </summary>
    public static class GameText
    {
        public const string Title = "Yol Çizen Pati";
        public const string Play = "Yola Çık";
        public const string House = "Kedi Evi";
        public const string Shop = "Dükkan";
        public const string Collection = "Patiler";
        public const string Continue = "Eve Dön";
        public const string InviteHome = "Eve davet et";
        public const string Selected = "Bugün oyna";
        public const string PlayingAs = "Şimdi oynayan";
        public const string Love = "Sevgi";
        public const string Ink = "Fırça";
        public const string Level = "Bölüm";
        public const string Owned = "Bizde var";
        public const string NotEnoughLove = "Biraz daha mırıldama lazım.";
        public const string Buy = "Sevgiyle al";

        public static string PettingLove(CatDefinition cat)
        {
            var name = cat != null ? cat.displayName : "Pati";
            return $"{name} mırıldanarak sana bir kalp verdi!";
        }

        public static string MissYou(CatDefinition cat)
        {
            var name = cat != null ? cat.displayName : "Kedin";
            return $"{name} seni çok özledi!";
        }

        public static string LevelLabel(int n) => $"Bölüm {n}";

        public static readonly string[] PushNotifications =
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

        public static string EncounterTitle(CatDefinition cat) =>
            cat != null ? $"{cat.displayName} ile tanıştın" : "Yeni bir pati";
    }
}
