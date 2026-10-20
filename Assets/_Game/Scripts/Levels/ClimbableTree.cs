using UnityEngine;
using PawPath.Cat;
using PawPath.Gameplay;
using PawPath.Core;
using PawPath.Localization;
using System.Collections.Generic;
using PawPath.Economy;
using PawPath.Audio;
using PawPath.UI;
using UnityEngine.UI;

namespace PawPath.Levels
{
    /// <summary>Jump climbs only at a grounded trunk, placed away from gap approaches.</summary>
    [DefaultExecutionOrder(-100)]
    public class ClimbableTree : MonoBehaviour
    {
        Sprite[] climbingFrames;
        Transform perch;
        CatController nearbyCat;
        readonly HashSet<Collider2D> contacts = new HashSet<Collider2D>();
        GameObject climbHint;
        Button climbButton;
        CatController climbingCat;
        GameObject rewardVisual;
        bool rewardCollected;
        public const int TopRewardPoints = 5;
        public const float GapApproachClearance = 3.5f;

        public void Configure(Sprite[] frames, Transform perchPoint)
        {
            climbingFrames = frames;
            perch = perchPoint;
            if (climbHint == null)
            {
                climbHint = ObstacleActionHint.Create(transform, true, GameText.L("Tırman", "Climb", "Klettern"));
                CreateClimbControl();
            }
            if (rewardVisual == null && perch != null) CreateTopReward();
        }

        void CreateClimbControl()
        {
            // A transparent UI hit target over the existing world marker. No physics collider.
            var control = new GameObject("ClimbControl", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            control.transform.SetParent(climbHint.transform, false);
            var rect = control.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 110f); rect.localScale = Vector3.one * .01f;
            rect.localPosition = new Vector3(0f, .22f, 0f);
            var canvas = control.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main; canvas.sortingOrder = 20;
            var target = new GameObject("ClimbButton", typeof(RectTransform), typeof(Image), typeof(Button));
            target.transform.SetParent(control.transform, false);
            var targetRect = target.GetComponent<RectTransform>(); targetRect.anchorMin = Vector2.zero; targetRect.anchorMax = Vector2.one;
            targetRect.offsetMin = targetRect.offsetMax = Vector2.zero;
            var image = target.GetComponent<Image>(); image.color = Color.clear;
            climbButton = target.GetComponent<Button>(); climbButton.targetGraphic = image;
            climbButton.transition = Selectable.Transition.None; climbButton.interactable = false;
            climbButton.onClick.AddListener(RequestClimb);
        }

        bool CanClimbNow => isActiveAndEnabled && (GameFlow.Instance == null || GameFlow.Instance.State == GameFlowState.Gameplay) &&
            nearbyCat != null && !nearbyCat.IsBusy && nearbyCat.CanStartClimb && perch != null &&
            nearbyCat.transform.position.y < perch.position.y - .25f;

        public void RequestClimb()
        {
            RefreshNearbyCat();
            StartClimb();
        }

        void StartClimb()
        {
            if (!CanClimbNow) return;
            var own = nearbyCat.Definition != null ? nearbyCat.Definition.climbFrames : null;
            nearbyCat.BeginClimb(perch.position, own != null && own.Length > 0 ? own : climbingFrames, 2.8f);
            if (nearbyCat.IsClimbing) climbingCat = nearbyCat;
        }

        void RefreshNearbyCat()
        {
            var trigger = GetComponent<Collider2D>();
            contacts.RemoveWhere(contact => contact == null || !contact.enabled || !contact.gameObject.activeInHierarchy ||
                (trigger != null && !trigger.bounds.Intersects(contact.bounds)));
            nearbyCat = null;
            foreach (var contact in contacts)
            {
                var candidate = contact.GetComponentInParent<CatController>();
                if (candidate != null) { nearbyCat = candidate; break; }
            }
        }

