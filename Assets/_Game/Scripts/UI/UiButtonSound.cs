using UnityEngine;
using UnityEngine.EventSystems;
using PawPath.Audio;

namespace PawPath.UI
{
    public class UiButtonSound : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            CozyAudioManager.Instance?.PlayUiClick();
        }
    }
}
