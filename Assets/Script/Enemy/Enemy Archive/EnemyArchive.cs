using UnityEngine;
using UnityEngine.UI;

// 도감 목록. EnemyTable의 모든 적을 순회하며 버튼 프리팹을 content에 하나씩 생성한다.
public class EnemyArchive : MonoBehaviour
{
    [Tooltip("적 하나당 생성할 버튼 프리팹")]
    [SerializeField] private Button enemyButtonPrefab;
    [Tooltip("버튼들이 담길 부모 (Scroll View의 Content 등)")]
    [SerializeField] private Transform content;
    [Tooltip("클릭 시 상세정보를 출력할 패널")]
    [SerializeField] private EnemyInfo infoPanel;

    private bool built;   // 최초 1회만 생성 (도감을 여러 번 열어도 중복 생성 방지)

    void Start()
    {
        Build();
    }

    private void Build()
    {
        if (built) return;

        var enemyTable = DataTableManager.EnemyTable;
        if (enemyTable == null || enemyButtonPrefab == null || content == null)
        {
            Debug.LogWarning("EnemyArchive: 참조 누락 — EnemyTable / 버튼프리팹 / Content 를 확인하세요.");
            return;
        }
        built = true;

        foreach (var data in enemyTable.GetAll())
        {
            Button btn = Instantiate(enemyButtonPrefab, content);
            btn.name = $"EnemyBtn_{data.Name}";

            // var icon = btn.GetComponentInChildren<Image>(); // 아직 이미지없음
            // if (icon != null) icon.sprite = Resources.Load<Sprite>($"Image/{data.Name}");

            // 클릭 시 상세정보 출력
            var captured = data;   // 클로저 캡처 함정 방지용 복사
            btn.onClick.AddListener(() => { if (infoPanel != null) infoPanel.Info(captured); });
        }
    }
}
