using UnityEngine;
using PawPath.Cat;

namespace PawPath.Levels
{
    /// <summary>
    /// Kedinin hedefe ulaşması bölümü bitirir.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalTrigger : MonoBehaviour
    {
        [SerializeField] float finishLineTolerance = 0.05f;
        bool completed;

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        void OnEnable() => completed = false;

        void Update()
        {
            if (!completed && CatController.Instance != null &&
                CatController.Instance.transform.position.x >= transform.position.x - finishLineTolerance)
                Complete();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (CatController.Instance == null)
                return;
            if (other.transform != CatController.Instance.transform &&
                other.transform.parent != CatController.Instance.transform)
                return;
            Complete();
        }

        void Complete()
        {
            if (completed || CatController.Instance == null || CatController.Instance.IsBusy)
                return;
            completed = true;
            LevelManager.Instance?.CompleteLevel();
        }
    }
}
