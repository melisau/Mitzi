using UnityEngine;
using PawPath.Data;

namespace PawPath.Audio
{
    /// <summary>
    /// Mırıldama loop, mevsim ambiyansı, fırça ve baloncuk SFX.
    /// Klipler Inspector'dan atanır; yoksa sessizce atlanır.
    /// </summary>
    public class CozyAudioManager : MonoBehaviour
    {
        const string MusicMutedKey = "PawPath.Audio.MusicMuted";
        const string EffectsMutedKey = "PawPath.Audio.EffectsMuted";
        public static CozyAudioManager Instance { get; private set; }

        public bool MusicMuted { get; private set; }
        public bool EffectsMuted { get; private set; }

        [SerializeField] AudioSource music;
        [SerializeField] AudioSource ambience;
        [SerializeField] AudioSource purr;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioSource meow;
        AudioSource dogBark;
        AudioClip naturalDogBark, robotDogBark;
        SpriteRenderer barkingDog;
        double nextDogBarkTime;
        double nextMeowTime;

        [SerializeField] AudioClip musicLoop;
        [SerializeField] AudioClip summerAmbience;
        [SerializeField] AudioClip autumnAmbience;
        [SerializeField] AudioClip winterAmbience;
        [SerializeField] AudioClip defaultPurr;
        [SerializeField] AudioClip brushSfx;
        [SerializeField] AudioClip bubbleSfx;
        [SerializeField] AudioClip bowlSfx;
        [SerializeField] AudioClip meowSfx;
        AudioClip homeMusic;
        AudioClip streetMusic;
        AudioClip forestMusic;
        AudioClip cityMusic;
        AudioClip stepSfx;
        AudioClip[] meowVariants;
        AudioClip hurtSfx;
        AudioClip fallSfx;
        AudioClip rescueSfx;
        AudioClip errorSfx;
        AudioClip uiClickSfx;
        AudioClip uiConfirmSfx;
        AudioClip shopBuySfx;
        AudioClip shopSellSfx;

        void Awake()
        {
            Instance = this;
            music = Ensure(music, "Music", 0.28f, true);
            ambience = Ensure(ambience, "Ambience", 0.35f, true);
            purr = Ensure(purr, "Purr", 0.55f, true);
            sfx = Ensure(sfx, "Sfx", 0.8f, false);
            // A dedicated, single voice prevents repeated cat/UI events from summing
            // several meows, without interrupting footsteps or confirmation sounds.
            meow = Ensure(meow, "Meow", 0.8f, false);
            meow.spatialBlend = 0f;
            meow.pitch = 1f;
            dogBark = Ensure(dogBark, "DogBark", .20f, false);
            dogBark.spatialBlend = 0f;
            naturalDogBark = Resources.Load<AudioClip>("Audio/DogBark");
            robotDogBark = Resources.Load<AudioClip>("Audio/RobotDogBark");
            MusicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
            EffectsMuted = PlayerPrefs.GetInt(EffectsMutedKey, 0) == 1;
            ApplyMuteState();
            if (musicLoop != null)
            {
                music.clip = musicLoop;
                music.Play();
            }
        }

        AudioSource Ensure(AudioSource existing, string name, float volume, bool loop)
        {
            if (existing != null)
            {
                existing.loop = loop;
                existing.playOnAwake = false;
                existing.volume = volume;
                return existing;
            }

            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop = loop;
            src.playOnAwake = false;
            src.volume = volume;
            src.spatialBlend = 0f;
            return src;
        }

        public void PlaySeason(SeasonId season)
        {
            AudioClip clip = season switch
            {
                SeasonId.Winter => winterAmbience,
                SeasonId.Autumn => autumnAmbience,
                _ => summerAmbience
            };
            if (clip == null)
                return;
            ambience.clip = clip;
            ambience.Play();
        }

        public void StartPurr(AudioClip overrideClip)
        {
            purr.clip = overrideClip != null ? overrideClip : defaultPurr;
            if (purr.clip == null)
                return;
            if (!purr.isPlaying)
                purr.Play();
        }

        public void StopPurr()
        {
            if (purr.isPlaying)
                purr.Stop();
        }

        public void PlayBrush() => PlayOne(brushSfx);
        public void PlayBubble() => PlayOne(bubbleSfx);
        public void PlayBowl() => PlayOne(bowlSfx);
        // Kısa miyavlar: uzun kayıtlar (ör. ~13 sn'lik cat_meow.wav) hiç çalınmaz.
        const float MaxMeowSeconds = 3f;

        public void PlayMeow()
        {
            if (meowSfx != null && meowSfx.length <= MaxMeowSeconds)
                PlayMeowClip(meowSfx);
            else
                PlayRandomMeow();
        }
        public void PlayStep() => PlayOne(stepSfx, 0.34f);
        public void PlayHurt() => PlayOne(hurtSfx);
        public void PlayFall() => PlayOne(fallSfx);
        public void PlayRescue() => PlayOne(rescueSfx);
        public void PlayError() => PlayOne(errorSfx);
        public void PlayUiClick() => PlayOne(uiClickSfx, 0.65f);
        public void PlayConfirm() => PlayOne(uiConfirmSfx);
        public void PlayShopBuy() => PlayOne(shopBuySfx);
        public void PlayShopSell() => PlayOne(shopSellSfx);

