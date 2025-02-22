using UnityEngine;

public class UIResultsMenuAnimationEventsHandler : MonoBehaviour
{
    public AudioClip sfxStarAppear;

    void OnStartAppear() {
        AudioManager.Instance.PlaySfx(sfxStarAppear);
    }
}
