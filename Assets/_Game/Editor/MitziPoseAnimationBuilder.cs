using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PawPath.EditorTools
{
    [InitializeOnLoad]
    internal static class MitziPoseAnimationBuilder
    {
        const string SitClipPath = "Assets/_Game/Art/Cats/mitzi_sit.anim";
        const string JumpClipPath = "Assets/_Game/Art/Cats/mitzi_jump.anim";
        const string ControllerPath = "Assets/_Game/Art/cat_walk_1.controller";
        const string SessionKey = "PawPath.MitziPoseAnimationBuilder.v2";

        static MitziPoseAnimationBuilder()
        {
            EditorApplication.delayCall += BuildOnce;
        }

        [MenuItem("Paw Path/Rebuild Mitzi Sit And Jump Animations")]
        static void BuildFromMenu() => Build(true);

        static void BuildOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;
            SessionState.SetBool(SessionKey, true);
            Build(false);
        }

        static void Build(bool logSuccess)
        {
            var sitFrames = LoadFrames("mitzi_sit");
            var jumpFrames = LoadFrames("mitzi_jump");
            if (sitFrames.Length != 4 || jumpFrames.Length != 4)
            {
                Debug.LogWarning("Mitzi poz animasyonları kurulamadı: sprite sheet'ler dört kareye ayrılamadı.");
                return;
            }

            var sitClip = CreateClip(SitClipPath, "mitzi_sit", sitFrames, 6f, false);
            var jumpClip = CreateClip(JumpClipPath, "mitzi_jump", jumpFrames, 7f, false);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogWarning("Mitzi poz animasyonları kurulamadı: Animator Controller bulunamadı.");
                return;
            }

            var stateMachine = controller.layers[0].stateMachine;
            var states = stateMachine.states.Select(child => child.state).ToArray();
            var walk = states.FirstOrDefault(state => state.motion != null && state.motion != sitClip && state.motion != jumpClip);
            if (walk != null)
            {
                walk.name = "Walk";
                stateMachine.defaultState = walk;
            }

            SetStateMotion(stateMachine, "Sit", sitClip);
            SetStateMotion(stateMachine, "Jump", jumpClip);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            if (logSuccess)
                Debug.Log("Mitzi için dört karelik oturma ve zıplama/düşme animasyonları hazırlandı.");
        }

        static Sprite[] LoadFrames(string prefix)
        {
            var frames = new Sprite[4];
            for (int index = 0; index < frames.Length; index++)
            {
                string path = $"Assets/_Game/Art/Cats/{prefix}_{index + 1}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (importer == null || texture == null)
                    continue;

                // Her poz artık ayrı dosyada. Komşu animasyon karesinden parça
                // alınması veya kedinin hücre sınırında ikiye bölünmesi mümkün değil.
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = texture.height / 11.5f;

                // Unity 2022.3 hizalama/pivot değerlerini doğrudan
                // TextureImporter üzerinden değil importer ayarlarıyla yazar.
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteAlignment = (int)SpriteAlignment.Center;
                textureSettings.spritePivot = new Vector2(0.5f, 0.5f);
                importer.SetTextureSettings(textureSettings);
                importer.SaveAndReimport();
                frames[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return frames.Where(frame => frame != null).ToArray();
        }

        static AnimationClip CreateClip(string path, string name, Sprite[] frames, float frameRate, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name };
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = frameRate;
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            var keys = frames.Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / frameRate,
                value = sprite
            }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static void SetStateMotion(AnimatorStateMachine stateMachine, string stateName, Motion motion)
        {
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == stateName);
            if (state == null)
                state = stateMachine.AddState(stateName);
            state.motion = motion;
        }
    }
}
