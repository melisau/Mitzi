using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using PawPath.Audio;
using PawPath.Core;
using PawPath.Gameplay;

namespace PawPath.Drawing
{
    /// <summary>
    /// Parmak sürükleyince LineRenderer + EdgeCollider2D ile yumuşak yol üretir.
    /// Editörde fare, telefonda dokunma aynı kod yolunu kullanır.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class LineDraw : MonoBehaviour
    {
        public static LineDraw Instance { get; private set; }

        [Header("Fırça")]
        [SerializeField] float minPointDistance = 0.07f;
        [SerializeField] float lineWidth = 0.22f;
        [SerializeField] Color lineColor = new Color(0.17f, 0.15f, 0.14f, 1f);
        [SerializeField] Material lineMaterial;
        [SerializeField] PhysicsMaterial2D pathPhysics;
        [SerializeField] int lineSortingOrder = 4;

        [Header("Mürekkep")]
        [SerializeField] float defaultInk = 18f;

        Camera cam;
        readonly List<GameObject> strokes = new List<GameObject>();
        readonly List<Vector2> currentPoints = new List<Vector2>();
        LineRenderer currentLine;
        LineRenderer currentShadow;
        LineRenderer currentHighlight;
        EdgeCollider2D currentCollider;
        bool drawing;
        float inkLeft;
        PathSurfaceType brushType = PathSurfaceType.Normal;
        bool eraserMode;

        public float InkLeft => inkLeft;
        public float InkMax { get; private set; } = 18f;
        public bool CanDraw { get; set; } = true;
        public bool HasDrawnPath { get; private set; }

        void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            inkLeft = defaultInk;
            InkMax = defaultInk;
        }

        public void ResetInk(float budget)
        {
            ClearStrokes();
            InkMax = budget;
            inkLeft = budget;
            CanDraw = GameplayMode.IsDrawing;
            HasDrawnPath = false;
        }

        public void ClearStrokes()
        {
            foreach (var stroke in strokes)
            {
                if (stroke)
                    Destroy(stroke);
            }
            strokes.Clear();
            EndStroke();
        }

        void Update()
        {
            if (!GameplayMode.IsDrawing || !CanDraw || GameFlow.Instance != null && GameFlow.Instance.InHub)
                return;
            if (IsPointerOverUi())
                return;

            if (eraserMode)
            {
                if (PointerHeld() || PointerDown())
                    EraseAt(PointerWorld());
                return;
            }

            if (PointerDown())
                BeginStroke(PointerWorld());
            else if (drawing && PointerHeld())
                AppendPoint(PointerWorld());
            else if (drawing && PointerUp())
                EndStroke();
        }

        void BeginStroke(Vector2 world)
        {
            if (inkLeft <= 0.01f)
                return;

            drawing = true;
            currentPoints.Clear();
            currentPoints.Add(world);

            var go = new GameObject("PathStroke");
            go.layer = LayerMask.NameToLayer("Default");
            strokes.Add(go);
            go.AddComponent<PathSurface>().Configure(brushType);

            currentLine = go.AddComponent<LineRenderer>();
            currentLine.positionCount = 1;
            currentLine.SetPosition(0, world);
            currentLine.startWidth = lineWidth;
            currentLine.endWidth = lineWidth;
            currentLine.numCapVertices = 8;
            currentLine.numCornerVertices = 6;
            currentLine.textureMode = LineTextureMode.Tile;
            currentLine.alignment = LineAlignment.TransformZ;
            currentLine.sortingOrder = lineSortingOrder;
            currentLine.useWorldSpace = true;
            if (lineMaterial != null)
            {
                currentLine.material = lineMaterial;
            }
            else
            {
                var shader = Shader.Find("Sprites/Default");
                currentLine.material = new Material(shader);
            }
            currentLine.startColor = lineColor;
            currentLine.endColor = lineColor;
            ApplyTaper(currentLine, lineWidth);

            currentShadow = CreateVisualLayer(go.transform, "StrokeShadow", lineWidth * 1.34f,
                new Color(0.05f, 0.04f, 0.04f, 0.38f), lineSortingOrder - 1);
            currentHighlight = CreateVisualLayer(go.transform, "StrokeHighlight", lineWidth * 0.30f,
                new Color(1f, 1f, 1f, 0.30f), lineSortingOrder + 1);

            currentCollider = go.AddComponent<EdgeCollider2D>();
            currentCollider.edgeRadius = lineWidth * 0.45f;
            if (pathPhysics != null)
                currentCollider.sharedMaterial = pathPhysics;

            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlayBrush();
        }

