using System;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Gameplay;
using PawPath.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run in the isolated MitziValidation project; never changes the player's save.
public static class PassageJumpValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(CatController cat, string method, params object[] args) =>
        typeof(CatController).GetMethod(method, Hidden).Invoke(cat, args);
    static void Set(CatController cat, string field, object value) =>
        typeof(CatController).GetField(field, Hidden).SetValue(cat, value);
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use an isolated validation project.");
        var previousMode = Physics2D.simulationMode;
        bool previousLarge = DisplaySizeSettings.Large;
        var previousPlayMode = GameplayMode.Current;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Physics2D.simulationMode = SimulationMode2D.Script;
            var floor = new GameObject("JumpTestFloor");
            floor.transform.position = new Vector3(1000, -.5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(40, 1);
            var cat = new GameObject("JumpTestCat").AddComponent<CatController>();
            Call(cat, "Awake");
            var body = cat.GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.None;
            var course = new GameObject("JumpTestCourse").AddComponent<LevelCourseBuilder>();
            int trials = 0;
            foreach (bool large in new[] { false, true })
            {
                DisplaySizeSettings.SetLarge(large);
                foreach (string theme in new[] { "street", "forest", "city", "cyber" })
                {
                    string art = "Assets/_Game/Art/CrouchObstacles/" + theme;
                    course.BindCrouchObstacle(AssetDatabase.LoadAssetAtPath<Sprite>(art + "_crouch_grounded.png"),
                        AssetDatabase.LoadAssetAtPath<Sprite>(art + "_crouch_upper.png"));
                    foreach (int level in new[] { 1, 2, 6, 7, 25, 100 })
                    {
                        course.Build(level);
                        Physics2D.SyncTransforms();
                        var template = course.GetComponentInChildren<CrouchPassage>();
                        Check(template != null, "Missing passage: " + theme + "/" + level);
                        var passage = UnityEngine.Object.Instantiate(template);
                        passage.transform.position = new Vector3(1000, 0);
                        passage.Configure(template.Width, 0, template.Clearance);
                        foreach (int direction in new[] { 1, -1 })
                        foreach (float offset in new[] { -2.9f, -2.15f, -1.8f, 0f, 1.8f, 2.15f, 2.9f })
                        foreach (bool keyboard in new[] { false, true })
                        {
                            GameplayMode.Select(GameplayPlayMode.Drawing);
                            // Establish the crouched pose before placing beneath the ceiling.
                            cat.PlaceAtSpawn(new Vector2(990, .35f));
                            Call(cat, "SetCrouching", true, true);
                            cat.transform.position = new Vector3(1000 + offset * direction, .35f);
                            Set(cat, "facing", direction);
                            Physics2D.SyncTransforms();
                            string context = $"{theme} level={level} large={large} dir={direction} offset={offset} keyboard={keyboard}";
                            for (int frame = 0; frame < 800; frame++)
                            {
                                Vector2 before = body.position;
                                bool inPassage = CrouchPassage.NeedsCrouch(cat, .45f);
                                int jumps = (int)typeof(CatController).GetField("jumpsRemaining", Hidden).GetValue(cat);
                                // Repeated jump attempts also check that blocked input is consumed.
                                if (frame % 3 == 0)
                                {
                                    if (keyboard) Set(cat, "keyboardJumpQueued", true);
                                    else MobileControlState.Set(MobileAction.Jump, true);
                                }
                                Call(cat, "FixedUpdate");
                                if (inPassage)
                                {
                                    Check(body.velocity.y < .2f, "Jump launched under ceiling: " + context);
                                    Check(cat.IsCrouching, "Standing collider enabled under ceiling: " + context);
                                    Check((int)typeof(CatController).GetField("jumpsRemaining", Hidden).GetValue(cat) >= jumps,
                                        "Blocked jump consumed allowance: " + context);
                                }
                                Check(!MobileControlState.JumpQueued, "Blocked input buffered: " + context);
                                Physics2D.Simulate(.02f);
                                Check(Mathf.Abs(body.position.x - before.x) < .15f, "Horizontal teleport: " + context);
                                Check(!cat.IsBusy, "Passage forced restart: " + context);
                                if (direction * (body.position.x - 1000) > 3.7f) break;
                                if (frame == 799)
                                {
                                    foreach (var contact in GetContacts(body))
                                        Debug.Log($"Passage contact: {contact.collider.name}/{contact.otherCollider.name} normal={contact.normal}");
                                    throw new InvalidOperationException("Cat stuck: " + context + " position=" + body.position +
                                        " velocity=" + body.velocity + " crouching=" + cat.IsCrouching +
                                        " recovery=" + typeof(CatController).GetField("recoveringPassage", Hidden).GetValue(cat));
                                }
                            }
                            trials++;
                        }
                        // Losing ground contact must not expand the collider inside the beam.
                        cat.PlaceAtSpawn(new Vector2(990, .35f));
                        Call(cat, "SetCrouching", true, true);
                        cat.transform.position = new Vector3(1000, .51f);
                        Physics2D.SyncTransforms();
                        Call(cat, "FixedUpdate");
                        Check(cat.IsCrouching, "Airborne crouch lost: " + theme);
                        UnityEngine.Object.DestroyImmediate(passage.gameObject);
                    }
                }
            }
            // Both modes retain ordinary first and second jumps away from a passage.
            foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
            {
                GameplayMode.Select(mode);
                cat.PlaceAtSpawn(new Vector2(1015, .35f));
                Physics2D.SyncTransforms();
                MobileControlState.Set(MobileAction.Jump, true);
                Call(cat, "FixedUpdate");
                Check(body.velocity.y > 7f, "First jump lost: " + mode);
                Physics2D.Simulate(.02f);
                MobileControlState.Set(MobileAction.Jump, true);
                Call(cat, "TryJump");
                Check(body.velocity.y > 8f, "Second jump lost: " + mode);
            }
            Debug.Log("PASSAGE_JUMP_VALIDATION_PASS trials=" + trials);
        }
        finally
        {
            Physics2D.simulationMode = previousMode;
            DisplaySizeSettings.SetLarge(previousLarge);
            GameplayMode.Select(previousPlayMode);
            MobileControlState.Reset();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    static ContactPoint2D[] GetContacts(Rigidbody2D body)
    {
        var contacts = new ContactPoint2D[16];
        body.GetContacts(contacts);
        return Array.FindAll(contacts, contact => contact.collider != null);
    }
}
