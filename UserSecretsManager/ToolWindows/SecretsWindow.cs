﻿using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using UserSecretsManager.Views;

namespace UserSecretsManager.ToolWindows
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("0bb8aeb1-2bb5-460d-9543-c52f6b3278f4")]
    public class SecretsWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsWindow"/> class.
        /// </summary>
        public SecretsWindow() : base(null)
        {
            this.Caption = "User Secrets Manager";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new SecretsWindowControl();
        }
    }
}
