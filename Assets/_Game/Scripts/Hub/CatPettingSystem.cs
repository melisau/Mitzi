using UnityEngine;
using UnityEngine.UI;
using PawPath.Audio;
using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;
using PawPath.Localization;

namespace PawPath.Hub
{
    /// <summary>
    /// Hub'da kedi üzerine parmak gezdirince mırıldama, kalp partikülü, titreşim ve Love Point üretir.
    /// </summary>
    public class CatPettingSystem : MonoBehaviour
    {
        [Header("UI & Efekt Referansları")]
        [SerializeField] GameObject loveTextObject; // Anlık toplam puanı gösteren UI objesi
        [SerializeField] Text loveText;             // loveTextObject içindeki Text bileşeni
        [SerializeField] GameObject floatingTextPrefab; // Kedinin üstünden uçan +1 yazısı (Opsiyonel)

        [Header("Ayar ve Bileşenler")]
        [SerializeField] CatDefinition cat;
        [SerializeField] ParticleSystem hearts;
        [SerializeField] float petRadius = 1.15f;
        [SerializeField] float loveTickInterval = 0.55f;
        [SerializeField] int lovePerTick = 1;
        [SerializeField] Camera worldCamera;

        float tick;
        bool petting;
        Collider2D col;

        public CatDefinition Cat => cat;

        void Awake()
        {
            col = GetComponent<Collider2D>();
            if (worldCamera == null)
                worldCamera = Camera.main;

            // Eğer Inspector'da Text bileşeni atanmadıysa otomatik bulmayı dene
            if (loveText == null && loveTextObject != null)
            {
                loveText = loveTextObject.GetComponentInChildren<Text>();
            }
        }

        public void Bind(CatDefinition definition, ParticleSystem heartFx)
        {
            cat = definition;
            hearts = heartFx;
        }

        void Update()
        {
            // 1. Oyun Hub modunda değilse sevmeyi durdur
            if (GameFlow.Instance != null && !GameFlow.Instance.InHub)
            {
                StopPetting();
                return;
            }

            // 2. UI / Buton üzerindeyse sevmeyi durdur
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                StopPetting();
                return;
            }

            // 3. Ekrana basılmıyorsa sevmeyi durdur
            if (!PointerHeld())
            {
                StopPetting();
                return;
            }

            // Farenin/Parmağın oyun dünyasındaki koordinatını hesapla
            Vector2 world = ScreenToWorld(PointerScreen());
            
            // Parmak kedi alanının (Collider) üstünde mi?
            bool over = col != null ? col.OverlapPoint(world) : Vector2.Distance(world, transform.position) <= petRadius;
            
            if (over)
            {
                if (!petting)
                    BeginPetting();

                tick += Time.deltaTime;
                if (tick >= loveTickInterval)
                {
                    tick = 0f;
                    
                    if (CozyEconomyManager.Instance != null)
                    {
                        // Puan Ekle (GameEvents.LovePointsChanged otomatik tetiklenir)
                        CozyEconomyManager.Instance.AddLove(lovePerTick, GameText.PettingLove(cat));
                    }

                    // Güncel Sevgi Puanı metnini yenile
                    UpdateLoveTextDisplay();

                    // Uçuşan +1 Efekti (Eğer prefab bağlandıysa)
                    SpawnFloatingText();

                    Vibrate();
                }
            }
            else
            {
                StopPetting();
            }

            // =====================================================================
            // GİZLEME SİSTEMİ: SADECE KEDİ AKTİF SEVİLİRKEN METNİ GÖSTERİR
            // =====================================================================
            if (loveTextObject != null)
            {
                // Sadece kedi okşanıyorsa (petting == true) UI aktif kalır
                loveTextObject.SetActive(petting);
            }
            // =====================================================================
        }

        void BeginPetting()
        {
            petting = true;
            tick = loveTickInterval;

            if (hearts != null && !hearts.isPlaying)
                hearts.Play();

            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.StartPurr(cat != null ? cat.purrClip : null);

            UpdateLoveTextDisplay();
        }

        void StopPetting()
        {
            if (!petting)
                return;

            petting = false;
            tick = 0f;

            if (hearts != null && hearts.isPlaying)
                hearts.Stop();

            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.StopPurr();

            if (loveTextObject != null)
                loveTextObject.SetActive(false);
        }

        void UpdateLoveTextDisplay()
        {
            if (loveText != null && CozyEconomyManager.Instance != null)
            {
                loveText.text = $"💖 Sevgi: {CozyEconomyManager.Instance.LovePoints}";
            }
        }

        void SpawnFloatingText()
        {
            if (floatingTextPrefab != null)
            {
                Vector3 spawnPos = transform.position + new Vector3(0, 1.2f, 0);
                Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
            }
        }

        static void Vibrate()
        {
#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }

        static Vector3 PointerScreen()
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
        }

        static bool PointerHeld()
        {
            if (Input.touchCount > 0)
                return true;
            return Input.GetMouseButton(0);
        }

        Vector2 ScreenToWorld(Vector3 screen)
        {
            var cam = worldCamera != null ? worldCamera : Camera.main;
            screen.z = Mathf.Abs(cam.transform.position.z);
            var p = cam.ScreenToWorldPoint(screen);
            p.z = 0f;
            return p;
        }
    }
}