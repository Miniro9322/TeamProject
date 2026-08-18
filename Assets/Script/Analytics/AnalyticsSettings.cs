using UnityEngine;

// Resources/AnalyticsSettings 에셋(선택 사항)으로 배치. 없으면 AnalyticsRecorder가 기본값(원격 전송 비활성화)으로 동작한다.
[CreateAssetMenu(fileName = "AnalyticsSettings", menuName = "Analytics/AnalyticsSettings")]
public class AnalyticsSettings : ScriptableObject
{
    [Tooltip("이벤트 배치를 전송할 서버 엔드포인트. 비워두면 로컬 파일에만 기록하고 원격 전송은 하지 않는다.")]
    public string remoteEndpointUrl;

    [Tooltip("버퍼를 로컬 파일/서버로 내보내는 주기(초).")]
    public float flushIntervalSeconds = 15f;

    [Tooltip("이 개수만큼 이벤트가 쌓이면 flushIntervalSeconds를 기다리지 않고 즉시 내보낸다.")]
    public int maxBufferedEvents = 50;
}
