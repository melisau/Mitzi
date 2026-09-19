using UnityEngine;
using PawPath.Cat;
using PawPath.Levels;
using PawPath.Core;

namespace PawPath.Gameplay
{
    public class LevelGoal : MonoBehaviour
    {
        private bool isTriggered = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isTriggered) return;

            var cat = other.GetComponent<CatController>();
            if (cat != null)
            {
                isTriggered = true;
                OnGoalReached();
            }
        }

        private void OnGoalReached()
        {
            Debug.Log("[PawPath] Seviye Başarıyla Tamamlandı!");

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.CompleteLevel();
            }
            else
            {
                GameEvents.LevelCompleted();
            }
        }

        public void ResetGoal()
        {
            isTriggered = false;
        }
    }
}