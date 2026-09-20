using UnityEngine;
using UnityEngine.EventSystems;
using PawPath.Core;

namespace PawPath.Hub
{
    /// <summary>Evde seçilen kediyi dokunulan zemin noktasına yürütür.</summary>
    public class CatHouseInteraction : MonoBehaviour
    {
        public static CatHouseInteraction Instance { get; private set; }

        CatHomeBehaviour selected;
        int selectedFrame = -1;

        void Awake() => Instance = this;

        public void Select(CatHomeBehaviour cat)
        {
            selected = cat;
            selectedFrame = Time.frameCount;
        }

        public void SendSelectedToBowl()
        {
            EnsureSelectedResident();
            if (selected == null)
                return;
            Vector3 bowl = HubFurnitureView.Instance != null
                ? HubFurnitureView.Instance.BowlPosition
                : new Vector3(-3.4f, -1.65f, 0f);
            selected.WalkTo(ApproachFromSide(bowl), ResidentActivity.Eating);
        }

        public void SendSelectedToWater()
        {
            EnsureSelectedResident();
            if (selected == null || HubFurnitureView.Instance == null || !HubFurnitureView.Instance.HasPlacedWater)
                return;
            selected.WalkTo(ApproachFromSide(HubFurnitureView.Instance.WaterPosition), ResidentActivity.Drinking);
        }

        Vector2 ApproachFromSide(Vector3 itemPosition)
        {
            float direction = selected.transform.position.x < itemPosition.x ? -1f : 1f;
            return new Vector2(itemPosition.x + direction * 0.72f, itemPosition.y);
        }

        void Update()
        {
            if (GameFlow.Instance == null || !GameFlow.Instance.InHub || !PointerUp())
                return;
            if (Time.frameCount == selectedFrame || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 world = PointerWorld();
            var hit = Physics2D.OverlapPoint(world);
            if (hit != null && (hit.GetComponent<CatHomeBehaviour>() != null || hit.GetComponent<DraggableFurniture>() != null))
                return;

            if (selected != null)
                selected.WalkTo(world);
        }

        void EnsureSelectedResident()
        {
            if (selected != null)
                return;
            string selectedId = SaveService.Data != null ? SaveService.Data.selectedCatId : "";
            foreach (var button in FindObjectsOfType<PlayableCatButton>())
            {
                if (button.Cat != null && button.Cat.id == selectedId)
                {
                    selected = button.GetComponent<CatHomeBehaviour>();
                    return;
                }
            }
        }

        static Vector2 PointerWorld()
        {
            Vector3 screen = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
            var cam = Camera.main;
            screen.z = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(screen);
        }

        static bool PointerUp()
        {
            if (Input.touchCount > 0)
            {
                var phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
            }
            return Input.GetMouseButtonUp(0);
        }
    }
}
