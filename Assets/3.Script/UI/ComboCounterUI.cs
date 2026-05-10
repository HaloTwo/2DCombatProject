using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ComboCounterUI : MonoBehaviour
{
    [SerializeField] private Text comboText;
    [SerializeField] private float resetDelay = 1.2f;
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float popScale = 1.18f;

    private int comboCount;
    private float lastComboTime = -999f;
    private Coroutine popRoutine;

    public static ComboCounterUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Refresh();
    }

    private void OnEnable()
    {
        Hitbox.OnAnyHit += HandleAnyHit;
        Health.OnAnyDead += HandleAnyDead;
    }

    private void OnDisable()
    {
        Hitbox.OnAnyHit -= HandleAnyHit;
        Health.OnAnyDead -= HandleAnyDead;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (comboCount > 0 && Time.time - lastComboTime > resetDelay)
        {
            comboCount = 0;
            Refresh();
        }
    }

    // 플레이어 공격이 실제로 명중했을 때만 콤보를 갱신한다.
    private void HandleAnyHit(Health target, DamageInfo info)
    {
        if (info.AttackerTeam != Team.Player || target == null || target.Team != Team.Enemy)
            return;

        RegisterCombo();
    }

    // 연속 처치도 콤보를 이어가게 해서 제출 영상에서 전투 템포가 더 잘 보이도록 한다.
    private void HandleAnyDead(Health dead)
    {
        if (dead == null || dead.Team != Team.Enemy)
            return;

        if (Time.time - lastComboTime <= 0.05f)
            return;

        RegisterCombo();
    }

    public void RegisterHit(DamageInfo info)
    {
        if (info.AttackerTeam == Team.Player)
            RegisterCombo();
    }

    private void RegisterCombo()
    {
        comboCount++;
        lastComboTime = Time.time;
        Refresh();
        PlayPop();
    }

    private void Refresh()
    {
        if (comboText == null)
            return;

        bool visible = comboCount > 0;
        comboText.gameObject.SetActive(visible);
        if (!visible)
            return;

        if (comboCount >= 10)
            comboText.text = $"COMBO x{comboCount}";
        else if (comboCount >= 3)
            comboText.text = $"x{comboCount} COMBO";
        else
            comboText.text = $"{comboCount} HIT";
    }

    private void PlayPop()
    {
        if (comboText == null || !isActiveAndEnabled)
            return;

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(CoPop());
    }

    private IEnumerator CoPop()
    {
        Transform target = comboText.transform;
        float half = Mathf.Max(0.01f, popDuration * 0.5f);
        float time = 0f;

        while (time < half)
        {
            time += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(Vector3.one, Vector3.one * popScale, Mathf.Clamp01(time / half));
            yield return null;
        }

        time = 0f;
        while (time < half)
        {
            time += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(Vector3.one * popScale, Vector3.one, Mathf.Clamp01(time / half));
            yield return null;
        }

        target.localScale = Vector3.one;
        popRoutine = null;
    }
}
