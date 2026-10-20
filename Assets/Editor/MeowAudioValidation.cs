using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Audio;
using PawPath.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MeowAudioValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Hidden).GetValue(target);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Hidden).SetValue(target, value);
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project.");
        int oldEffects = PlayerPrefs.GetInt("PawPath.Audio.EffectsMuted", 0);
        float oldTimeScale = Time.timeScale;
        var oldRandom = UnityEngine.Random.state;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PlayerPrefs.SetInt("PawPath.Audio.EffectsMuted", 0);
            new GameObject("AudioValidationListener").AddComponent<AudioListener>();
            var audio = new GameObject("MeowValidationAudio").AddComponent<CozyAudioManager>();
            Call(audio, "Awake");
            var source = Get<AudioSource>(audio, "meow");
            var sfx = Get<AudioSource>(audio, "sfx");
            var purr = Get<AudioSource>(audio, "purr");
            Check(source != null && source != sfx && source != purr, "Meows share an overlapping SFX voice");
            Check(source.volume == .8f && source.spatialBlend == 0f && source.pitch == 1f && !source.loop,
                "Meow playback gain/pitch/spatial settings changed");
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            audio.BindLibrary(catalog);
            var variants = Get<AudioClip[]>(audio, "meowVariants");
            Check(variants.Length == 3, "Expected three short meow variants");
            string output = Environment.GetEnvironmentVariable("MITZI_MEOW_OUTPUT") ??
                Path.Combine(Path.GetTempPath(), "mitzi-meow-imports");
            Directory.CreateDirectory(output);
            foreach (var clip in variants)
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                Check(File.ReadAllText(AssetDatabase.GetAssetPath(clip) + ".meta").Contains("normalize: 0") && !importer.forceToMono &&
                    importer.defaultSampleSettings.compressionFormat == AudioCompressionFormat.PCM,
                    "Importer changes calibrated levels: " + clip.name);
                Check(clip.channels == 2 && clip.frequency == 48000 && clip.length <= 3,
                    "Meow format/duration inconsistent: " + clip.name);
                clip.LoadAudioData();
                var samples = new float[clip.samples * clip.channels];
                Check(clip.GetData(samples, 0), "Cannot inspect imported meow: " + clip.name);
                float peak = samples.Max(value => Mathf.Abs(value));
                Check(peak < .23f, "Imported meow exceeds calibrated peak: " + clip.name);
                WriteWave(Path.Combine(output, "unity-" + clip.name + ".wav"), samples, clip.frequency, clip.channels);

                source.Stop(); Set(audio, "nextMeowTime", 0d);
                Set(audio, "meowSfx", clip);
                audio.PlayMeow();
                Check(source.clip == clip, "Single meow bypasses dedicated voice");
                double next = Get<double>(audio, "nextMeowTime");
                Check(next > Time.unscaledTimeAsDouble + clip.length, "Meow cooldown missing");
                for (int i = 0; i < 50; i++) { audio.PlayRandomMeow(); audio.PlayMeow(); }
                Check(source.clip == clip && Get<double>(audio, "nextMeowTime") == next,
                    "Repeated cat events overlap/restart meows");
                // Stopping a voice early must not bypass the reserved window, even in paused UI.
                source.Stop(); Time.timeScale = 0;
                audio.PlayRandomMeow();
                Check(Get<double>(audio, "nextMeowTime") == next, "Paused UI bypasses meow guard");
                Time.timeScale = oldTimeScale;
            }
            audio.ToggleEffects();
            Check(source.mute && sfx.mute && purr.mute, "Effects mute does not cover meows");
            Set(audio, "nextMeowTime", 0d); source.clip = null;
            audio.PlayMeow(); audio.PlayRandomMeow();
            Check(source.clip == null && Get<double>(audio, "nextMeowTime") == 0d, "Muted meows queued playback");
            audio.ToggleEffects();
            Check(!source.mute && !sfx.mute && !purr.mute, "Unmute did not restore meow controls");
            audio.PlayMeow(); Check(source.clip != null, "Unmuting cannot play meows");
            Call(audio, "OnDisable");
            Check(Get<double>(audio, "nextMeowTime") == 0d, "Disable left stale cooldown");
            var longClip = AudioClip.Create("RejectedLongMeow", 48000 * 4, 2, 48000, false);
            source.clip = null;
            Set(audio, "meowVariants", new AudioClip[0]); Set(audio, "meowSfx", longClip);
            audio.PlayMeow();
            Check(source.clip == null, "Long meow fallback bypasses duration limit");
            UnityEngine.Object.DestroyImmediate(longClip);
            Debug.Log("MEOW_AUDIO_VALIDATION_PASS clips=3 rapidCalls=300 import=PCM48k " +
                "singleVoice=pass pausedUI=pass mute=pass fallback=pass output=" + output);
        }
        finally
        {
            PlayerPrefs.SetInt("PawPath.Audio.EffectsMuted", oldEffects);
            PlayerPrefs.Save();
            Time.timeScale = oldTimeScale;
            UnityEngine.Random.state = oldRandom;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    static void WriteWave(string path, float[] samples, int rate, int channels)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 4);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
        writer.Write((short)3); writer.Write((short)channels); writer.Write(rate);
        writer.Write(rate * channels * 4); writer.Write((short)(channels * 4)); writer.Write((short)32);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 4);
        foreach (float sample in samples) writer.Write(sample);
    }
}