        void AppendPoint(Vector2 world)
        {
            if (currentPoints.Count == 0)
                return;

            Vector2 last = currentPoints[currentPoints.Count - 1];
            float dist = Vector2.Distance(last, world);
            if (dist < minPointDistance)
                return;

            if (inkLeft <= 0f)
            {
                EndStroke();
                return;
            }

            float spend = Mathf.Min(dist, inkLeft);
            Vector2 next = last + (world - last).normalized * spend;
            inkLeft -= spend;
            currentPoints.Add(next);
            HasDrawnPath = true;

            var smooth = SmoothPoints(currentPoints);
            SetLinePoints(currentShadow, smooth);
            SetLinePoints(currentLine, smooth);
            SetLinePoints(currentHighlight, smooth);

            if (currentPoints.Count >= 2)
                currentCollider.points = smooth.ToArray();
        }

        void EndStroke()
        {
            drawing = false;
            currentLine = null;
            currentShadow = null;
            currentHighlight = null;
            currentCollider = null;
            currentPoints.Clear();
        }

        Vector2 PointerWorld()
        {
            Vector3 screen = PointerScreen();
            screen.z = Mathf.Abs(cam.transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(screen);
            world.z = 0f;
            return world;
        }

        static Vector3 PointerScreen()
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
        }

        static bool PointerDown()
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).phase == TouchPhase.Began;
            return Input.GetMouseButtonDown(0);
        }

        static bool PointerHeld()
        {
            if (Input.touchCount > 0)
            {
                var p = Input.GetTouch(0).phase;
                return p == TouchPhase.Moved || p == TouchPhase.Stationary;
            }
            return Input.GetMouseButton(0);
        }

        static bool PointerUp()
        {
            if (Input.touchCount > 0)
            {
                var p = Input.GetTouch(0).phase;
                return p == TouchPhase.Ended || p == TouchPhase.Canceled;
            }
            return Input.GetMouseButtonUp(0);
        }

        static bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
                return false;
            if (Input.touchCount > 0)
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }

        public void SetBrush(Material material, Color color, PhysicsMaterial2D physics)
        {
            lineMaterial = material;
            lineColor = color;
            pathPhysics = physics;
        }

        LineRenderer CreateVisualLayer(Transform parent, string name, float width, Color color, int sorting)
        {
            var layer = new GameObject(name).AddComponent<LineRenderer>();
            layer.transform.SetParent(parent, false);
            layer.useWorldSpace = true;
            layer.positionCount = 1;
            layer.SetPosition(0, currentPoints[0]);
            layer.numCapVertices = 8;
            layer.numCornerVertices = 8;
            layer.textureMode = LineTextureMode.Tile;
            layer.alignment = LineAlignment.TransformZ;
            layer.sortingOrder = sorting;
            layer.material = new Material(Shader.Find("Sprites/Default"));
            layer.startColor = layer.endColor = color;
            ApplyTaper(layer, width);
            return layer;
        }

        static void ApplyTaper(LineRenderer line, float width)
        {
            line.widthMultiplier = width;
            line.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.58f), new Keyframe(0.08f, 1f),
                new Keyframe(0.92f, 1f), new Keyframe(1f, 0.58f));
        }

        static void SetLinePoints(LineRenderer line, List<Vector2> points)
        {
            if (line == null)
                return;
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
                line.SetPosition(i, points[i]);
        }

        static List<Vector2> SmoothPoints(List<Vector2> source)
        {
            if (source.Count < 3)
                return new List<Vector2>(source);

            var result = new List<Vector2>(source.Count * 2);
            result.Add(source[0]);
            for (int i = 0; i < source.Count - 1; i++)
            {
                Vector2 a = source[i];
                Vector2 b = source[i + 1];
                result.Add(Vector2.Lerp(a, b, 0.25f));
                result.Add(Vector2.Lerp(a, b, 0.75f));
            }
            result.Add(source[source.Count - 1]);
            return result;
        }

        public void SetBrushType(PathSurfaceType type)
        {
            eraserMode = false;
            brushType = type;
            lineColor = type switch
            {
                PathSurfaceType.Bounce => new Color(0.20f, 0.55f, 0.95f, 1f),
                PathSurfaceType.Hazard => new Color(0.88f, 0.20f, 0.20f, 1f),
                PathSurfaceType.Ice => new Color(0.95f, 0.98f, 1f, 1f),
                _ => new Color(0.17f, 0.15f, 0.14f, 1f)
            };
        }

        public void SetEraser()
        {
            EndStroke();
            eraserMode = true;
        }

        void EraseAt(Vector2 world)
        {
            var hits = Physics2D.OverlapCircleAll(world, 0.32f);
            foreach (var hit in hits)
            {
                if (hit == null || hit.GetComponent<PathSurface>() == null)
                    continue;
                strokes.Remove(hit.gameObject);
                Destroy(hit.gameObject);
            }
        }
    }
}
