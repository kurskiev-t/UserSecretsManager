﻿using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
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
    [ProvideToolWindow(typeof(UserSecretsManager.ToolWindows.SecretsWindow), Width = 400, Height = 300, Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Right, Transient = false)]
    public sealed class UserSecretsManagerPackage : AsyncPackage
    {
         /// <summary>
        /// UserSecretsManagerPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "3f9d20a1-ec19-4486-9538-f2ca17950b28";

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
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Проверка версии через VS.Shell
            var vsVersion = await VS.Shell.GetVsVersionAsync();
            if (vsVersion == null || vsVersion.Major < 17 || (vsVersion.Major == 17 && vsVersion.Minor < 13))
            {
                await VS.MessageBox.ShowAsync(
                    "User Secrets Manager",
                    $"This extension requires Visual Studio 2022 version 17.13 or higher. Current version: {vsVersion}",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK);
            }

            await UserSecretsManager.ToolWindows.SecretsWindowCommand.InitializeAsync(this);
        }

        #endregion
    }
}
