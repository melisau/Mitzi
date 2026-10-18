using System.Collections.Generic;
using UnityEngine;
using PawPath.Core;
using PawPath.Content;

namespace PawPath.Hub
{
    public class HubFurnitureView : MonoBehaviour
    {
        public static HubFurnitureView Instance { get; private set; }
        [SerializeField] Transform[] slots;
        [SerializeField] SpriteRenderer roomBackground;
        [SerializeField] Sprite defaultRoom;
        readonly Dictionary<string, GameObject> copies = new Dictionary<string, GameObject>();
        void Awake() => Instance = this;
        public Vector3 BowlPosition => PlacedInteraction(FurnitureSlotType.Bowl)?.transform.position ?? new Vector3(0f,-1.72f,0f);
        public Vector3 WaterPosition => PlacedInteraction(FurnitureSlotType.Water)?.transform.position ?? new Vector3(0.95f,-1.82f,0f);
        public bool HasPlacedWater => HasPlacedInSlot(FurnitureSlotType.Water);
        public bool HasPlacedBowl => HasPlacedInSlot(FurnitureSlotType.Bowl);
        public bool HasPlacedInSlot(FurnitureSlotType type) => PlacedInteraction(type) != null;

        public FurnitureInteraction PlacedInteraction(FurnitureSlotType type)
        {
            if (GameFlow.Instance == null) return null;
            foreach (var copy in SaveService.Data.furniture)
            {
                if (!copy.placed || copy.roomId != SaveService.Data.activeHomeRoom) continue;
                var item = GameFlow.Instance.Catalog.GetItem(copy.itemId);
                if (item != null && item.slotType == type && copies.TryGetValue(copy.instanceId, out var go) && go != null)
                    return go.GetComponent<FurnitureInteraction>();
            }
            return null;
        }
        public void Bind(Transform[] anchors) => slots = anchors;
        public void BindRoom(SpriteRenderer background, Sprite defaultSprite) { roomBackground=background; defaultRoom=defaultSprite; }
        void OnEnable() { GameEvents.OnShopChanged += Refresh; GameEvents.OnHubEntered += Refresh; Refresh(); }
        void OnDisable() { GameEvents.OnShopChanged -= Refresh; GameEvents.OnHubEntered -= Refresh; }

