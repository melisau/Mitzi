using UnityEngine;

namespace PawPath.Hub
{
    /// <summary>Ev eşyası taşıma ve ayna çevirme durumunu yönetir.</summary>
    public static class HomeEditMode
    {
        public static bool Active { get; private set; }
        public static DraggableFurniture Selected { get; private set; }

        public static bool Toggle()
        {
            Active = !Active;
            if (!Active)
                Selected = null;
            return Active;
        }

        public static void Select(DraggableFurniture furniture)
        {
            if (Active)
                Selected = furniture;
        }

        public static void FlipSelected()
        {
            if (Active && Selected != null)
                Selected.FlipHorizontal();
        }

        public static void Close()
        {
            Active = false;
            Selected = null;
        }
    }
}
