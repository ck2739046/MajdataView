using UnityEngine;
using UnityEngine.UI;

public class DataVisibleToggle : MonoBehaviour
{
    public Sprite onSprite;
    public Sprite offSprite;
    public GameObject[] targets;

    private Toggle toggle;
    private Image icon;

    // 场景重载前保存开关状态，重载后自动恢复
    private static bool? savedState;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        icon = GetComponent<Image>();
    }

    private void Start()
    {
        if (savedState.HasValue)
        {
            toggle.isOn = savedState.Value;
            savedState = null;
        }
        Apply(toggle.isOn);
        toggle.onValueChanged.AddListener(Apply);
    }

    private void OnDestroy()
    {
        savedState = toggle.isOn;
    }

    private void Apply(bool on)
    {
        icon.sprite = on ? onSprite : offSprite;
        foreach (var target in targets)
            if (target != null)
                target.SetActive(on);
    }
}
