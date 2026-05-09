using UnityEngine;
using UnityEngine.UI;

public class ComboCounterUI : MonoBehaviour
{
    [SerializeField] private Text comboText;
    [SerializeField] private float resetDelay = 1.2f;

    private int comboCount;
    private float lastHitTime = -999f;

    public static ComboCounterUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Refresh();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (comboCount > 0 && Time.time - lastHitTime > resetDelay)
        {
            comboCount = 0;
            Refresh();
        }
    }

    // 데미지가 실제로 들어간 경우에만 호출되어 콤보 시간을 갱신한다.
    public void RegisterHit(DamageInfo info)
    {
        comboCount++;
        lastHitTime = Time.time;
        Refresh();
    }

    private void Refresh()
    {
        if (comboText == null)
            return;

        comboText.gameObject.SetActive(comboCount > 0);
        comboText.text = comboCount <= 1 ? "1 HIT" : $"{comboCount} COMBO";
    }
}
