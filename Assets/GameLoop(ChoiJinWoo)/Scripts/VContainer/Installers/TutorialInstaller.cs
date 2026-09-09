using VContainer;
using VContainer.Unity;

/// <summary>
/// 튜토리얼 관련 등록.
/// 진행 상태/트리거/입력 게이트/롤백(순수 C#)과 오버레이·매니저(씬 컴포넌트)를 담당한다.
/// </summary>
public sealed class TutorialInstaller : IInstaller
{
    public void Install(IContainerBuilder builder)
    {
        builder.Register<TutorialState>(Lifetime.Singleton).AsSelf();
        builder.Register<TutorialTriggers>(Lifetime.Singleton).AsSelf();
        builder.Register<TutorialGate>(Lifetime.Singleton).AsSelf();
        builder.Register<TutorialRollback>(Lifetime.Singleton).AsSelf();

        builder.RegisterComponentInHierarchy<TutorialOverlayUI>();
        builder.RegisterComponentInHierarchy<TutorialManager>();
    }
}
