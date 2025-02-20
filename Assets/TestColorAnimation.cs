using UnityEngine;

public class TestColorAnimation : MonoBehaviour
{

    [SerializeField]
    private ColorBlock.EBlockColor colorType;

    void Start()
    {
        GetComponent<ColorBlock>().ColorType = colorType;
    }
}
