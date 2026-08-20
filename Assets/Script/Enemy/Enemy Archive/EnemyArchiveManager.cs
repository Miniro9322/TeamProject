using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EnemyArchiveManager : MonoBehaviour
{
    // 스테이지 정보 팝업은 풀링 프리팹이라 씬 오브젝트를 인스펙터로 참조할 수 없다 → 런타임 창구.
    public static EnemyArchiveManager Instance { get; private set; }

    public GameObject archive;
    public GameObject guardPanal;
    public Button hidePanal;
    public Button infoOpenButton;
    public Button infoCloseButton;
    [Tooltip("적 목록 컴포넌트. 비워두면 archive 하위에서 찾는다.")]
    [SerializeField] private EnemyArchive archiveList;
    private CancellationTokenSource cts;
    private bool isOpenCheck;

    void Awake() => Instance = this;

    // 도감을 열고 그 적 페이지를 띄운다. 이미 열려 있으면 페이지만 갈아끼운다.
    public void OpenAt(EnemyTable.Data data)
    {
        if (data == null || archive == null) return;

        // OpenArchiveCor는 첫 await 전까지 동기로 도므로 archive.SetActive(true)가 여기서 이미 끝난다.
        // → 그 뒤에 ShowEnemy를 불러야 EnemyArchive.OnEnable(Build) 다음 순서가 된다.
        if (!isOpenCheck) OnClickOpenArchive();

        if (archiveList == null) archiveList = archive.GetComponentInChildren<EnemyArchive>(true);
        if (archiveList == null)
        {
            Debug.LogWarning("EnemyArchiveManager: archive 하위에 EnemyArchive가 없어 페이지를 띄울 수 없습니다.", this);
            return;
        }
        archiveList.ShowEnemy(data);
    }

    void Start()
    {
        archive.SetActive(false);
        archive.transform.localScale = Vector3.zero; // 닫힘 = 스케일 0 기준 (resume 로직이 진행도를 스케일로 읽음)
        isOpenCheck = false;
        guardPanal.SetActive(false);
        hidePanal.gameObject.SetActive(false);
        ResetCts();
        infoOpenButton.onClick.AddListener(OnClickOpenArchive);
        infoCloseButton.onClick.AddListener(OnClickCloseArchive); // 닫기 창구 통일
        hidePanal.onClick.AddListener(OnClickCloseArchive);        // 여기서 한 번만 등록(열 때마다 누적 방지)
        // 버튼 라벨을 코드가 채우므로 LocalizeText가 없다 → 언어 전환 이벤트를 직접 받아 갱신한다.

    }


    // 진행 중이던 애니메이션 취소 + 새 토큰 발급
    private void ResetCts()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
    }
    void Update()
    {
        OnEscInput();
    }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }
    private void OnEscInput()
    {
        if (Keyboard.current == null) return;
        if(Time.timeScale==0)return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame&&isOpenCheck)
            OnClickCloseArchive();   // 동일 닫기 창구 재사용
    }
    private void OnClickOpenArchive()
    {
        ResetCts();
        OpenArchiveCor(cts.Token).Forget();
    }
    // 버튼/판넬/ESC 공용 닫기 창구 — 진행 중이던 열기 코루틴을 취소하고 닫는다(동시 실행 방지)
    private void OnClickCloseArchive()
    {
        ResetCts();
        CloseArchiveCor(cts.Token).Forget();
    }
    private async UniTask OpenArchiveCor(CancellationToken token)
    {
        archive.SetActive(true);
        guardPanal.SetActive(true);
        float t = Progress01(archive.transform.localScale); // 현재 스케일에서 이어서 열기(연타 시 튐 방지)
        float speed = 5f;
        EnemySoundManager.Play("BookOpen");
        while(t<1f)
        {
            t+=Time.unscaledDeltaTime*speed;
            archive.transform.localScale = Vector3.Lerp(Vector3.zero,Vector3.one,t);
            await UniTask.Yield(token);
        }
        hidePanal.gameObject.SetActive(true);
        guardPanal.SetActive(false);
        isOpenCheck =true;
    }
    private async UniTask CloseArchiveCor(CancellationToken token)
    {
        guardPanal.SetActive(true);
        hidePanal.gameObject.SetActive(false);
        float t = 1f - Progress01(archive.transform.localScale); 
        float speed = 5f;
        EnemySoundManager.Play("BookClose");
        while(t<1f)
        {
            t+=Time.unscaledDeltaTime*speed;
            archive.transform.localScale = Vector3.Lerp(Vector3.one,Vector3.zero,t);
            await UniTask.Yield(token);
        }
        guardPanal.SetActive(false);
        archive.SetActive(false);
        isOpenCheck =false;
    }
    

    // 현재 스케일이 0~1 열림 진행도의 어디쯤인지(연타/중간취소 시 이어서 애니메이션)
    private static float Progress01(Vector3 scale)
        => Mathf.Clamp01(scale.x);
}