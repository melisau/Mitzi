using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Localization;

namespace PawPath.UI
{
    /// <summary>
    /// "Nasıl Oynanır" ekranı. Okuma bilmeyen çocuklar için her sayfa büyük,
    /// renkli ikonlarla anlatır; altındaki kısa yazı ebeveyn içindir.
    /// İlk açılışta bir kez kendiliğinden gösterilir; sonra Ayarlar'dan açılır.
    /// </summary>
    public class HowToPlayUI : MonoBehaviour
    {
        const string SeenKey = "PawPath.HowToPlaySeen.v1";

        static HowToPlayUI instance;

        struct Art
        {
            public Sprite icon;
            public Color iconColor;
            public Color background;
            public bool hasBackground;
        }

        struct Page
        {
            public string title;
            public string body;
            public Art[] art;
        }

        static readonly Color Cream = new Color(1f, 0.93f, 0.84f);
        static readonly Color Brown = new Color(0.63f, 0.43f, 0.31f);
        static readonly Color Food = new Color(0.90f, 0.69f, 0.33f);
        static readonly Color Water = new Color(0.36f, 0.63f, 0.90f);
        static readonly Color Sleep = new Color(0.58f, 0.50f, 0.82f);
        static readonly Color Love = new Color(0.90f, 0.42f, 0.58f);
        static readonly Color Play = new Color(0.45f, 0.72f, 0.38f);
        static readonly Color Caramel = new Color(0.66f, 0.49f, 0.37f);
        static readonly Color DrawMode = new Color(0.30f, 0.62f, 0.60f);
        static readonly Color Control = new Color(0.36f, 0.28f, 0.22f);
        static readonly Color DotOn = new Color(0.45f, 0.72f, 0.38f);
        static readonly Color DotOff = new Color(0.80f, 0.74f, 0.66f);

        GameObject panel;
        Text title;
        Text body;
        RectTransform artRoot;
        Image[] dots;
        Button previous;
        Button next;
        Button start;
        Page[] pages;
        int page;

        public static void Open()
        {
            if (instance != null)
                instance.Show(0);
        }

        public void Bind(GameObject panelRoot, Text titleText, Text bodyText, RectTransform art,
            Image[] pageDots, Button previousButton, Button nextButton, Button startButton, Button skipButton)
        {
            instance = this;
            panel = panelRoot;
            title = titleText;
            body = bodyText;
            artRoot = art;
            dots = pageDots;
            previous = previousButton;
            next = nextButton;
            start = startButton;
            pages = BuildPages();

            previous.onClick.AddListener(() => Show(page - 1));
            next.onClick.AddListener(() => Show(page + 1));
            start.onClick.AddListener(Close);
            skipButton.onClick.AddListener(Close);
            panel.SetActive(false);
        }

        public static int PageCount => 7;

        void Start()
        {
            if (PlayerPrefs.GetInt(SeenKey, 0) == 0)
                StartCoroutine(ShowFirstTime());
        }

        IEnumerator ShowFirstTime()
        {
            // Ev ekranı kurulup kedi yerleşsin, sonra açılsın.
            yield return new WaitForSecondsRealtime(0.8f);
            if (GameFlow.Instance == null || GameFlow.Instance.InHub)
                Show(0);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        void Show(int index)
        {
            if (pages == null || pages.Length == 0)
                return;
            page = Mathf.Clamp(index, 0, pages.Length - 1);
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);

            var current = pages[page];
            title.text = current.title;
            body.text = current.body;
            if (page >= 4 && page <= 8)
            {
                for (int i = artRoot.childCount - 1; i >= 0; i--)
                {
                    var child = artRoot.GetChild(i).gameObject;
                    child.SetActive(false);
                    if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
                }
                var stage = new GameObject("AnimatedExample", typeof(RectTransform));
                stage.transform.SetParent(artRoot, false);
                stage.transform.localScale = Vector3.one * 0.85f;
                stage.AddComponent<GameplayTutorialDemo>().Configure(page == 4 ? TutorialDemoKind.DrawPath : page == 5 ? TutorialDemoKind.Move : page == 6 ? TutorialDemoKind.DogStomp : page == 7 ? TutorialDemoKind.DogContact : TutorialDemoKind.BirdReward);
            }
            else BuildArt(current.art);

            for (int i = 0; i < dots.Length; i++)
                if (dots[i] != null)
                {
                    dots[i].color = i == page ? DotOn : DotOff;
                    dots[i].rectTransform.localScale = Vector3.one * (i == page ? 1.25f : 1f);
                }

            bool last = page == pages.Length - 1;
            previous.gameObject.SetActive(page > 0);
            next.gameObject.SetActive(!last);
            start.gameObject.SetActive(last);
        }

