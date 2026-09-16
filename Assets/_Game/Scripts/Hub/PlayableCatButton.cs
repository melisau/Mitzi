using UnityEngine;
using PawPath.Core;
using PawPath.Data;

namespace PawPath.Hub
{
    /// <summary>
    /// Hub'daki bir kediye kısa dokunuş oynanacak karakteri seçer (sürükleyerek okşama ile çakışmasın diye eşik var).
    /// </summary>
    public class PlayableCatButton : MonoBehaviour
    {
        [SerializeField] CatDefinition cat;
        Vector2 downPos;
        bool down;

        public void Bind(CatDefinition definition) => cat = definition;

        void Update()
        {
            if (GameFlow.Instance != null && !GameFlow.Instance.InHub)
                return;

            if (PointerDown())
            {
                down = true;
                downPos = PointerScreen();
            }
            else if (down && PointerUp())
            {
                down = false;
                if (Vector2.Distance(downPos, PointerScreen()) > 24f)
                    return;
                if (!HitThis())
                    return;
                Select();
            }
        }

        void Select()
        {
            if (cat == null)
                return;
            SaveService.Data.selectedCatId = cat.id;
            SaveService.Persist();
            GameEvents.PlayableCatChanged(cat);
        }

        bool HitThis()
        {
            var cam = Camera.main;
            if (cam == null)
                return false;
            Vector3 s = PointerScreen();
            s.z = Mathf.Abs(cam.transform.position.z);
            var world = cam.ScreenToWorldPoint(s);
            var col = GetComponent<Collider2D>();
            return col != null && col.OverlapPoint(world);
        }

        static Vector2 PointerScreen()
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

        static bool PointerUp()
        {
            if (Input.touchCount > 0)
            {
                var p = Input.GetTouch(0).phase;
                return p == TouchPhase.Ended || p == TouchPhase.Canceled;
            }
            return Input.GetMouseButtonUp(0);
        }
    }
}
