using System.Collections.Generic;
using UnityEngine;
using PawPath.Core;
using PawPath.Data;
using PawPath.Cat;

namespace PawPath.Hub
{
    /// <summary>
    /// Evdeki koleksiyon kedilerini spawner'lar ve oynanacak kediyi seçtirir.
    /// </summary>
    public class CatHouseManager : MonoBehaviour
    {
        public static CatHouseManager Instance { get; private set; }

        [SerializeField] Transform[] loungingSpots;
        [SerializeField] GameObject residentPrefab;
        [SerializeField] ParticleSystem heartPrefab;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            GameEvents.OnHubEntered += RefreshResidents;
            GameEvents.OnCatUnlocked += OnUnlocked;
        }

        void OnDisable()
        {
            GameEvents.OnHubEntered -= RefreshResidents;
            GameEvents.OnCatUnlocked -= OnUnlocked;
        }

        void OnUnlocked(CatDefinition _) => RefreshResidents();

        public void RefreshResidents()
        {
            if (GameFlow.Instance == null || GameFlow.Instance.Catalog == null)
                return;

            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Resident_"))
                    Destroy(child.gameObject);
            }

            var shuffledSpots = BuildShuffledSpotOrder();
            int slot = 0;
            foreach (var cat in GameFlow.Instance.Catalog.cats)
            {
                if (cat == null || !SaveService.HasCat(cat.id))
                    continue;
                if (SaveService.DeveloperMitziOnly && !cat.isStarterCat)
                    continue;
                int spotIndex = shuffledSpots.Count > 0
                    ? shuffledSpots[slot % shuffledSpots.Count]
                    : slot;
                SpawnResident(cat, spotIndex);
                slot++;
            }
        }

        List<int> BuildShuffledSpotOrder()
        {
            int count = loungingSpots != null ? loungingSpots.Length : 0;
            var order = new List<int>(count);
            for (int i = 0; i < count; i++)
                order.Add(i);

            for (int i = order.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (order[i], order[swapIndex]) = (order[swapIndex], order[i]);
            }
            return order;
        }

        void SpawnResident(CatDefinition cat, int slot)
        {
            Vector3 pos = transform.position + new Vector3(-2.4f + slot * 1.6f, -0.8f, 0f);
            if (loungingSpots != null && loungingSpots.Length > 0)
                pos = loungingSpots[slot % loungingSpots.Length].position;
            // Eski dinlenme noktalarının bazıları süpürgelik/duvar seviyesindeydi.
            // İlk kareden itibaren bütün kedileri gerçek zemin bandında tut.
            pos.y = Mathf.Clamp(pos.y, -1.82f, -1.12f);

            GameObject go;
            if (residentPrefab != null)
                go = Instantiate(residentPrefab, pos, Quaternion.identity, transform);
            else
                go = CreateFallbackResident(pos);

            go.name = $"Resident_{cat.id}";
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                if (cat.idleSprite != null)
                    sr.sprite = cat.idleSprite;
                sr.color = Color.white;
                // Ev sahnesinde kediler dekorların yanında kaybolmasın; bu ölçek
                // yalnızca resident görsellerine uygulanır, bölüm kedisini etkilemez.
                FitSpriteToHeight(sr, 2.20f * DisplaySizeSettings.CatScale);
                var residentAnimator = sr.GetComponent<Animator>();
                if (residentAnimator == null)
                    residentAnimator = sr.gameObject.AddComponent<Animator>();
                residentAnimator.runtimeAnimatorController = cat.animator;
                residentAnimator.enabled = cat.animator != null;
            }

            var residentCollider = go.GetComponent<CircleCollider2D>();
            if (residentCollider != null)
                residentCollider.radius = 0.95f;

            var pet = go.GetComponent<CatPettingSystem>();
            if (pet == null)
                pet = go.AddComponent<CatPettingSystem>();

            var homeBehaviour = go.GetComponent<CatHomeBehaviour>();
            if (homeBehaviour == null)
                homeBehaviour = go.AddComponent<CatHomeBehaviour>();
            homeBehaviour.BindDefinition(cat);
            if (cat.useDetailedHomeAnimations && sr != null && cat.sleepFrames != null && cat.sleepFrames.Length > 0)
            {
                var sleepAnimator = go.GetComponent<MitziSleepAnimator>();
                if (sleepAnimator == null)
                    sleepAnimator = go.AddComponent<MitziSleepAnimator>();
                sleepAnimator.Bind(cat, sr, homeBehaviour);
            }

            ParticleSystem hearts = go.GetComponentInChildren<ParticleSystem>();
            if (hearts == null && heartPrefab != null)
                hearts = Instantiate(heartPrefab, go.transform);
            pet.Bind(cat, hearts);

            var selector = go.GetComponent<PlayableCatButton>();
            if (selector == null)
                selector = go.AddComponent<PlayableCatButton>();
            selector.Bind(cat);
        }

        GameObject CreateFallbackResident(Vector3 pos)
        {
            var go = new GameObject("Resident");
            go.transform.SetParent(transform);
            go.transform.position = pos;
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform);
            vis.transform.localPosition = Vector3.zero;
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = FallbackSprite.WhiteCircle();
            sr.sortingOrder = 6;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.55f;
            return go;
        }

        public void Bind(Transform[] spots, GameObject prefab, ParticleSystem hearts)
        {
            loungingSpots = spots;
            residentPrefab = prefab;
            heartPrefab = hearts;
        }

        static void FitSpriteToHeight(SpriteRenderer renderer, float targetHeight)
        {
            if (renderer == null || renderer.sprite == null || renderer.sprite.bounds.size.y <= 0f)
                return;

            float scale = targetHeight / renderer.sprite.bounds.size.y;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public static class FallbackSprite
    {
        static Sprite cached;

        public static Sprite WhiteCircle()
        {
            if (cached != null)
                return cached;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var mid = new Vector2(15.5f, 15.5f);
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), mid);
                tex.SetPixel(x, y, d < 14f ? Color.white : Color.clear);
            }
            tex.Apply();
            cached = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            return cached;
        }

        static Sprite square;

        public static Sprite WhiteSquare()
        {
            if (square != null)
                return square;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return square;
        }
    }
}
