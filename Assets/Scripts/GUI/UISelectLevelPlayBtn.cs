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

        // activate/deactivate play button based on current player level index
        int currentLevel = PlayerPrefs.GetInt("currentLevel", 0);
        GameObject playBtn = transform.Find("PlayBtn").gameObject;
        playBtn.GetComponent<Button>().interactable = id <= currentLevel;

        GameObject label = playBtn.transform.Find("Label").gameObject;
        label.GetComponent<TextMeshProUGUI>().text = (id + 1).ToString();

        // highlight current level
        highlight.SetActive(id == currentLevel);

        // check how many stars player earned for that level
        int starsAmount = PlayerPrefs.GetInt("stars" + id, 0);
        for (int i = 0; i < starsAmount; i++) {
            stars[i].SetActive(true);
        }
    }

    public void OnPlay() {
        AudioManager.Instance.PlaySfx((int)AudioManager.SFX.BtnClick);
        LoadScreen.Instance.LoadLevel(id);
    }
}
