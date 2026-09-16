using UnityEngine;
using PawPath.Content;
using PawPath.Data;

namespace PawPath.Core
{
    /// <summary>
    /// Tek sahneli akış: Hub <-> Seviye. DontDestroyOnLoad gerekmez; sahne kurucu bunu kök nesneye koyar.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] PawPathCatalog catalog;
        [SerializeField] GameObject hubRoot;
        [SerializeField] GameObject levelRoot;
        [SerializeField] GameObject hudRoot;
        [SerializeField] GameObject rescueRoot;

        public PawPathCatalog Catalog => catalog;
        public bool InHub { get; private set; } = true;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SaveService.Load();
            if (catalog == null)
                catalog = CatalogFactory.CreateRuntime();
        }

        void Start()
        {
            EnterHub();
        }

        public void EnterHub()
        {
            InHub = true;
            SetRoots(hub: true, level: false, hud: true, rescue: false);
            GameEvents.HubEntered();
        }

        public void StartNextLevel()
        {
            InHub = false;
            SetRoots(hub: false, level: true, hud: true, rescue: false);
            if (LevelManager.Instance != null)
                LevelManager.Instance.BeginCurrentLevel();
        }

        public void ShowRescue(CatDefinition cat)
        {
            SetRoots(hub: false, level: false, hud: false, rescue: true);
            var screen = rescueRoot != null ? rescueRoot.GetComponent<UI.RescueScreen>() : null;
            if (screen != null)
                screen.Present(cat);
        }

        public void BindCatalog(PawPathCatalog value) => catalog = value;

        public void BindRoots(GameObject hub, GameObject level, GameObject hud, GameObject rescue)
        {
            hubRoot = hub;
            levelRoot = level;
            hudRoot = hud;
            rescueRoot = rescue;
        }

        void SetRoots(bool hub, bool level, bool hud, bool rescue)
        {
            if (hubRoot) hubRoot.SetActive(hub);
            if (levelRoot) levelRoot.SetActive(level);
            if (hudRoot) hudRoot.SetActive(hud);
            if (rescueRoot) rescueRoot.SetActive(rescue);
        }
    }
}
