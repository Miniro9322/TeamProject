using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 팝업 오브젝트 1개를 문구,확인 콜백만 갈아끼워 재사용.
public class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirmed;

    // 확인 버튼과 취소 버튼에 실행 메서드를 연결한다.
    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
    }

    // 문구와 확인 콜백을 갈아끼워 팝업을 띄운다.
    public void ShowPopup(string message, string buttonLabel, Action onConfirmed)
    {
        messageText.text = message;
        confirmText.text = buttonLabel;
        this.onConfirmed = onConfirmed;
        gameObject.SetActive(true);
    }

    // 확인 버튼: 콜백을 1회 실행하고 닫는다.
    private void OnConfirm()
    {
        Action callback = onConfirmed;
        HidePopup();
        callback.Invoke();
    }

    // 취소 버튼: 아무 것도 실행하지 않고 닫는다.
    private void OnCancel()
    {
        HidePopup();
    }

    // 저장된 동작을 비우고 팝업을 닫는다.
    private void HidePopup()
    {
        onConfirmed = null;
        gameObject.SetActive(false);
    }
}
