using System;
using System.IO;
using LibGit2Sharp;
using Nop.Core;

namespace Nop.Plugin.Development.TopicsOnGit.Services
{
    public class BackupService : IBackupService
    {
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
    }
}
