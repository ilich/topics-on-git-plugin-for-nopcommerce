using System.Web.Mvc;
using Nop.Plugin.Development.TopicsOnGit.Models;
using Nop.Plugin.Development.TopicsOnGit.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Development.TopicsOnGit.Controllers
{
    [AdminAuthorize]
    public class TopicsOnGitController : BasePluginController
    {
        private readonly ISettingService _settingService;

        private readonly ILocalizationService _localizationService;

        private readonly IBackupService _backupService;

        private readonly TopicsOnGitSettings _pluginSettings;

        public TopicsOnGitController(
            ISettingService settingService,
            ILocalizationService localizationService,
            TopicsOnGitSettings pluginSettings,
            IBackupService backupService)
        {
            _settingService = settingService;
            _pluginSettings = pluginSettings;
            _localizationService = localizationService;
            _backupService = backupService;
        }

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new TopicsOnGitSettingsModel
            {
                Repository = _pluginSettings.Repository,
                Name = _pluginSettings.Name,
                Email = _pluginSettings.Email
            };

            return View("~/Plugins/Devemopment.TopicsOnGit/Views/TopicsOnGit/Configure.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ChildActionOnly]
        [FormValueRequired("save")]
        public ActionResult Configure(TopicsOnGitSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Plugins/Devemopment.TopicsOnGit/Views/TopicsOnGit/Configure.cshtml", model);
            }

            _pluginSettings.Repository = model.Repository;
            _pluginSettings.Name = model.Name;
            _pluginSettings.Email = model.Email;
            _settingService.SaveSetting(_pluginSettings);

            _backupService.UpdateUserInfo(_pluginSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return View("~/Plugins/Devemopment.TopicsOnGit/Views/TopicsOnGit/Configure.cshtml", model);
        }

        [HttpPost]
        [ActionName("Configure")]
        [ValidateAntiForgeryToken]
        [ChildActionOnly]
        [FormValueRequired("backup")]
        public ActionResult Backup(TopicsOnGitSettingsModel model)
        {
            _backupService.BackupAllTopics();
            SuccessNotification(_localizationService.GetResource("Nop.Plugin.Development.TopicsOnGit.Backup.Saved"));

            return Configure();
        }
    }
}
