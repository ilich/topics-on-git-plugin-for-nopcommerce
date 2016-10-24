using System;
using System.Web;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Topics;
using Nop.Core.Events;
using Nop.Core.Plugins;
using Nop.Plugin.Development.TopicsOnGit.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Localization;

namespace Nop.Plugin.Development.TopicsOnGit
{
    public class TopicsOnGitPlugin : BasePlugin, IMiscPlugin, IConsumer<EntityUpdated<Topic>>,
        IConsumer<EntityDeleted<Topic>>, IConsumer<EntityInserted<Topic>>
    {
        private const string Requirements = @"
Devemopment.TopicsOnGit plugin uses <a href='https://github.com/libgit2/libgit2sharp' target='_blank'>LibGit2Sharp</a> library.
It reuires <a href='https://libgit2.github.com/' target='_blank'>libgit2</a> native library available on your server. 
Make sure you have added <strong>~\Plugins\Devemopment.TopicsOnGit\lib\win32\x64\git2-baa87df.dll</strong>
and <strong>~\Plugins\Devemopment.TopicsOnGit\lib\win32\x86\git2-baa87df.dll</strong> to your <strong>PATH</strong> system variable.
";
        private const string DefaultRepository = "~/App_Data/TopicsBackup";

        private readonly ISettingService _settingService;

        private readonly IWorkContext _workContext;

        private readonly IBackupService _backupService;

        public TopicsOnGitPlugin(
            ISettingService settingService,
            IWorkContext workContext,
            IBackupService backupService)
        {
            _settingService = settingService;
            _workContext = workContext;
            _backupService = backupService;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out System.Web.Routing.RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "TopicsOnGit";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Development.TopicsOnGit.Controllers" }, { "area", null } };
        }

        public override void Install()
        {
            var repository = HttpContext.Current.Server.MapPath(DefaultRepository);
            var email = _workContext.CurrentCustomer.Email;
            var username = _workContext.CurrentCustomer.Username;
            var settings = new TopicsOnGitSettings
            {
                Name = string.IsNullOrEmpty(username) ? email : username,
                Email = email,
                Repository = repository
            };

            _backupService.Install(settings);
            _settingService.SaveSetting(settings);

            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Requirements", Requirements);
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Repository", "Repository");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Repository.Hint", "Enter path to your Git repository.");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Repository.Required", "Repository is required.");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Repository.Info", "Make sure your repository existis and <a href='https://git-scm.com/docs/git-init' target='_blank'>initialized</a>. ");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Name", "Username");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Name.Hint", "Enter your Git username.");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Name.Required", "Username is required.");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Email", "Email");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Email.Hint", "Enter your Git email.");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Email.Required", "Email is required.");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Backup", "Backup all topics");
            this.AddOrUpdatePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Backup.Saved", "The topics has been backed up successfully.");

            base.Install();
        }

        public override void Uninstall()
        {
            var settings = _settingService.LoadSetting<TopicsOnGitSettings>();
            _backupService.Uninstall(settings);
            _settingService.DeleteSetting<TopicsOnGitSettings>();

            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Requirements");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Repository");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Repository.Hint");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Repository.Required");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Repository.Info");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Name");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Name.Hint");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Name.Required");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Email");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Email.Hint");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Email.Required");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Backup");
            this.DeletePluginLocaleResource("Nop.Plugin.Development.TopicsOnGit.Backup.Saved");

            base.Uninstall();
        }

        public void HandleEvent(EntityUpdated<Topic> eventMessage)
        {
            _backupService.Save(eventMessage.Entity);
        }

        public void HandleEvent(EntityDeleted<Topic> eventMessage)
        {
            _backupService.Delete(eventMessage.Entity);
        }

        public void HandleEvent(EntityInserted<Topic> eventMessage)
        {
            _backupService.Save(eventMessage.Entity);
        }
    }
}
