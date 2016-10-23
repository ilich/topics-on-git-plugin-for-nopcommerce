using System;
using System.Web;
using Nop.Core;
using Nop.Core.Plugins;
using Nop.Plugin.Development.TopicsOnGit.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;

namespace Nop.Plugin.Development.TopicsOnGit
{
    public class TopicsOnGitPlugin : BasePlugin, IMiscPlugin
    {
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
            throw new NotImplementedException();
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

            try
            {
                _backupService.Install(settings);
                _settingService.SaveSetting(settings);
            }
            catch
            {
                _backupService.Uninstall(settings);
                throw;
            }

            // TODO Create localized messages

            base.Install();
        }

        public override void Uninstall()
        {
            var settings = _settingService.LoadSetting<TopicsOnGitSettings>();
            _backupService.Uninstall(settings);
            _settingService.DeleteSetting<TopicsOnGitSettings>();

            // TODO Remove localized messages

            base.Uninstall();
        }
    }
}