        public void PlayRandomMeow()
        {
            if (meowVariants == null || meowVariants.Length == 0)
            {
                // Liste boşsa yalnızca kısa tekil miyav çalınır (PlayMeow'a geri dönülmez).
                if (meowSfx != null && meowSfx.length <= MaxMeowSeconds)
                    PlayMeowClip(meowSfx);
                return;
            }
            PlayMeowClip(meowVariants[Random.Range(0, meowVariants.Length)]);
        }

        void PlayMeowClip(AudioClip clip)
        {
            if (clip == null || clip.length > MaxMeowSeconds || meow == null ||
                !isActiveAndEnabled || EffectsMuted || meow.isPlaying || Time.unscaledTimeAsDouble < nextMeowTime)
                return;
            // Unscaled time also protects consecutive events while the UI pauses timeScale.
            nextMeowTime = Time.unscaledTimeAsDouble + clip.length + .12d;
            meow.clip = clip;
            meow.Play();
        }

        void OnDisable()
        {
            if (dogBark != null) dogBark.Stop();
            barkingDog = null;
            if (meow != null) meow.Stop();
            nextMeowTime = 0d;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ToggleMusic()
        {
            MusicMuted = !MusicMuted;
            PlayerPrefs.SetInt(MusicMutedKey, MusicMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMuteState();
        }

        public void ToggleEffects()
        {
            EffectsMuted = !EffectsMuted;
            PlayerPrefs.SetInt(EffectsMutedKey, EffectsMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMuteState();
        }

        void ApplyMuteState()
        {
            if (dogBark != null)
            {
                dogBark.mute = EffectsMuted;
                if (EffectsMuted) { dogBark.Stop(); barkingDog = null; }
            }
            if (music != null) music.mute = MusicMuted;
            if (ambience != null) ambience.mute = MusicMuted;
            if (sfx != null) sfx.mute = EffectsMuted;
            if (purr != null) purr.mute = EffectsMuted;
            if (meow != null)
            {
                meow.mute = EffectsMuted;
                if (EffectsMuted)
                {
                    meow.Stop();
                    nextMeowTime = 0d;
                }
            }
        }

        public void PlayHomeMusic() => SwitchMusic(homeMusic);

        public static bool DogVisible(SpriteRenderer dog)
        {
            var camera = Camera.main;
            if (dog == null || !dog.enabled || !dog.gameObject.activeInHierarchy || camera == null) return false;
            var bounds = dog.bounds;
            Vector3 min = camera.WorldToViewportPoint(bounds.min), max = camera.WorldToViewportPoint(bounds.max);
            return max.x > 0 && min.x < 1 && max.y > 0 && min.y < 1 && max.z > 0;
        }

        public bool TryPlayDogBark(SpriteRenderer dog, bool robotic)
        {
            if (!isActiveAndEnabled || dogBark == null || EffectsMuted ||
                !PawPath.Levels.MobSimulationGate.GameplayActive || !DogVisible(dog) ||
                dogBark.isPlaying || Time.timeAsDouble < nextDogBarkTime) return false;
            AudioClip clip = robotic ? robotDogBark : naturalDogBark;
            if (clip == null) return false;
            barkingDog = dog; dogBark.clip = clip;
            dogBark.panStereo = Mathf.Clamp((Camera.main.WorldToViewportPoint(dog.bounds.center).x - .5f) * .5f, -.25f, .25f);
            dogBark.Play();
            nextDogBarkTime = Time.timeAsDouble + clip.length + 6d;
            return true;
        }

        void Update()
        {
            if (dogBark != null && (!PawPath.Levels.MobSimulationGate.GameplayActive || EffectsMuted || !DogVisible(barkingDog)))
            {
                dogBark.Stop(); barkingDog = null;
            }
        }

        public void PlayThemeMusic(int theme)
        {
            AudioClip clip = theme == 2 || theme == 3 ? cityMusic : theme == 1 ? forestMusic : streetMusic;
            SwitchMusic(clip);
        }

        void SwitchMusic(AudioClip clip)
        {
            if (clip == null || music == null || music.clip == clip)
                return;
            music.clip = clip;
            music.Play();
        }

        void PlayOne(AudioClip clip)
        {
            if (clip == null || sfx == null)
                return;
            sfx.PlayOneShot(clip);
        }

        void PlayOne(AudioClip clip, float volumeScale)
        {
            if (clip == null || sfx == null)
                return;
            sfx.PlayOneShot(clip, volumeScale);
        }

        public void BindLibrary(PawPathCatalog catalog)
        {
            if (catalog == null)
                return;
            homeMusic = catalog.homeMusic;
            streetMusic = catalog.streetMusic;
            forestMusic = catalog.forestMusic;
            cityMusic = catalog.cityMusic;
            stepSfx = catalog.catStepSfx;
            defaultPurr = catalog.catPurrSfx;
            meowVariants = catalog.catMeowSfx != null
                ? System.Array.FindAll(catalog.catMeowSfx, c => c != null && c.length <= MaxMeowSeconds)
                : null;
            hurtSfx = catalog.catHurtSfx;
            fallSfx = catalog.catFallSfx;
            rescueSfx = catalog.catRescueSfx;
            errorSfx = catalog.errorSfx;
            uiClickSfx = catalog.uiClickSfx;
            uiConfirmSfx = catalog.uiConfirmSfx;
            shopBuySfx = catalog.shopBuySfx;
            shopSellSfx = catalog.shopSellSfx;
            PlayHomeMusic();
        }

        public void BindClips(AudioClip musicClip, AudioClip purrClip, AudioClip brush, AudioClip bubble)
        {
            musicLoop = musicClip;
            defaultPurr = purrClip;
            brushSfx = brush;
            bubbleSfx = bubble;
        }
    }
}
