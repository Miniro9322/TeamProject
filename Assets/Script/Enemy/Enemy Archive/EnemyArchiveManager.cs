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
    [Tooltip("여기 등록한 오브젝트가 하나라도 켜지면 도감을 닫는다. 다른 UI의 루트 패널을 넣으면 된다.")]
    [SerializeField] private GameObject[] closeWhenOpened;
    private CancellationTokenSource cts;
    private bool isOpenCheck;

    void Awake() => Instance = this;

    // 도감을 열고 그 적 페이지를 띄운다. 이미 열려 있으면 페이지만 갈아끼운다.
    public void OpenAt(EnemyTable.Data data)
    {
        if (data == null || archive == null) return;

        // OpenArchiveCor는 첫 await 전까지 동기로 도므로 archive.SetActive(true)가 여기서 이미 끝난다.
        // → 그 뒤에 ShowEnemy를 불러야 EnemyArchive.OnEnable(Build) 다음 순서가 된다.
        // 보여줄 적이 이미 정해져 있으니 마지막 페이지 복원은 건너뛴다(두 번 펼치지 않게).
        if (!isOpenCheck) OpenArchive(restoreLastPage: false);

        if (!TryGetArchiveList(out EnemyArchive list)) return;
        list.ShowEnemy(data);
    }

    // archive 하위의 EnemyArchive를 늦게 찾아 캐시한다. 인스펙터에 안 꽂아도 동작하게.
    private bool TryGetArchiveList(out EnemyArchive list)
    {
        if (archiveList == null) archiveList = archive.GetComponentInChildren<EnemyArchive>(true);
        list = archiveList;
        if (list != null) return true;

        Debug.LogWarning("EnemyArchiveManager: archive 하위에 EnemyArchive가 없어 페이지를 띄울 수 없습니다.", this);
        return false;
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
        CloseIfOtherUiOpened();
    }

    // 등록해 둔 다른 UI가 켜지면 도감을 닫는다.
    // hidePanal(바깥 클릭)로는 못 잡는 경우를 메운다 — 그 위에 그려지는 버튼으로 연 UI는
    // hidePanal의 클릭을 거치지 않으므로 도감이 뒤에 그대로 남는다.
    private void CloseIfOtherUiOpened()
    {
        if (!isOpenCheck) return;               // 완전히 열려 있을 때만 본다
        if (closeWhenOpened == null) return;

        for (int i = 0; i < closeWhenOpened.Length; i++)
        {
            GameObject go = closeWhenOpened[i];
            if (go == null || !go.activeInHierarchy) continue;
            CloseArchive();
            return;
        }
    }

    /// <summary>
    /// 바깥에서 도감을 닫는 창구. 다른 UI 버튼의 OnClick에 직접 걸어도 된다.
    /// 열려 있을 때만 동작하고, 닫히는 중에 또 불려도 애니메이션을 다시 시작하지 않는다.
    /// </summary>
    public void CloseArchive()
    {
        if (!isOpenCheck) return;
        isOpenCheck = false;   // CloseArchiveCor가 끝나기 전에 매 프레임 다시 불리는 것을 막는다
        OnClickCloseArchive();
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
    // 도감 버튼으로 여는 경로 — 마지막으로 보던 페이지(없으면 기본 적)로 되돌린다.
    private void OnClickOpenArchive() => OpenArchive(restoreLastPage: true);

    private void OpenArchive(bool restoreLastPage)
    {
        ResetCts();
        OpenArchiveCor(cts.Token).Forget();   // 첫 await 전까지 동기 — 여기서 archive가 이미 활성화된다

        if (!restoreLastPage) return;
        if (TryGetArchiveList(out EnemyArchive list)) list.ShowLastOrDefault();
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