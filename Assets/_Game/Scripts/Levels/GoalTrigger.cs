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
        void Reset()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (CatController.Instance == null)
                return;
            if (other.transform != CatController.Instance.transform &&
                other.transform.parent != CatController.Instance.transform)
                return;
            if (CatController.Instance.IsBusy)
                return;
            if (LevelManager.Instance != null)
                LevelManager.Instance.CompleteLevel();
        }
    }
}
