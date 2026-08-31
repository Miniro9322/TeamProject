using UnityEditor;
using UnityEngine;

// 테스트용으로 자원·시민을 즉시 지급하는 에디터 전용 창
public class ResourceCheatWindow : EditorWindow
{
    private const int DefaultResourceAmount = 1000;
    private const int DefaultCitizenAmount = 10;
    private const float GrantButtonHeight = 24f;

    private int resourceAmount = DefaultResourceAmount;
    private int citizenAmount = DefaultCitizenAmount;

    // 메뉴에서 자원 치트 창을 연다
    [MenuItem("Tools/Debug/자원 치트")]
    public static void OpenCheatWindow()
    {
        GetWindow<ResourceCheatWindow>("자원 치트");
    }

    // 실행 상태를 확인하고 지급 화면을 그린다
    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("게임을 Play로 실행한 뒤에 사용할 수 있습니다.", MessageType.Info);
            return;
        }

        ResourcesManager resourcesManager = FindFirstObjectByType<ResourcesManager>();
        CitizenManager citizenManager = FindFirstObjectByType<CitizenManager>();

        if (IsManagerMissing(resourcesManager, citizenManager))
        {
            EditorGUILayout.HelpBox("현재 씬에서 자원 또는 시민 관리자를 찾지 못했습니다.", MessageType.Warning);
            return;
        }

        DrawResourceSection(resourcesManager);
        DrawCitizenSection(citizenManager);
    }

    // 자원·시민 관리자 중 하나라도 없는지 판정한다
    private bool IsManagerMissing(ResourcesManager resourcesManager, CitizenManager citizenManager)
    {
        if (resourcesManager == null)
        {
            return true;
        }

        return citizenManager == null;
    }

    // 자원 지급 수량 칸과 버튼들을 그린다
    private void DrawResourceSection(ResourcesManager resourcesManager)
    {
        EditorGUILayout.LabelField("자원", EditorStyles.boldLabel);
        resourceAmount = EditorGUILayout.IntField("지급 수량", resourceAmount);

        EditorGUILayout.BeginHorizontal();
        DrawResourceButton(resourcesManager, "나무", ProductionType.Wood);
        DrawResourceButton(resourcesManager, "식량", ProductionType.Food);
        DrawResourceButton(resourcesManager, "골드", ProductionType.Gold);
        DrawResourceButton(resourcesManager, "철", ProductionType.Iron);
        DrawResourceButton(resourcesManager, "석재", ProductionType.Stone);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawSpecialButton(resourcesManager);
        DrawGrantAllButton(resourcesManager);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    // 자원 한 종류를 지급하는 버튼을 그린다
    private void DrawResourceButton(ResourcesManager resourcesManager, string buttonLabel, ProductionType productionType)
    {
        if (!GUILayout.Button(buttonLabel, GUILayout.Height(GrantButtonHeight)))
        {
            return;
        }

        GrantSingleResource(resourcesManager, productionType, resourceAmount);
    }

    // 특수자원을 지급하는 버튼을 그린다
    private void DrawSpecialButton(ResourcesManager resourcesManager)
    {
        if (!GUILayout.Button("특수자원", GUILayout.Height(GrantButtonHeight)))
        {
            return;
        }

        GrantSpecialResource(resourcesManager, resourceAmount);
    }

    // 자원 5종을 한 번에 지급하는 버튼을 그린다
    private void DrawGrantAllButton(ResourcesManager resourcesManager)
    {
        if (!GUILayout.Button("자원 전부 지급", GUILayout.Height(GrantButtonHeight)))
        {
            return;
        }

        GrantAllResources(resourcesManager, resourceAmount);
    }

    // 시민 추가 수량 칸과 버튼을 그린다
    private void DrawCitizenSection(CitizenManager citizenManager)
    {
        EditorGUILayout.LabelField("시민", EditorStyles.boldLabel);
        citizenAmount = EditorGUILayout.IntField("추가 수량", citizenAmount);

        if (!GUILayout.Button("시민 추가", GUILayout.Height(GrantButtonHeight)))
        {
            return;
        }

        GrantCitizen(citizenManager, citizenAmount);
    }

    // 자원 한 종류를 원하는 만큼 더한다
    private void GrantSingleResource(ResourcesManager resourcesManager, ProductionType productionType, int amount)
    {
        resourcesManager.ProductChanged(new[] { (productionType, amount) });
    }

    // 자원 5종을 같은 수량으로 한 번에 더한다
    private void GrantAllResources(ResourcesManager resourcesManager, int amount)
    {
        resourcesManager.ProductChanged(new[]
        {
            (ProductionType.Wood, amount),
            (ProductionType.Food, amount),
            (ProductionType.Gold, amount),
            (ProductionType.Iron, amount),
            (ProductionType.Stone, amount),
        });
    }

    // 특수자원만 원하는 만큼 더한다
    private void GrantSpecialResource(ResourcesManager resourcesManager, int amount)
    {
        resourcesManager.RestoreResources(
            resourcesManager.Wood,
            resourcesManager.Food,
            resourcesManager.Gold,
            resourcesManager.Iron,
            resourcesManager.Stone,
            resourcesManager.Special + amount);
    }

    // 시민을 원하는 만큼 더한다
    private void GrantCitizen(CitizenManager citizenManager, int amount)
    {
        citizenManager.IncreaseCitizen(amount);
    }
}
