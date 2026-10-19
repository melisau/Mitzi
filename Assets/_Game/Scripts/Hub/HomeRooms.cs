using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using PawPath.Core;
using PawPath.UI;

namespace PawPath.Hub
{
    /// <summary>Two adjacent rooms share a continuous floor and resident walking area.</summary>
    [DefaultExecutionOrder(-100)]
    public class HomeRooms : MonoBehaviour
    {
        public static HomeRooms Instance { get; private set; }
        public int ActiveRoom => SaveService.Data.activeHomeRoom;
        public float RoomWidth { get; private set; } = 16f;
        // Furniture bases use actual sprite bounds; cat roots include paw-canvas padding.
        public const float FurnitureBackY = -1.95f;
        public const float FloorMinY = -3.95f;
        public const float FloorMaxY = -2.75f;
        SpriteRenderer[] backgrounds;
        Camera camera;
        GameObject controls;
        Image[] roomIcons;
        Button previous, next;
        Button store, move;
        float velocity;
        bool swipeTracking, swipeConsumed;
        int swipeFinger, swipeReleaseFrame = -1;
        Vector2 swipeStart;
        float swipeStartedAt;
        readonly List<RaycastResult> swipeUiHits = new List<RaycastResult>();
        public static bool SuppressWorldTap => Instance != null &&
            (Instance.swipeConsumed || Instance.swipeReleaseFrame == Time.frameCount);
        readonly Dictionary<Sprite, Sprite> roomSprites = new Dictionary<Sprite, Sprite>();

