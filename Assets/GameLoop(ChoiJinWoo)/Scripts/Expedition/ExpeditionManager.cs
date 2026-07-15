//using UnityEngine;

//public class ExpeditionManager : MonoBehaviour
//{
//    private bool userStronger = false;
//    private int sRank = 20;
//    private int bRank = 70;
//    private int fRank = 10;

//    //실패, 성공, 대성공 각각 기초 비율이 있고, 권장 전투력과 원정 인원 총 전투력에 비례하여 각각의 확률이 변동 단 최소치 밑으로는 내려가지 않음
//    //원정 인원 총 전투력 > 권장 전투력 기준 실패 : 10%, 성공 : 70%, 대성공 : 20%
//    //차이나는 비율의 절반 만큼 대성공 확률 감소 실패, 성공 총 퍼센티지 증가 후 비율 절반의 절반씩 실패 증가 성공 감소

//    public void CalculateReward(int cp, ExpeditionLevel level)
//    {
//        userStronger = level.RecommendStat - cp < 0;
//        var gap = 100 - (100f * cp / level.RecommendStat);

//        if (!userStronger)
//        {
//            sRank -= Mathf.CeilToInt(sRank * gap * 0.5f);
//            bRank += Mathf.CeilToInt((100 - sRank) * gap * 0.5f * 0.3f);
//            fRank += Mathf.CeilToInt((100 - sRank) * gap * 0.5f * 0.7f);
//        }

//    }
//}
