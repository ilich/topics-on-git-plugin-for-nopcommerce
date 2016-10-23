using System;
using System.IO;
using LibGit2Sharp;
using Nop.Core;
using Nop.Core.Domain.Topics;
using Nop.Services.Configuration;

namespace Nop.Plugin.Development.TopicsOnGit.Services
{
    public class BackupService : IBackupService
    {
        private readonly ISettingService _settingService;

        public BackupService(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public void Delete(Topic topic)
        {
            // Method does not require TopicsOnGitSettings as a parameter
            // because we're going to use current system settings

            if (topic == null)
            {
                return;
            }

            var settings = LoadSettings();
            var filename = GetFilename(settings, topic);
            if (!File.Exists(filename))
            {
                return;
            }

            var repo = new Repository(settings.Repository);
            Commands.Remove(repo, filename);
            var committer = new Signature(settings.Name, settings.Email, DateTime.Now);
            repo.Commit($"Topic {topic.SystemName} has been removed", committer, committer);
        }

        public void Save(Topic topic)
        {
            // Method does not require TopicsOnGitSettings as a parameter
            // because we're going to use current system settings

            if (topic == null)
            {
                return;
            }
        }

        public void Install(TopicsOnGitSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var path = settings.Repository;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                throw new NopException($"{path} already exists. Please choose another directory for topics backup.");
            }

            Repository.Init(path);
            var repo = new Repository(path);
            repo.Config.Set("user.name", settings.Name, ConfigurationLevel.Local);
            repo.Config.Set("user.email", settings.Email, ConfigurationLevel.Local);
        }

        public void Uninstall(TopicsOnGitSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var path = settings.Repository;
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var folder = new DirectoryInfo(path);
            if (!folder.Exists)
            {
                return;
            }

            DeleteFiles(folder);
            folder.Delete(true);
        }

        private void DeleteFiles(DirectoryInfo folder)
        {
            foreach(var subFolder in folder.GetDirectories())
            {
                DeleteFiles(subFolder);
            }

            foreach(var file in folder.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
                file.Delete();
            }
        }

        private TopicsOnGitSettings LoadSettings()
        {
            var settings = _settingService.LoadSetting<TopicsOnGitSettings>();
            if (settings == null || string.IsNullOrEmpty(settings.Repository))
            {
                throw new NopException("Git repository hasn't been setup yet");
            }

            if (!Directory.Exists(settings.Repository))
            {
                throw new NopException($"{settings.Repository} is not found");
            }

            return settings;
        }

        private string GetFilename(TopicsOnGitSettings settings, Topic topic)
        {
            var filename = $"{topic.SystemName}.sql";
            var path = Path.Combine(settings.Repository, filename);
            return path;
        }
    }
}
