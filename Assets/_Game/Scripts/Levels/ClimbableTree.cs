using UnityEngine;
using PawPath.Cat;
using PawPath.Gameplay;

namespace PawPath.Levels
{
    /// <summary>Kedi yakındayken yalnızca yukarı komutuyla güvenli tırmanış başlatır.</summary>
    [DefaultExecutionOrder(-100)]
    public class ClimbableTree : MonoBehaviour
    {
        Sprite[] climbingFrames;
        Transform perch;
        CatController nearbyCat;

        public void Configure(Sprite[] frames, Transform perchPoint)
        {
            climbingFrames = frames;
            perch = perchPoint;
        }

        void Update()
        {
            if (nearbyCat == null || nearbyCat.IsClimbing || !nearbyCat.CanStartClimb || perch == null ||
                nearbyCat.transform.position.y >= perch.position.y - .25f)
                return;

            bool keyboardUp = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            bool mobileUp = MobileControlState.JumpQueued;
            if (!keyboardUp && !mobileUp)
                return;

            if (mobileUp)
                MobileControlState.ConsumeJump();
            // Seçili kedinin kendi tırmanma kareleri varsa onları kullan (yoksa ortak kareler).
            var own = nearbyCat.Definition != null ? nearbyCat.Definition.climbFrames : null;
            nearbyCat.BeginClimb(perch.position, own != null && own.Length > 0 ? own : climbingFrames, 2.8f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var cat = other.GetComponentInParent<CatController>();
            if (cat != null) nearbyCat = cat;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (nearbyCat == null)
                nearbyCat = other.GetComponentInParent<CatController>();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var cat = other.GetComponentInParent<CatController>();
            if (cat != null && cat == nearbyCat)
                nearbyCat = null;
        }
    }
}
