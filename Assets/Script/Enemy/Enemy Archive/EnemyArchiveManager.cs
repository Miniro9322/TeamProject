using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EnemyArchiveManager : MonoBehaviour
{
    public GameObject archive;
    public GameObject guardPanal;
    public Button hidePanal;
    public Button enemyInfoButton;
    public Button infoOpenButton;
    public Button infoCloseButton;
    private CancellationTokenSource cts;
    private Camera cam;
    private bool isOpenCheck;

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
        while(t<1f)
        {
            t+=Time.deltaTime*speed;
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
        hidePanal.gameObject.SetActive(false);   // 닫는 중엔 뒤 판넬 클릭 막기
        float t = 1f - Progress01(archive.transform.localScale); // 현재 스케일에서 이어서 닫기
        float speed = 5f;
        while(t<1f)
        {
            t+=Time.deltaTime*speed;
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