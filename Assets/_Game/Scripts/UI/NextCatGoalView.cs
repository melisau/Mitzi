using PawPath.Core;
using PawPath.Data;
using PawPath.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace PawPath.UI
{
    // Only the next milestone is presented. The cat's identity stays a surprise.
    public sealed class NextCatGoalView : MonoBehaviour
    {
        GameObject card;
        Text label;
        Text symbol;
        public void Bind(GameObject root, Text text, Text icon)
        {
            card = root; label = text; symbol = icon; Refresh();
        }
        void OnEnable()
        {
            GameEvents.OnCatUnlocked += OnUnlocked;
            GameEvents.OnHubEntered += Refresh;
            GameEvents.OnLevelCompleted += Refresh;
            if (GameFlow.Instance != null) GameFlow.Instance.StateChanged += OnState;
            Refresh();
        }
        void OnDisable()
        {
            GameEvents.OnCatUnlocked -= OnUnlocked;
            GameEvents.OnHubEntered -= Refresh;
            GameEvents.OnLevelCompleted -= Refresh;
            if (GameFlow.Instance != null) GameFlow.Instance.StateChanged -= OnState;
        }
        void OnUnlocked(CatDefinition cat) => Refresh();
        void OnState(GameFlowState state) => Refresh();
        public void Refresh()
        {
            if (card == null) return;
            var flow = GameFlow.Instance;
            var next = CatUnlockService.NextLockedCat();
            card.SetActive(next != null && flow != null && flow.Catalog != null && flow.State == GameFlowState.MainMenu);
            symbol.text = "?";
            label.text = next != null ? next.unlockAfterLevel.ToString() : "";
        }
    }
}
