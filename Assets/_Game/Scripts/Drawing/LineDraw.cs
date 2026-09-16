using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using PawPath.Audio;
using PawPath.Core;

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
        [SerializeField] Color lineColor = new Color(0.93f, 0.78f, 0.86f, 1f);
        [SerializeField] Material lineMaterial;
        [SerializeField] PhysicsMaterial2D pathPhysics;
        [SerializeField] int lineSortingOrder = 4;

        [Header("Mürekkep")]
        [SerializeField] float defaultInk = 18f;

        Camera cam;
        readonly List<GameObject> strokes = new List<GameObject>();
        readonly List<Vector2> currentPoints = new List<Vector2>();
        LineRenderer currentLine;
        EdgeCollider2D currentCollider;
        bool drawing;
        float inkLeft;

        public float InkLeft => inkLeft;
        public float InkMax { get; private set; } = 18f;
        public bool CanDraw { get; set; } = true;

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
            CanDraw = true;
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
            if (!CanDraw || GameFlow.Instance != null && GameFlow.Instance.InHub)
                return;
            if (IsPointerOverUi())
                return;

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

            currentLine.positionCount = currentPoints.Count;
            for (int i = 0; i < currentPoints.Count; i++)
                currentLine.SetPosition(i, currentPoints[i]);

            if (currentPoints.Count >= 2)
                currentCollider.points = currentPoints.ToArray();
        }

        void EndStroke()
        {
            drawing = false;
            currentLine = null;
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
    }
}