        public Sprite RoomSprite(Sprite source)
        {
            if (source == null || camera == null) return source;
            if (roomSprites.TryGetValue(source, out var cropped)) return cropped;
            Rect rect = source.rect;
            float aspect = RoomWidth / (camera.orthographicSize * 2f);
            if (rect.width / rect.height > aspect)
            {
                float width = rect.height * aspect; rect.x += (rect.width - width) * 0.5f; rect.width = width;
            }
            else
            {
                float height = rect.width / aspect; rect.y += (rect.height - height) * 0.5f; rect.height = height;
            }
            cropped = Sprite.Create(source.texture, rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            roomSprites[source] = cropped; return cropped;
        }

        void Awake() => Instance = this;
        public float Offset(int room) => Mathf.Clamp(room, 0, 1) * RoomWidth;
        public Vector2 RoomBounds(int room) => new Vector2(Offset(room) - RoomWidth * 0.5f, Offset(room) + RoomWidth * 0.5f);
        public SpriteRenderer Background(int room) => backgrounds != null ? backgrounds[Mathf.Clamp(room, 0, 1)] : null;

        public static int StyleForRoom(int room)
        {
            var data = SaveService.Data;
            int style = room == 0 ? data.homeRoomStyle0 : data.homeRoomStyle1;
            bool chosen = room == 0 ? data.homeRoomStyle0Chosen : data.homeRoomStyle1Chosen;
            return !chosen && style == 0 ? 3 : style;
        }
        public static void SelectStyle(int style)
        {
            if (GameFlow.Instance == null || !GameFlow.Instance.InHub) return;
            int count = GameFlow.Instance.Catalog.homeRoomOptions?.Length ?? 0;
            style = Mathf.Clamp(style, 0, count);
            if (SaveService.Data.activeHomeRoom == 0) { SaveService.Data.homeRoomStyle0 = style; SaveService.Data.homeRoomStyle0Chosen = true; }
            else { SaveService.Data.homeRoomStyle1 = style; SaveService.Data.homeRoomStyle1Chosen = true; }
            SaveService.Persist();
            GameEvents.ShopChanged();
        }

        public void Bind(Camera view, SpriteRenderer first)
        {
            camera = view;
            RoomWidth = view.orthographicSize * 2f * view.aspect;
            var second = Instantiate(first.gameObject, first.transform.parent).GetComponent<SpriteRenderer>();
            second.name = "HubRoom_2";
            second.transform.position = first.transform.position + Vector3.right * RoomWidth;
            backgrounds = new[] { first, second };
        }

        public void BuildControls(Transform hud)
        {
            controls = new GameObject("HomeRoomNavigation", typeof(RectTransform));
            controls.transform.SetParent(hud, false);
            controls.transform.SetAsFirstSibling();
            var rect = controls.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            previous = MakeButton(controls.transform, "PreviousRoom", CozyIconFactory.ArrowLeft(), new Vector2(0f, 0.45f), new Vector2(80f, 0f), () => SelectRoom(0));
            next = MakeButton(controls.transform, "NextRoom", CozyIconFactory.ArrowRight(), new Vector2(1f, 0.45f), new Vector2(-80f, 0f), () => SelectRoom(1));
            store = MakeButton(controls.transform, "StoreFurniture", CozyIconFactory.Unpack(), new Vector2(0.5f, 0f), new Vector2(-70f, 160f), () =>
            {
                var selected = HomeEditMode.Selected;
                if (selected != null) PawPath.Economy.CozyEconomyManager.Instance?.Store(selected.InstanceId);
                HomeEditMode.Select(null);
            });
            move = MakeButton(controls.transform, "MoveFurnitureRoom", CozyIconFactory.House(), new Vector2(0.5f, 0f), new Vector2(70f, 160f), () =>
            {
                var selected = HomeEditMode.Selected;
                if (selected != null) PawPath.Economy.CozyEconomyManager.Instance?.MoveToRoom(selected.InstanceId, 1 - ActiveRoom);
                HomeEditMode.Select(null);
            });
            var badge = new GameObject("RoomBadge", typeof(RectTransform));
            badge.transform.SetParent(controls.transform, false);
            var badgeRect=badge.GetComponent<RectTransform>(); badgeRect.anchorMin=badgeRect.anchorMax=new Vector2(0.5f,1f);
            badgeRect.anchoredPosition=new Vector2(0f,-112f); badgeRect.sizeDelta=new Vector2(100f,36f);
            roomIcons = new Image[2];
            for (int i = 0; i < roomIcons.Length; i++)
            {
                var icon = new GameObject("Room_" + i, typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(badge.transform, false);
                var rt = icon.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(i == 0 ? -14f : 14f, 0f);
                rt.sizeDelta = new Vector2(20f, 20f);
                roomIcons[i] = icon.GetComponent<Image>();
                roomIcons[i].sprite = CozyIconFactory.Dot();
                roomIcons[i].raycastTarget = false;
                var shadow = icon.AddComponent<Shadow>();
                shadow.effectDistance = new Vector2(0f, -2f);
                shadow.effectColor = new Color(0.20f, 0.13f, 0.09f, 0.5f);
            }
            UpdateControls();
        }

        public void SelectRoom(int room)
        {
            if (GameFlow.Instance == null || !GameFlow.Instance.InHub) return;
            HomeEditMode.Close();
            SaveService.Data.activeHomeRoom = Mathf.Clamp(room, 0, 1);
            SaveService.Persist();
            UpdateControls();
            GameEvents.ShopChanged();
        }

        void OnEnable() { if (controls != null) controls.SetActive(true); }
        void OnDisable() { EndSwipe(); if (controls != null) controls.SetActive(false); }
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach(var sprite in roomSprites.Values) ReleaseSprite(sprite);
        }
        void LateUpdate()
        {
            if (camera == null || GameFlow.Instance == null || !GameFlow.Instance.InHub) return;
            float width = camera.orthographicSize * 2f * camera.aspect;
            if(Mathf.Abs(width - RoomWidth) > 0.01f)
            {
                float oldWidth=RoomWidth; RoomWidth=width;
                foreach(var sprite in roomSprites.Values) ReleaseSprite(sprite);
                roomSprites.Clear();
                backgrounds[1].transform.position = backgrounds[0].transform.position + Vector3.right * RoomWidth;
                foreach(var cat in GetComponentsInChildren<CatHomeBehaviour>()) cat.ReflowHome(oldWidth);
                GetComponent<HubFurnitureView>()?.Refresh();
            }
            var p = camera.transform.position;
            p.x = Mathf.SmoothDamp(p.x, Offset(ActiveRoom), ref velocity, 0.25f);
            camera.transform.position = p;
            UpdateControls();
        }

        void Update()
        {
            if (GameFlow.Instance == null || !GameFlow.Instance.InHub || HomeEditMode.Active ||
                controls == null || !controls.activeInHierarchy)
            { EndSwipe(); return; }
            if (Input.touchCount > 1) { EndSwipe(); return; }
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) BeginSwipe(touch.position, touch.fingerId);
                else if (swipeTracking && touch.fingerId == swipeFinger)
                {
                    if (touch.phase != TouchPhase.Canceled) TrackSwipe(touch.position);
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) EndSwipe();
                }
                return;
            }
            if (Input.GetMouseButtonDown(0)) BeginSwipe(Input.mousePosition, -1);
            if (swipeTracking && swipeFinger == -1)
            {
                TrackSwipe(Input.mousePosition);
                if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0)) EndSwipe();
            }
            else if (swipeTracking) EndSwipe();
        }

        bool BeginSwipe(Vector2 screen, int finger)
        {
            EndSwipe();
            if (camera == null || GameFlow.Instance == null || !GameFlow.Instance.InHub ||
                HomeEditMode.Active || controls == null || !controls.activeInHierarchy) return false;
            if (EventSystem.current != null)
            {
                swipeUiHits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, swipeUiHits);
                if (swipeUiHits.Count > 0) return false;
            }
            Vector3 point = screen; point.z = Mathf.Abs(camera.transform.position.z);
            foreach (var hit in Physics2D.OverlapPointAll(camera.ScreenToWorldPoint(point)))
                if (hit.GetComponentInParent<CatHomeBehaviour>() != null ||
                    hit.GetComponentInParent<CatPettingSystem>() != null ||
                    hit.GetComponentInParent<DraggableFurniture>() != null) return false;
            swipeTracking = true; swipeConsumed = false; swipeFinger = finger;
            swipeStart = screen; swipeStartedAt = Time.unscaledTime;
            return true;
        }

        void TrackSwipe(Vector2 screen)
        {
            if (!swipeTracking || swipeConsumed) return;
            Vector2 delta = screen - swipeStart;
            float threshold = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * .12f, 60f, 160f);
            if (Time.unscaledTime - swipeStartedAt > 2.5f ||
                (Mathf.Abs(delta.y) > threshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x)))
            { EndSwipe(); return; }
            if (Mathf.Abs(delta.x) < threshold || Mathf.Abs(delta.x) < Mathf.Abs(delta.y) * 1.5f) return;
            swipeConsumed = true;
            int room = Mathf.Clamp(ActiveRoom + (delta.x < 0 ? 1 : -1), 0, 1);
            if (room != ActiveRoom) SelectRoom(room);
        }

        void EndSwipe()
        {
            if (swipeConsumed) swipeReleaseFrame = Time.frameCount;
            swipeTracking = false; swipeConsumed = false;
        }
        void UpdateControls()
        {
            if (controls == null) return;
            previous.gameObject.SetActive(ActiveRoom == 1);
            next.gameObject.SetActive(ActiveRoom == 0);
            for (int i = 0; i < roomIcons.Length; i++)
            {
                roomIcons[i].sprite = i == ActiveRoom ? CozyIconFactory.Dot() : CozyIconFactory.Ring();
                roomIcons[i].color = new Color(1f, 0.95f, 0.82f, i == ActiveRoom ? 1f : 0.65f);
            }
            bool selected = HomeEditMode.Active && HomeEditMode.Selected != null;
            store.gameObject.SetActive(selected); move.gameObject.SetActive(selected);
        }
        static void ReleaseSprite(Sprite sprite)
        {
            if(sprite==null) return;
            if(Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite);
        }
        static Button MakeButton(Transform parent, string name, Sprite icon, Vector2 anchor, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = position; rt.sizeDelta = new Vector2(96f, 96f);
            var background = go.GetComponent<Image>();
            background.sprite = CozyIconFactory.CircleButton();
            background.color = new Color(0.66f, 0.49f, 0.37f);
            var shadow = go.AddComponent<Shadow>(); shadow.effectDistance = new Vector2(0f, -6f);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            var symbol = new GameObject("Icon", typeof(RectTransform), typeof(Image)); symbol.transform.SetParent(go.transform, false);
            var graphic = symbol.GetComponent<Image>(); graphic.sprite = icon; graphic.raycastTarget = false;
            graphic.rectTransform.sizeDelta = new Vector2(52f, 52f);
            go.AddComponent<UiButtonSound>();
            var button = go.GetComponent<Button>(); button.onClick.AddListener(action); return button;
        }
    }
}