        void CreateTopReward()
        {
            rewardVisual = new GameObject("TreeTopReward");
            rewardVisual.transform.SetParent(perch, false);
            rewardVisual.transform.localScale = new Vector3(1f / perch.lossyScale.x, 1f / perch.lossyScale.y, 1f);
            rewardVisual.transform.position = perch.position + Vector3.up * .1f;
            var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(rewardVisual.transform, false); glow.sprite = CozyIconFactory.SoftGlow();
            glow.color = new Color(1f, .83f, .3f, .65f); glow.sortingOrder = 9;
            glow.transform.localScale = Vector3.one * (1.15f / glow.sprite.bounds.size.x);
            var coin = new GameObject("Coin").AddComponent<SpriteRenderer>();
            coin.transform.SetParent(rewardVisual.transform, false); coin.sprite = CozyIconFactory.Coin();
            coin.transform.localPosition = new Vector3(-.24f, 0f, 0f); coin.sortingOrder = 11;
            coin.transform.localScale = Vector3.one * (.44f / coin.sprite.bounds.size.x);
            var amount = new GameObject("Amount").AddComponent<TextMesh>();
            amount.transform.SetParent(rewardVisual.transform, false); amount.transform.localPosition = new Vector3(.19f, 0f, 0f);
            amount.text = "+" + TopRewardPoints; amount.fontSize = 64; amount.characterSize = .05f;
            amount.fontStyle = FontStyle.Bold; amount.anchor = TextAnchor.MiddleCenter;
            amount.color = new Color(1f, .95f, .72f); amount.GetComponent<MeshRenderer>().sortingOrder = 12;
            var shadow = new GameObject("AmountShadow").AddComponent<TextMesh>();
            shadow.transform.SetParent(rewardVisual.transform, false);
            shadow.transform.localPosition = amount.transform.localPosition + new Vector3(.025f, -.025f, 0f);
            shadow.text = amount.text; shadow.fontSize = amount.fontSize; shadow.characterSize = amount.characterSize;
            shadow.fontStyle = amount.fontStyle; shadow.anchor = amount.anchor;
            shadow.color = new Color(.12f, .10f, .07f, .9f); shadow.GetComponent<MeshRenderer>().sortingOrder = 11;
            rewardVisual.SetActive(!rewardCollected);
        }

        void TryCollectTopReward()
        {
            // Reward arrival, not the first upward tap or a contact at the trunk.
            if (rewardCollected || !MobSimulationGate.GameplayActive || climbingCat == null ||
                climbingCat.IsBusy || !climbingCat.IsPerched || perch == null || CozyEconomyManager.Instance == null ||
                Mathf.Abs(climbingCat.transform.position.x - perch.position.x) > .8f ||
                Mathf.Abs(climbingCat.transform.position.y - perch.position.y) > .3f) return;
            rewardCollected = true; // Reserve before callbacks; multiple colliders/frames cannot pay twice.
            if (rewardVisual != null) rewardVisual.SetActive(false);
            CozyEconomyManager.Instance.AddShoppingPoints(TopRewardPoints, "Ağaç tepesi ödülü");
            CozyAudioManager.Instance?.PlayConfirm();
        }

        void Update()
        {
            TryCollectTopReward();
            RefreshNearbyCat();
            bool gameplay = GameFlow.Instance == null || GameFlow.Instance.State == GameFlowState.Gameplay;
            var player = CatController.Instance;
            bool showHint = gameplay && player != null && !player.IsBusy && !player.IsClimbing &&
                Mathf.Abs(player.transform.position.x - transform.position.x) < 6f;
            if (climbHint != null)
            {
                climbHint.SetActive(showHint);
                if (showHint)
                {
                    float ground = GetComponent<Collider2D>().bounds.min.y;
                    climbHint.transform.position = new Vector3(transform.position.x,
                        ground + 1.45f + Mathf.Sin(Time.unscaledTime * 4f) * .06f, 0f);
                }
            }
            if (climbButton != null)
            {
                climbButton.interactable = CanClimbNow;
                climbButton.targetGraphic.raycastTarget = climbButton.interactable;
            }
            HandleJumpToClimb(MobileControlState.JumpQueued || Input.GetKeyDown(KeyCode.UpArrow) ||
                Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space));
        }

        void HandleJumpToClimb(bool jumpPressed)
        {
            if (!jumpPressed || !CanClimbNow) return;
            StartClimb();
            if (nearbyCat != null && nearbyCat.IsClimbing) MobileControlState.ConsumeJump();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var cat = other.GetComponentInParent<CatController>();
            if (cat != null) { contacts.Add(other); nearbyCat = cat; }
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (other.GetComponentInParent<CatController>() != null) contacts.Add(other);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            contacts.Remove(other);
        }

        void OnDisable()
        {
            contacts.Clear(); nearbyCat = null;
            climbingCat = null;
            if (climbHint != null) climbHint.SetActive(false);
        }
    }
}
