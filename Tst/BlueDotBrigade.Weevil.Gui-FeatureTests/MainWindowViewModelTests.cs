namespace BlueDotBrigade.Weevil.Gui
{
    using System.Linq;
    using System.Reflection;
    using BlueDotBrigade.Weevil.Gui.Threading;

    [TestClass]
    public class MainWindowViewModelTests : UiTestBase
    {
        [TestMethod]
        public void GivenMainWindowViewModel_WhenConstructed_ThenApplicationTitleUsesConfiguredReleaseVersion()
        {
            var expectedVersion = typeof(MainWindowViewModel)
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion
                .Split('+')
                .First();

            var viewModel = new MainWindowViewModel(
                new UiDispatcherFake(),
                Substitute.For<IBulletinMediator>());

            viewModel.ApplicationTitle.Should().Be($"Weevil: v{expectedVersion}");
        }
    }
}
