using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Gameplay;
using PawPath.Localization;

namespace PawPath.Hub
{
    /// <summary>Selected resident movement and an explicit nearby furniture action.</summary>
    public sealed class HomeManualControls : MonoBehaviour
    {
        MobileJoystick stick;
        Button actionButton;
        Text selectedLabel, actionLabel;
        CatHomeBehaviour controlled;
        FurnitureInteraction nearby;
        CareAction nearbyAction;

        public void Bind(MobileJoystick joystick, Button action, Text selectedName)
        {
            stick = joystick; actionButton = action; selectedLabel = selectedName;
            actionLabel = action.GetComponentInChildren<Text>();
            actionButton.onClick.AddListener(UseNearby);
            actionButton.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            bool available = GameFlow.Instance != null && GameFlow.Instance.InHub && !HomeEditMode.Active;
            var selected = available && CatHouseInteraction.Instance != null ? CatHouseInteraction.Instance.Selected : null;
            if (controlled != selected)
            {
                if (controlled != null) controlled.SetManualInput(Vector2.zero);
                controlled = selected;
            }
            bool busy = controlled != null && controlled.HasExternalAnimation;
            var furniture = FindObjectsOfType<FurnitureInteraction>();
            foreach (var item in furniture) if (item.IsBusyWith(controlled)) busy = true;
            stick.gameObject.SetActive(available && controlled != null && !busy);
            if (controlled != null) controlled.SetManualInput(available && !busy ? stick.HomeValue : Vector2.zero);
            if (controlled != null && stick.HomeValue.sqrMagnitude > 0f && HomeRooms.Instance != null)
            {
                int room = controlled.transform.position.x < HomeRooms.Instance.RoomWidth * .5f ? 0 : 1;
                if (room != HomeRooms.Instance.ActiveRoom) HomeRooms.Instance.SelectRoom(room);
            }
            selectedLabel.gameObject.SetActive(available && controlled != null);
            selectedLabel.text = controlled != null && controlled.Definition != null ? controlled.Definition.displayName : "";
            nearby = null;
            float nearest = float.PositiveInfinity;
            if (available && controlled != null && !busy)
                foreach (var item in furniture)
                    if (item.TryNearbyCare(controlled, out var action))
                    {
                        float distance = Vector2.SqrMagnitude(item.transform.position - controlled.transform.position);
                        if (distance < nearest) { nearest = distance; nearby = item; nearbyAction = action; }
                    }
            actionButton.gameObject.SetActive(nearby != null);
            if (nearby != null)
                actionLabel.text = nearbyAction == CareAction.Eat ? GameText.L("Mama ye", "Eat", "Fressen")
                    : nearbyAction == CareAction.Drink ? GameText.L("Su iç", "Drink", "Trinken")
                    : GameText.L("Uyu", "Sleep", "Schlafen");
        }

        void UseNearby()
        {
            if (controlled == null || nearby == null || !nearby.TryNearbyCare(controlled, out var action)) return;
            controlled.SetManualInput(Vector2.zero);
            nearby.StartCareAction(action);
            Refresh();
        }
        void Update() => Refresh();
        void OnDisable() { if (controlled != null) controlled.SetManualInput(Vector2.zero); }
    }
}
