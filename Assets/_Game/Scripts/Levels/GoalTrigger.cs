using UnityEngine;
using PawPath.Cat;
using PawPath.Core;

namespace PawPath.Levels
{
    /// <summary>
    /// Kedinin hedefe ulaşması bölümü bitirir.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalTrigger : MonoBehaviour
    {
        [SerializeField] float finishLineTolerance = 0.05f;
        [SerializeField] float lowerYFromGoal = -0.65f;
        [SerializeField] float upperYFromGoal = 4.70f;
        bool completed;

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        void OnEnable() => completed = false;

        void Update()
        {
            // Hızlı geçişlerde trigger olayı kaçsa bile aynı X/Y koşulları geçerlidir.
            TryComplete(CatController.Instance);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (CatController.Instance == null)
                return;
            if (other.transform != CatController.Instance.transform &&
                other.transform.parent != CatController.Instance.transform)
                return;
            TryComplete(CatController.Instance);
        }

        void TryComplete(CatController cat)
        {
            if (completed || cat == null || cat.IsBusy ||
                GameFlow.Instance == null || GameFlow.Instance.State != GameFlowState.Gameplay ||
                !IsWithinFinishBounds(cat.transform.position, transform.position,
                    finishLineTolerance, lowerYFromGoal, upperYFromGoal))
                return;
            completed = true;
            LevelManager.Instance?.CompleteLevel();
        }

        public static bool IsWithinFinishBounds(Vector2 cat, Vector2 goal,
            float xTolerance, float lowerY, float upperY)
        {
            return cat.x >= goal.x - xTolerance &&
                cat.y >= goal.y + lowerY && cat.y <= goal.y + upperY;
        }
    }
}
