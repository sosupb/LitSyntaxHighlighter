global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Threading;
using System.Runtime.InteropServices;
using System.Threading;
using static LitSyntaxHighlighter.OptionsProvider;

namespace LitSyntaxHighlighter
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.LitSyntaxHighlighterString)]
    [ProvideOptionPage(typeof(OptionsProvider.GeneralOptionPage), "Lit Syntax Highlighter", "General", 0, 0, true, SupportsProfiles = true)]
    public sealed class LitSyntaxHighlighterPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }
}
