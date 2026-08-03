using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 체력바 아래 디버프 아이콘 줄. EnemyBase가 소유하고 Tick으로 굴린다(EnemyCloak/EnemyHealthBar와 같은 구조).
/// 디버프가 걸리면 아이콘을 풀에서 소환해 root에 붙이고, 풀리면 풀에 반납한다.
/// 배치는 root에 달린 GridLayoutGroup이 전담 — 소환 순서대로 옆에 쌓인다.
/// root나 프리팹을 안 꽂은 프리팹이면 Setup/Tick/Reset이 전부 조용히 no-op(일반 적 전부 해당).
///
/// "지속시간이 끝나면 사라지는" 동작은 BuffManager가 만료 시 모디파이어를 벗기는 것을
/// 이쪽 폴링이 감지해서 이뤄진다 — 실제 만료 시각과 일치하되 최대 RefreshInterval만큼 늦다.
///
/// 임시 구현이다 — 걸린 디버프를 "현재 스탯이 원본보다 낮은가"로 추론한다.
/// 적에게 걸리는 디버프는 전부 BuffManager를 지나 StatContainer에 Modifier로 꽂히는데,
/// BuffManager에 조회 API가 없어서(팀원 소유 파일) 결과값을 역으로 보는 방식을 택했다.
/// 한계 둘: (1) 같은 스탯에 버프와 디버프가 동시에 걸리면 구분하지 못한다
/// (폭주가 SPD에 Flat +3을 넣으므로, 폭주한 보스가 둔화되면 순 이속이 원본보다 높아 아이콘이 안 뜬다).
/// (2) 남은 시간을 알 수 없어 쿨다운 표시(원형 게이지 등)를 할 수 없다.
/// 둘 중 하나가 필요해지면 BuffManager에서 목록을 받아 판정부만 갈아끼우면 된다.
/// </summary>
public class EnemyDebuffBar
{
    // 매 프레임 갱신할 이유가 없다. GridLayoutGroup은 자식이 드나들 때마다 레이아웃을 다시 계산한다.
    private const float RefreshInterval = 0.1f;

    private RectTransform _root;
    private GameObject _iconPrefab;
    private EnemyDebuffIcon[] _icons;
    private GameObject[] _spawned;   // 종류별 현재 소환된 아이콘. null = 안 걸린 상태
    private float _timer;

    // _spawned를 함께 보는 이유는 Setup 전에 Reset/Tick이 불려도 안전하게 no-op이 되게 하기 위함이다.
    public bool IsSetup => _icons != null && _spawned != null;

    /// <summary>EnemyBase가 Awake에서 1회 호출. root/프리팹/목록 중 하나라도 비면 이후 모든 호출이 no-op가 된다.</summary>
    public void Setup(RectTransform root, GameObject iconPrefab, EnemyDebuffIcon[] icons)
    {
        if (root == null || iconPrefab == null || icons == null || icons.Length == 0) return;

        _root = root;
        _iconPrefab = iconPrefab;
        _icons = icons;
        _spawned = new GameObject[icons.Length];
        _timer = float.MaxValue;   // 첫 Tick에서 간격을 기다리지 않고 즉시 판정
    }

    /// <summary>
    /// 걸린 디버프에 맞춰 아이콘을 소환/반납한다. baseStats는 모디파이어가 붙기 전 원본값 — EnemyBase가 ApplyData에서 기록한다.
    /// </summary>
    public void Tick(StatContainer sc, IReadOnlyDictionary<StatType, float> baseStats, bool isStunned)
    {
        if (!IsSetup || sc == null || baseStats == null) return;

        _timer += Time.deltaTime;
        if (_timer < RefreshInterval) return;
        _timer = 0f;

        for (int i = 0; i < _icons.Length; i++)
        {
            bool on = _icons[i].kind == EnemyDebuffKind.Stun
                ? isStunned
                : IsLowered(sc, baseStats, StatOf(_icons[i].kind));

            // 파괴된 오브젝트도 == null이 true라, 밖에서 사라졌으면 저절로 다시 소환된다.
            bool spawned = _spawned[i] != null;
            if (on == spawned) continue;

            if (on) Spawn(i);
            else Despawn(i);
        }
    }