        void Close()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            panel.SetActive(false);
        }

        void BuildArt(Art[] items)
        {
            for (int i = artRoot.childCount - 1; i >= 0; i--)
            {
                    var child = artRoot.GetChild(i).gameObject;
                    child.SetActive(false);
                    if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
                }
            if (items == null || items.Length == 0)
                return;

            const float size = 190f;
            const float gap = 60f;
            float total = items.Length * size + (items.Length - 1) * gap;
            for (int i = 0; i < items.Length; i++)
            {
                float x = -total * 0.5f + size * 0.5f + i * (size + gap);
                var item = items[i];
                var holder = new GameObject("Art_" + i, typeof(RectTransform));
                holder.transform.SetParent(artRoot, false);
                var rt = holder.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, 0f);
                rt.sizeDelta = new Vector2(size, size);

                if (item.hasBackground)
                {
                    var bg = holder.AddComponent<Image>();
                    bg.sprite = CozyIconFactory.CircleButton();
                    bg.color = item.background;
                    bg.preserveAspect = true;
                    bg.raycastTarget = false;
                    var shadow = holder.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
                    shadow.effectDistance = new Vector2(0f, -6f);
                }

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(holder.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = new Vector2(0f, 2f);
                float iconSize = item.hasBackground ? size * 0.56f : size * 0.8f;
                iconRt.sizeDelta = new Vector2(iconSize, iconSize);
                var icon = iconGo.GetComponent<Image>();
                icon.sprite = item.icon;
                icon.color = item.iconColor;
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                // Sırayla hafifçe "nefes alsınlar": ekran canlı dursun.
                var pulse = holder.AddComponent<UiPulse>();
                pulse.Configure(0.05f, 2.6f + i * 0.35f);
            }
        }

        static Art OnCircle(Sprite icon, Color background) =>
            new Art { icon = icon, iconColor = Color.white, background = background, hasBackground = true };

        static Art Tinted(Sprite icon, Color iconColor, Color background) =>
            new Art { icon = icon, iconColor = iconColor, background = background, hasBackground = true };

