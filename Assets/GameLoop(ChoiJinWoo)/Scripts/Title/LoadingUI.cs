using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private List<GameObject> loadingHeros;
    [SerializeField] private List<Transform> spawnPositions;
    // 로딩 히어로를 RenderTexture로 그리는 전용 카메라 - 이 패널이 떠 있는 동안에만 켠다.
    [SerializeField] private Camera loadingCamera;
    private readonly int[] animatorTriggers = new int[] { Animator.StringToHash("Dance1"), Animator.StringToHash("Dance2"), Animator.StringToHash("Dance3"), Animator.StringToHash("Dance4"), Animator.StringToHash("Dance5") };
    [SerializeField] private TextMeshProUGUI loadingText;
    private CancellationTokenSource dotsCts;
    private readonly List<GameObject> spawnedHeros = new List<GameObject>();

    private void OnEnable()
    {
        if (loadingCamera != null) loadingCamera.enabled = true;

        var animation = animatorTriggers[UnityEngine.Random.Range(0, animatorTriggers.Length)];

        for (int i = 0; i < spawnPositions.Count; i++)
        {
            var go = Instantiate(loadingHeros[UnityEngine.Random.Range(0, loadingHeros.Count)], spawnPositions[i]);
            go.GetComponent<Animator>().SetTrigger(animation);
            spawnedHeros.Add(go);
        }

        dotsCts = new CancellationTokenSource();
        AnimateDots(dotsCts.Token).Forget();
    }

    private void OnDisable()
    {
        dotsCts?.Cancel();
        dotsCts?.Dispose();

        for (int i = 0; i < spawnedHeros.Count; i++)
        {
            if (spawnedHeros[i] != null) Destroy(spawnedHeros[i]);
        }
        spawnedHeros.Clear();

        if (loadingCamera != null) loadingCamera.enabled = false;
    }

    private async UniTask AnimateDots(CancellationToken token)
    {
        await UniTask.Yield();

        const string baseText = "Loading";
        int dotCount = 0;

        while (!token.IsCancellationRequested)
        {
            dotCount = (dotCount % 3) + 1; // 1 -> 2 -> 3 -> 1 ...
            loadingText.text = baseText + new string('.', dotCount);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);
        }
    }
}
