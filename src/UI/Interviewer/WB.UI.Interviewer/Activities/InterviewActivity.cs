using Android.App;
using Android.Views;
using Java.Interop;
using MvvmCross.Commands;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.UI.Interviewer.ViewModel;
using WB.UI.Shared.Enumerator.Activities;

namespace WB.UI.Interviewer.Activities
{
    [Activity(Label = "",
        Theme = "@style/BlueAppTheme",
        HardwareAccelerated = true,
        WindowSoftInputMode = SoftInput.StateAlwaysHidden | SoftInput.AdjustPan,
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize,
        Exported = false)]
    public class InterviewActivity : BaseInterviewActivity<InterviewViewModel>
    {
        protected override int LanguagesMenuGroupId => Resource.Id.interview_languages;
        protected override int OriginalLanguageMenuItemId => Resource.Id.interview_language_original;
        protected override int LanguagesMenuItemId => Resource.Id.interview_language;
        protected override int MenuId => Resource.Menu.interview;

        protected override MenuDescription MenuDescriptor => new MenuDescription
        {
            {
                Resource.Id.menu_return,
                EnumeratorUIResources.MenuItem_Title_Return,
                new MvxAsyncCommand(async () =>
                {
                    await this.ViewModel.NavigateFromInterviewAsync.ExecuteAsync();
                    this.ReleaseActivity();
                })
            },
            {
                Resource.Id.menu_dashboard,
                EnumeratorUIResources.MenuItem_Title_Dashboard,
                new MvxAsyncCommand(async () =>
                {
                    await this.ViewModel.NavigateToDashboardInterviewAsync.ExecuteAsync();
                    this.ReleaseActivity();
                })
            },
            {
                Resource.Id.menu_signout,
                EnumeratorUIResources.MenuItem_Title_SignOut,
                this.ViewModel.SignOutCommand
            },
            {
                Resource.Id.menu_diagnostics,
                EnumeratorUIResources.MenuItem_Title_Diagnostics,
                this.ViewModel.NavigateToDiagnosticsPageCommand
            },
            {
                Resource.Id.menu_maps,
                EnumeratorUIResources.MenuItem_Title_Maps,
                this.ViewModel.NavigateToMapsCommand
            },
            {
                Resource.Id.interview_language,
                EnumeratorUIResources.MenuItem_Title_Language
            },
            {
                Resource.Id.interview_language_original,
                this.ViewModel.DefaultLanguageName ?? EnumeratorUIResources.MenuItem_Title_Language_Original
            },
        };

        [Export("NavigateToApi")]
        public void NavigateToApi(string navigateTo)
        {
            base.Navigate(navigateTo);
        }
    }
}
