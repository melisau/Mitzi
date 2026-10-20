using PawPath.Core;
using PawPath.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace PawPath.UI
{
    public sealed class MitziPhoneUI : MonoBehaviour
    {
        public const string InstagramUrl = "https://www.instagram.com/egyptianmaumitzi/";
        public const string AccountHandle = "@egyptianmaumitzi";
        public const string AccountName = "Mitzi Tosuncuk";
        GameObject panel;
        Button launcher;
        GameFlow flow;
        Image avatar;
        Image[] photoTiles;
        Sprite[] album;
        GameObject parentGate;
        Text challenge, gateStatus, verifyLabel;
        InputField answer;
        int expectedAnswer;
        bool parentApproved;
        public bool IsOpen => panel != null && panel.activeSelf;
        public bool AlbumLoaded => album != null;
        public bool IsParentGateOpen => parentGate != null && parentGate.activeSelf;
        public bool ParentApproved => parentApproved;
        public void BindAlbum(Image avatarPhoto, Image[] photos) { avatar=avatarPhoto;photoTiles=photos; }
        public void BindParentGate(GameObject gate, Text question, InputField input, Text status, Button verify, Button cancel, Text verifyText)
        {
            parentGate=gate;challenge=question;answer=input;gateStatus=status;verifyLabel=verifyText;
            verify.onClick.AddListener(OnVerifyPressed);cancel.onClick.AddListener(CancelParentGate);parentGate.SetActive(false);
        }
        public void Bind(GameObject overlay, Button phoneButton, Button close, Button instagram)
        {
            panel = overlay; launcher = phoneButton;
            launcher.onClick.AddListener(Open); close.onClick.AddListener(Close);
            instagram.onClick.AddListener(OpenInstagram);
            panel.SetActive(false); Refresh();
        }
        void OnEnable()
        {
            flow = GameFlow.Instance;
            if (flow != null) flow.StateChanged += OnState;
            GameEvents.OnHubEntered += Refresh; Refresh();
        }
        void OnDisable()
        {
            if (flow != null) flow.StateChanged -= OnState;
            GameEvents.OnHubEntered -= Refresh; Close();
        }
        void OnState(GameFlowState state) => Refresh();
        void Refresh()
        {
            bool home = GameFlow.Instance != null && GameFlow.Instance.InHub;
            if (launcher != null) launcher.gameObject.SetActive(home);
            if (!home) Close();
        }
        public void Open()
        {
            if (panel == null || GameFlow.Instance == null || !GameFlow.Instance.InHub) return;
            CancelParentGate();
            if (album == null)
            {
                album=Resources.LoadAll<Sprite>("MitziPhone/mitzi-instagram-photos");
                System.Array.Sort(album,(a,b)=>string.CompareOrdinal(a.name,b.name));
                for(int i=0;photoTiles!=null && i<photoTiles.Length;i++)
                    if(i<album.Length) { photoTiles[i].sprite=album[i];photoTiles[i].color=Color.white; }
                var profilePhotos=Resources.LoadAll<Sprite>("MitziPhone/mitzi-profile-photo");
                if(avatar!=null && profilePhotos.Length>0)avatar.sprite=profilePhotos[0];
            }
            panel.transform.SetAsLastSibling(); panel.SetActive(true);
        }
        public void Close() { CancelParentGate();if (panel != null) panel.SetActive(false); }
        public void OpenInstagram()
        {
            if (!IsOpen || GameFlow.Instance == null || !GameFlow.Instance.InHub || parentGate==null) return;
            parentApproved=false;parentGate.SetActive(true);NewChallenge();
        }
        void NewChallenge()
        {
            int a=Random.Range(12,30),b=Random.Range(12,30);expectedAnswer=a*b;
            challenge.text=a+" × "+b+" = ?";answer.text="";gateStatus.text="";
            verifyLabel.text=GameText.L("Doğrula","Verify","Bestätigen");
        }
        public void CancelParentGate() { parentApproved=false;if(parentGate!=null)parentGate.SetActive(false); }
        public bool SubmitParentAnswer()
        {
            if(!IsOpen || !IsParentGateOpen || parentApproved || GameFlow.Instance==null || !GameFlow.Instance.InHub)return false;
            if(!int.TryParse(answer.text,out int value) || value!=expectedAnswer)
            {
                NewChallenge();gateStatus.text=GameText.L("Tekrar deneyin.","Please try again.","Bitte erneut versuchen.");return false;
            }
            parentApproved=true;answer.text="";
            gateStatus.text=GameText.L("Instagram oyun dışında açılacak.","Instagram will open outside the game.","Instagram wird außerhalb des Spiels geöffnet.");
            verifyLabel.text=GameText.L("Instagram’da aç","Open Instagram","Instagram öffnen");return true;
        }
        void OnVerifyPressed()
        {
            if(!parentApproved){SubmitParentAnswer();return;}
            if(!IsOpen || !IsParentGateOpen || GameFlow.Instance==null || !GameFlow.Instance.InHub)return;
            CancelParentGate();Application.OpenURL(InstagramUrl);
        }
    }
}
