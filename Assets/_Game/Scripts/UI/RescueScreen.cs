using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Data;
using PawPath.Localization;

namespace PawPath.UI
{
    public class RescueScreen : MonoBehaviour
    {
        [SerializeField] Text title;
        [SerializeField] Text body;
        [SerializeField] Button inviteButton;

        CatDefinition pending;

        void Awake()
        {
            if (inviteButton != null)
            {
                inviteButton.onClick.RemoveAllListeners();
                inviteButton.onClick.AddListener(Invite);
            }
        }

        public void Present(CatDefinition cat)
        {
            pending = cat;
            gameObject.SetActive(true);
            if (title != null)
                title.text = $"Bölüm Tamamlandı!\n{GameText.EncounterTitle(cat)}";
            if (body != null)
                body.text = cat != null ? cat.encounterDialogue : "";
            if (inviteButton != null)
            {
                var label = inviteButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = GameText.InviteHome;
            }
        }

        void Invite()
        {
            gameObject.SetActive(false);
            if (GameFlow.Instance != null)
                GameFlow.Instance.EnterHub();
        }

        public void Bind(Text titleLabel, Text bodyLabel, Button invite)
        {
            title = titleLabel;
            body = bodyLabel;
            inviteButton = invite;
            if (inviteButton != null)
            {
                inviteButton.onClick.RemoveAllListeners();
                inviteButton.onClick.AddListener(Invite);
            }
        }
    }
}