        public void Refresh()
        {
            if (GameFlow.Instance == null || GameFlow.Instance.Catalog == null || slots == null) return;
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                var old = slot.GetComponent<SpriteRenderer>(); if (old != null) old.sprite=null;
                slot.GetComponent<DraggableFurniture>()?.DisableInteraction();
                var interaction=slot.GetComponent<FurnitureInteraction>(); if(interaction!=null) interaction.enabled=false;
                var oldCollider=slot.GetComponent<Collider2D>(); if(oldCollider!=null) oldCollider.enabled=false;
            }
            for(int room=0;room<2;room++)
            {
                int style = HomeRooms.StyleForRoom(room);
                var options = GameFlow.Instance.Catalog.homeRoomOptions;
                ApplyRoom(style > 0 && options != null && style <= options.Length ? options[style - 1] : defaultRoom, room);
            }
            var alive=new HashSet<string>();
            var positioned = new List<DraggableFurniture>();
            var pending = new List<DraggableFurniture>();
            foreach (var copy in SaveService.Data.furniture)
            {
                if (!copy.placed) continue;
                var item=GameFlow.Instance.Catalog.GetItem(copy.itemId); if(item==null) continue;
                if(item.slotType==FurnitureSlotType.Wallpaper) { ApplyRoom(item.placedSprite, copy.roomId); continue; }
                alive.Add(copy.instanceId);
                if(!copies.TryGetValue(copy.instanceId,out var go) || go==null)
                {
                    go=new GameObject("Furniture_"+copy.instanceId); go.transform.SetParent(transform,false); copies[copy.instanceId]=go;
                }
                var anchor=(int)item.slotType<slots.Length ? slots[(int)item.slotType] : null;
                Vector3 position=anchor!=null ? anchor.position : new Vector3(0f,-2f,0f);
                position.x += HomeRooms.Instance!=null ? HomeRooms.Instance.Offset(copy.roomId) : copy.roomId*16f;
                go.SetActive(true);
                go.transform.position=position;
                var sr=go.GetComponent<SpriteRenderer>();
                if(sr==null) sr=go.AddComponent<SpriteRenderer>();
                sr.sprite=item.placedSprite!=null ? item.placedSprite : FallbackSprite.WhiteCircle(); sr.color=Color.white;
                sr.sortingOrder=item.slotType==FurnitureSlotType.Poster ? 3 : item.slotType==FurnitureSlotType.Rug ? 2 : 4;
                FitItem(sr,item.slotType);
                var drag=go.GetComponent<DraggableFurniture>();
                if(drag==null) drag=go.AddComponent<DraggableFurniture>();
                drag.Configure(item.slotType,sr,copy);
                bool fresh = drag.NeedsInitialPlacement;
                bool clamped = drag.ClampPlacement();
                // Saved arrangements reserve their space before any new purchase.
                // Old wall-level placements are rehomed using the same layout search.
                if (fresh || clamped) pending.Add(drag); else positioned.Add(drag);
                var interaction=go.GetComponent<FurnitureInteraction>();
                if(interaction==null) interaction=go.AddComponent<FurnitureInteraction>();
                interaction.Configure(item);
            }
            var removed=new List<string>();
            foreach(var pair in copies) if(!alive.Contains(pair.Key)) { if(pair.Value!=null) { pair.Value.SetActive(false); Destroy(pair.Value); } removed.Add(pair.Key); }
            foreach(string id in removed) copies.Remove(id);
            foreach (var drag in pending)
            {
                if (drag.TryPlaceApart(positioned)) positioned.Add(drag);
                else drag.StoreWhenRoomFull();
            }
        }

        void ApplyRoom(Sprite sprite, int room)
        {
            var renderer=HomeRooms.Instance!=null ? HomeRooms.Instance.Background(room) : room==0 ? roomBackground : null;
            if(renderer==null || sprite==null) return;
            renderer.sprite=HomeRooms.Instance!=null ? HomeRooms.Instance.RoomSprite(sprite) : sprite;
            var camera=Camera.main; var size=renderer.sprite.bounds.size;
            if(camera==null || size.x<=0f || size.y<=0f) return;
            float height=camera.orthographicSize*2f;
            float scale=HomeRooms.Instance!=null ? height/size.y : Mathf.Max(height*camera.aspect/size.x,height/size.y)*1.12f;
            renderer.transform.localScale=new Vector3(scale,scale,1f);
            renderer.transform.position=new Vector3(renderer.transform.position.x,camera.transform.position.y,renderer.transform.position.z);
        }

        static void FitItem(SpriteRenderer renderer, PawPath.Content.FurnitureSlotType type)
        {
            if (renderer.sprite == null || renderer.sprite.bounds.size.y <= 0f)
                return;
            // Görseller kırpılmış ve zemin gölgeli; ölçüler evdeki kediye (~2.2 birim) göre ayarlandı.
            float targetHeight = type switch
            {
                PawPath.Content.FurnitureSlotType.Rug => 1.35f,
                PawPath.Content.FurnitureSlotType.Bed => 1.55f,
                PawPath.Content.FurnitureSlotType.Bowl => 0.80f,
                PawPath.Content.FurnitureSlotType.Water => 0.72f,
                PawPath.Content.FurnitureSlotType.Sand => 2.85f,
                PawPath.Content.FurnitureSlotType.Poster => 2.05f,
                PawPath.Content.FurnitureSlotType.Tree => 4.40f,
                PawPath.Content.FurnitureSlotType.Toy => 0.95f,
                _ => 2f
            };
            float scale = targetHeight / renderer.sprite.bounds.size.y;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
