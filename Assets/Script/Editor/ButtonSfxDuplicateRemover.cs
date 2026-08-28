#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ButtonSfxAutoAttacher가 과거 "이미 붙어 있는지"를 GetComponent로 확인하지 않고 자기 인스턴스의
/// 로컬 처리 목록으로만 확인하던 시절, 런타임에 추가된 중복 ButtonSfx가 프리팹에 그대로 구워진 채
/// 남아 있는 경우가 있다(예: BasePanel.prefab). 버튼마다 첫 번째 ButtonSfx만 남기고 나머지는 지운다.
///
/// 사용법: 프리팹을 편집 모드로 열거나 씬에서 대상을 선택한 뒤, 하이어라키에서 루트(또는 원하는
/// 오브젝트)를 선택하고 이 메뉴를 실행한다. 자식까지 전부 훑어서 처리한다.
/// </summary>
public static class ButtonSfxDuplicateRemover
{
    [MenuItem("Tools/UI/선택 항목의 중복 ButtonSfx 제거 (자식 포함)")]
    public static void RemoveDuplicates()
    {
        GameObject[] targets = Selection.gameObjects;
        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning("[ButtonSfxDuplicateRemover] 선택된 오브젝트가 없습니다. 하이어라키에서 대상을 선택하세요.");
            return;
        }

        int buttonsChecked = 0, removed = 0;
        var log = new System.Text.StringBuilder("[ButtonSfxDuplicateRemover] 처리 결과\n");

        foreach (GameObject root in targets)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                buttonsChecked++;
                ButtonSfx[] copies = button.GetComponents<ButtonSfx>();
                if (copies.Length <= 1) continue;

                for (int i = 1; i < copies.Length; i++)
                {
                    Undo.DestroyObjectImmediate(copies[i]);
                    removed++;
                }
                log.AppendLine($"  [{copies.Length - 1}개 제거] {Path(button.transform)}");
            }
        }

        log.AppendLine($"\n버튼 확인 {buttonsChecked}개 / 중복 제거 {removed}개");
        log.AppendLine("프리팹/씬을 저장하는 걸 잊지 마세요 (Ctrl+S).");
        Debug.Log(log.ToString());
    }

    private static string Path(Transform t)
    {
        var sb = new System.Text.StringBuilder(t.name);
        for (Transform p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
        return sb.ToString();
    }
}
#endif
