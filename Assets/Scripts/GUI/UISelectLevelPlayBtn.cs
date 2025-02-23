using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISelectLevelPlayBtn : MonoBehaviour
{
    [SerializeField]
    private int id;

    [SerializeField]
    private GameObject highlight;

    [SerializeField]
    private List<GameObject> stars = new List<GameObject>(3);

    public void Init(int levelId)
    {
        id = levelId;
        name = "PlayLevel_" + id;

        highlight = transform.Find("UISelectLevelBtnCurrentBcg").gameObject;

        bool unlocked = GameManager.Instance.IsLevelUnlocked(id);

        // activate/deactivate play button if level is unlocked/locked
        GameObject playBtn = transform.Find("PlayBtn").gameObject;
        playBtn.GetComponent<Button>().interactable = unlocked;

        GameObject label = playBtn.transform.Find("Label").gameObject;
        label.GetComponent<TextMeshProUGUI>().text = (id + 1).ToString();

        if (unlocked) {
            // get level stars
            int starsAmount = GameManager.Instance.GetLevelStars(id);
            for (int i = 0; i < starsAmount; i++) {
                stars[i].SetActive(true);
            }

            // highlight level that is unlocked and has no stars
            highlight.SetActive(starsAmount == 0);
        }
    }

    public void OnPlay() {
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        LoadScreen.Instance.LoadLevel(id);
    }
}
