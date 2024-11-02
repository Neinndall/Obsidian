using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Obsidian.Data;
using Obsidian.Shared.Dialogs;
using Obsidian.Utils;
using Octokit;
using Semver;
using Serilog;

namespace Obsidian.Shared;

public partial class MainLayout {
    [Inject]
    public Config Config { get; set; }

    [Inject]
    public IDialogService DialogService { get; set; }

    [Inject]
    public IJSRuntime Js { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; }

    public AppTheme Theme { get; } = new();

    public string UpdateUrl { get; set; }

    private bool _isLoadingHashtable;
    private bool _isReady;

    private void OnHashtableLoadingStart() => this._isLoadingHashtable = true;

    private async Task OnHashtableLoadingFinished() {
        this._isLoadingHashtable = false;

        if (
            string.IsNullOrEmpty(this.Config.GameDataDirectory)
            && !this.Config.DoNotRequireGameDirectory
        )
            await this.DialogService.Show<StartupDialog>().Result;

        this._isReady = true;
    }

    private async Task GoToNewRelease() =>
        await this.Js.InvokeVoidAsync("useCmd", @$"explorer ""{this.UpdateUrl}""");

    private async Task CheckForUpdate() {
        Log.Information("Checking for new update");
        try {
            GitHubClient gitClient = new(new ProductHeaderValue("Obsidian"));
            IReadOnlyList<Release> releases = await gitClient.Repository.Release.GetAll(
                "Crauzer",
                "Obsidian"
            );
            Release newestRelease = releases[0];

            SemVersion latestReleaseSemver = SemVersion.Parse(
                newestRelease.TagName,
                SemVersionStyles.Strict
            );

            if (
                latestReleaseSemver.IsPrerelease is false
                && latestReleaseSemver.ComparePrecedenceTo(Program.VERSION) > 0
            ) {
                Log.Information($"Found new update: {latestReleaseSemver}");
                this.UpdateUrl = newestRelease.HtmlUrl;
            }
        } catch (Exception exception) {
            SnackbarUtils.ShowSoftError(this.Snackbar, exception);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender)
            await CheckForUpdate();

        await base.OnAfterRenderAsync(firstRender);
    }
}