    /// <summary>
    /// 풀 반납 등에서 호출. 소환된 아이콘을 전부 풀에 되돌려, 다음 스폰이 이전 개체의 아이콘을 물고 나오지 않게 한다.
    /// (EnemyBurrow의 지면 마커가 겪던 것과 같은 함정 — 풀링에선 되돌리는 경로가 생명선이다.)
    /// </summary>
    public void Reset()
    {
        if (!IsSetup) return;

        _timer = float.MaxValue;
        for (int i = 0; i < _spawned.Length; i++) Despawn(i);
    }

    private void Spawn(int index)
    {
        Sprite sprite = _icons[index].sprite;
        if (sprite == null) return;   // 종류만 골라두고 스프라이트를 안 넣은 칸은 건너뛴다

        GameObject go = PoolManager.Instance.Spawn(_iconPrefab, Vector3.zero, Quaternion.identity);
        if (go == null) return;

        // PoolManager.Spawn은 월드 이펙트 기준이라 worldPositionStays=true로 붙인다.
        // UI에 그대로 쓰면 캔버스 스케일이 1이 아닌 해상도(CanvasScaler ScaleWithScreenSize)에서
        // 크기 보정이 localScale에 들어가 아이콘이 커지거나 작아진다 — 여기서 false로 다시 붙인다.
        go.transform.SetParent(_root, false);
        go.transform.localScale = Vector3.one;

        Image img = go.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = sprite;
            img.raycastTarget = false;   // 화면 위 UI가 포탈 클릭 등을 삼키지 않게
        }

        _spawned[index] = go;
    }

    private void Despawn(int index)
    {
        if (_spawned[index] == null) { _spawned[index] = null; return; }

        PoolManager.Instance.Despawn(_spawned[index]);
        _spawned[index] = null;
    }

    // 원본보다 낮아졌으면 그 스탯이 깎인 것으로 본다.
    // baseStats에 없는 스탯은 이 적의 StatContainer에도 없다 — sc[t]가 KeyNotFoundException을 던지므로 반드시 먼저 막는다.
    private static bool IsLowered(StatContainer sc, IReadOnlyDictionary<StatType, float> baseStats, StatType type)
    {
        if (!baseStats.TryGetValue(type, out float baseValue)) return false;
        // 절대 오차만 쓰면 체력(수천 단위)에서 너무 민감하고, 비율만 쓰면 이속(1~6)에서 너무 둔하다.
        float epsilon = Mathf.Max(0.01f, Mathf.Abs(baseValue) * 0.001f);
        return sc[type] < baseValue - epsilon;
    }

    private static StatType StatOf(EnemyDebuffKind kind) => kind switch
    {
        EnemyDebuffKind.Slow      => StatType.SPD,
        EnemyDebuffKind.AtkDown   => StatType.ATK,
        EnemyDebuffKind.DefDown   => StatType.DEF,
        EnemyDebuffKind.AsDown    => StatType.AS,
        EnemyDebuffKind.MaxHpDown => StatType.HP,
        _                         => StatType.SPD,   // Stun은 여기 오지 않는다(Tick에서 먼저 분기)
    };
}

/// <summary>
/// 디버프 종류 하나와 그때 소환할 아이콘 스프라이트 짝. EnemyBase의 인스펙터에서 프리팹별로 채운다.
/// 여기 등록되고 스프라이트가 들어 있는 종류만 표시된다.
/// </summary>
[System.Serializable]
public struct EnemyDebuffIcon
{
    [Tooltip("이 아이콘이 나타낼 디버프 종류")]
    public EnemyDebuffKind kind;
    [Tooltip("해당 디버프가 걸린 동안 소환될 아이콘 이미지")]
    public Sprite sprite;
}
