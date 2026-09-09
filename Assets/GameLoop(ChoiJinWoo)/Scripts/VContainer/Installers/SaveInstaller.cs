using VContainer;
using VContainer.Unity;

/// <summary>
/// 세이브/로드 관련 등록.
/// 캡처/복원/IO/암호화 등 세부 컴포넌트와, 엔트리포인트(IStartable/ITickable)로 도는
/// 매니저들을 담당한다.
/// </summary>
public sealed class SaveInstaller : IInstaller
{
    public void Install(IContainerBuilder builder)
    {
        builder.Register<SaveCheck>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveIO>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveSlot>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveCapture>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveRestore>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveTimeData>(Lifetime.Singleton).As<ITickable>().AsSelf();
        builder.Register<SaveManager>(Lifetime.Singleton).As<IStartable>().AsSelf();
        builder.Register<LoadManager>(Lifetime.Singleton).As<IStartable>().AsSelf();
        builder.Register<SaveExitHook>(Lifetime.Singleton).As<IStartable>().AsSelf();
        builder.Register<SaveChangeTracker>(Lifetime.Singleton).As<IStartable>().As<ITickable>().AsSelf();
        builder.Register<SaveKey>(Lifetime.Singleton).AsSelf();
        builder.Register<SaveCipher>(Lifetime.Singleton).AsSelf();
        builder.Register<DayNightData>(Lifetime.Singleton).AsSelf();
    }
}