        static Page[] BuildPages()
        {
            return new[]
            {
                new Page
                {
                    title = GameText.L("Hoş geldin!", "Welcome!", "Willkommen!"),
                    body = GameText.L("Kedini sev. Birlikte oyna!", "Pet your cat. Play together!", "Streichle deine Katze. Spielt zusammen!"),
                    art = new[]
                    {
                        Tinted(CozyIconFactory.HappyCatFace(), Brown, Cream),
                        OnCircle(CozyIconFactory.Heart(), Love),
                        OnCircle(CozyIconFactory.Paw(), Caramel)
                    }
                },
                new Page
                {
                    title = GameText.L("Kediyi sev", "Pet your cat", "Streichle deine Katze"),
                    body = GameText.L("Parmağınla okşa. Mırlamasını dinle.", "Stroke with your finger. Hear it purr.", "Streichle mit deinem Finger. Hör das Schnurren."),
                    art = new[]
                    {
                        Tinted(CozyIconFactory.PettingHand(), Color.white, Cream),
                        OnCircle(CozyIconFactory.Heart(), Love)
                    }
                },
                new Page
                {
                    title = GameText.L("Kedine bak", "Look after your cat", "Versorge deine Katze"),
                    body = GameText.L("Mama ver. Su ver. Uyut.", "Feed. Give water. Let it sleep.", "Füttern. Wasser geben. Schlafen lassen."),
                    art = new[]
                    {
                        OnCircle(CozyIconFactory.FoodBowl(), Food),
                        OnCircle(CozyIconFactory.WaterDrop(), Water),
                        OnCircle(CozyIconFactory.Moon(), Sleep)
                    }
                },
                new Page
                {
                    title = GameText.L("Maceraya çık", "Go on an adventure", "Auf ins Abenteuer"),
                    body = GameText.L("Yeşil oka bas. Oyununu seç.", "Tap green play. Choose your game.", "Tippe auf Grün. Wähle dein Spiel."),
                    art = new[]
                    {
                        OnCircle(CozyIconFactory.Play(), Play),
                        OnCircle(CozyIconFactory.Pencil(), DrawMode),
                        OnCircle(CozyIconFactory.Gamepad(), Water)
                    }
                },
                new Page
                {
                    title = GameText.L("Çizerek oyna", "Draw to play", "Zeichnen & spielen"),
                    body = GameText.L("Boşluğu çizerek birleştir. Yeşil oka bas.", "Draw across the gap. Tap green play.", "Zeichne über die Lücke. Tippe auf Grün."),
                    art = new[]
                    {
                        OnCircle(CozyIconFactory.Pencil(), DrawMode),
                        OnCircle(CozyIconFactory.ArrowRight(), Caramel),
                        OnCircle(CozyIconFactory.Star(), Food)
                    }
                },
                new Page
                {
                    title = GameText.L("Tuşlarla oyna", "Play with buttons", "Mit Tasten spielen"),
                    body = GameText.L("Okları basılı tutarak yürü.", "Hold the arrows to walk.", "Halte die Pfeile zum Laufen."),
                    art = new[]
                    {
                        OnCircle(CozyIconFactory.ArrowLeft(), Control),
                        OnCircle(CozyIconFactory.ArrowRight(), Control),
                        OnCircle(CozyIconFactory.ArrowUp(), Control)
                    }
                },
                new Page
                {
                    title = GameText.L("Üstüne zıpla", "Jump on top", "Spring darauf"),
                    body = GameText.L("Köpeğin üstüne in. Puf, kaybolur!", "Land on the dog. Poof, it disappears!", "Lande auf dem Hund. Puff, er verschwindet!"),
                    art = new Art[0]
                },
                new Page
                {
                    title = GameText.L("Dokunma!", "Avoid touching!", "Nicht berühren!"),
                    body = GameText.L("Köpeğe yandan dokunursan kaybedersin.", "Touching the dog's side ends the round.", "Berührst du den Hund seitlich, ist die Runde vorbei."),
                    art = new Art[0]
                },
                new Page
                {
                    title = GameText.L("Kuşu yakala", "Catch the bird", "Fang den Vogel"),
                    body = GameText.L("Kuşları topla. +3 puan kazan!", "Collect birds. Earn +3 points!", "Sammle Vögel. Bekomme +3 Punkte!"),
                    art = new Art[0]
                },
                new Page
                {
                    title = GameText.L("Topla ve süsle", "Collect and decorate", "Sammeln und einrichten"),
                    body = GameText.L("Para topla. Evini süsle. Kedileri kurtar!", "Collect coins. Decorate. Rescue cats!", "Sammle Münzen. Dekoriere. Rette Katzen!"),
                    art = new[]
                    {
                        Tinted(CozyIconFactory.Coin(), Color.white, Cream),
                        OnCircle(CozyIconFactory.Storefront(), Caramel),
                        Tinted(CozyIconFactory.HappyCatFace(), Brown, Cream)
                    }
                }
            };
        }
    }
}
