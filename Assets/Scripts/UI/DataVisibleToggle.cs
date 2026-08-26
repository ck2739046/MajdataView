using UnityEngine;
using UnityEngine.UI;

public class DataVisibleToggle : MonoBehaviour
{
    public Sprite onSprite;
    public Sprite offSprite;
    public GameObject[] targets;

    private Toggle toggle;
    private Image icon;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        icon = GetComponent<Image>();
    }

    private void Start()
    {
        Apply(toggle.isOn);
        toggle.onValueChanged.AddListener(Apply);
    }

    private void Apply(bool on)
    {
        icon.sprite = on ? onSprite : offSprite;
        foreach (var target in targets)
            if (target != null)
                target.SetActive(on);
    }
}
