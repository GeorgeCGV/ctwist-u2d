using UnityEngine;

public class ResultsMenuAnimationEventsHandler : MonoBehaviour
{
    public AudioClip sfxStarAppear;

    void OnStartAppear() {
        AudioManager.Instance.PlaySfx(sfxStarAppear);
    }
}
