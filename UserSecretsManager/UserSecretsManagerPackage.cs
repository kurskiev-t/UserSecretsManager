using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using UserSecretsManager.ToolWindows;
using UserSecretsManager.ViewModels;
using Task = System.Threading.Tasks.Task;

namespace UserSecretsManager
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(UserSecretsManagerPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(UserSecretsManager.ToolWindows.SecretsWindow))]
    public sealed class UserSecretsManagerPackage : AsyncPackage
    {
         /// <summary>
        /// UserSecretsManagerPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "3f9d20a1-ec19-4486-9538-f2ca17950b28";

        private IServiceProvider? _serviceProvider = null!;

        public static UserSecretsManagerPackage Instance { get; private set; } = null!;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // Сохраняем экземпляр пакета
            Instance = this;

            // Переключаемся на главный поток для получения сервисов VS
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Получаем IVsSolution
            if (await GetServiceAsync(typeof(SVsSolution)) is not IVsSolution vsSolution)
            {
                throw new InvalidOperationException("Не удалось получить IVsSolution.");
            }

            // Настройка DI
            ServiceCollection services = new();
            ConfigureServices(services, vsSolution);
            _serviceProvider = services.BuildServiceProvider();
            await UserSecretsManager.ToolWindows.SecretsWindowCommand.InitializeAsync(this);

            // Инициализация команды с DI
            //await SecretsWindowCommand.InitializeAsync(this, _serviceProvider);
        }

        private void ConfigureServices(ServiceCollection services, IVsSolution vsSolution)
        {
            // Регистрируем IVsSolution как Singleton
            services.AddSingleton(vsSolution);

            // Регистрируем ViewModel
            services.AddTransient<SecretsViewModel>();

            // Если позже добавишь сервис для работы с секретами, например ISecretsService
            // services.AddSingleton<ISecretsService, SecretsService>();
        }

        protected override object GetService(Type serviceType)
        {
            // Перехватываем запросы сервисов через DI
            return _serviceProvider?.GetService(serviceType) ?? base.GetService(serviceType);
        }

        #endregion
    }
}
