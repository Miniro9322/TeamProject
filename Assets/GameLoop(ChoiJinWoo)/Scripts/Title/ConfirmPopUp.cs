 using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 팝업 오브젝트 1개를 문구,확인 콜백만 갈아끼워 재사용.
public class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action onConfirmed;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
        gameObject.SetActive(false);
    }

    // 문구와 확인 콜백을 갈아끼워 팝업을 띄운다.
    public void ShowPopup(string message, Action onConfirmed)
    {
        messageText.text = message;
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

    private void HidePopup()
    {
        onConfirmed = null;
        gameObject.SetActive(false);
    }
}
