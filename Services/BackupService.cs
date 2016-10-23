using System;
using System.IO;
using System.Text;
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

            var settings = LoadSettings();
            var filename = GetFilename(settings, topic);
            var query = CreateQuery(topic);
            File.WriteAllText(filename, query);

            var repo = new Repository(settings.Repository);
            Commands.Stage(repo, filename);
            var committer = new Signature(settings.Name, settings.Email, DateTime.Now);
            repo.Commit($"Topic {topic.SystemName} has created/updated", committer, committer);
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

        private string CreateQuery(Topic topic)
        {
            var query = new StringBuilder();
            query.AppendLine($"IF EXISTS(SELECT 1 FROM [dbo].[Topic] WHERE [SystemName] = '{topic.SystemName}')");
            query.AppendLine(CreateUpdateQuery(topic));
            query.AppendLine("ELSE");
            query.AppendLine(CreateInsertQuery(topic));
            return query.ToString();
        }

        private string CreateUpdateQuery(Topic topic)
        {
            var sql = @"
                UPDATE [dbo].[Topic]
                   SET [IncludeInSitemap] = {1}
                      ,[IncludeInTopMenu] = {2}
                      ,[IncludeInFooterColumn1] = {3}
                      ,[IncludeInFooterColumn2] = {4}
                      ,[IncludeInFooterColumn3] = {5}
                      ,[DisplayOrder] = {6}
                      ,[AccessibleWhenStoreClosed] = {7}
                      ,[IsPasswordProtected] = {8}
                      ,[Password] = '{9}'
                      ,[Title] = '{10}'
                      ,[Body] = '{11}'
                      ,[Published] = {12}
                      ,[TopicTemplateId] = {13}
                      ,[MetaKeywords] = '{14}'
                      ,[MetaDescription] = '{15}'
                      ,[MetaTitle] = '{16}'
                      ,[SubjectToAcl] = {17}
                      ,[LimitedToStores] = {18}
                 WHERE [SystemName] = '{0}'
            ";

            var query = string.Format(sql,
                Quotes(topic.SystemName),
                Bit(topic.IncludeInSitemap),
                Bit(topic.IncludeInTopMenu),
                Bit(topic.IncludeInFooterColumn1),
                Bit(topic.IncludeInFooterColumn2),
                Bit(topic.IncludeInFooterColumn3),
                topic.DisplayOrder,
                Bit(topic.AccessibleWhenStoreClosed),
                Bit(topic.IsPasswordProtected),
                Quotes(topic.Password),
                Quotes(topic.Title),
                Quotes(topic.Body),
                Bit(topic.Published),
                topic.TopicTemplateId,
                Quotes(topic.MetaKeywords),
                Quotes(topic.MetaDescription),
                Quotes(topic.MetaTitle),
                Bit(topic.SubjectToAcl),
                Bit(topic.LimitedToStores));

            return query;
        }

        private string CreateInsertQuery(Topic topic)
        {
            var sql =  @"
                INSERT INTO [dbo].[Topic]
                           ([SystemName]
                           ,[IncludeInSitemap]
                           ,[IncludeInTopMenu]
                           ,[IncludeInFooterColumn1]
                           ,[IncludeInFooterColumn2]
                           ,[IncludeInFooterColumn3]
                           ,[DisplayOrder]
                           ,[AccessibleWhenStoreClosed]
                           ,[IsPasswordProtected]
                           ,[Password]
                           ,[Title]
                           ,[Body]
                           ,[Published]
                           ,[TopicTemplateId]
                           ,[MetaKeywords]
                           ,[MetaDescription]
                           ,[MetaTitle]
                           ,[SubjectToAcl]
                           ,[LimitedToStores])
                     VALUES
                           ('{0}'
                           ,{1}
                           ,{2}
                           ,{3}
                           ,{4}
                           ,{5}
                           ,{6}
                           ,{7}
                           ,{8}
                           ,'{9}'
                           ,'{10}'
                           ,'{11}'
                           ,{12}
                           ,{13}
                           ,'{14}'
                           ,'{15}'
                           ,'{16}'
                           ,{17}
                           ,{18})
            ";

            var query = string.Format(sql,
                Quotes(topic.SystemName),
                Bit(topic.IncludeInSitemap),
                Bit(topic.IncludeInTopMenu),
                Bit(topic.IncludeInFooterColumn1),
                Bit(topic.IncludeInFooterColumn2),
                Bit(topic.IncludeInFooterColumn3),
                topic.DisplayOrder,
                Bit(topic.AccessibleWhenStoreClosed),
                Bit(topic.IsPasswordProtected),
                Quotes(topic.Password),
                Quotes(topic.Title),
                Quotes(topic.Body),
                Bit(topic.Published),
                topic.TopicTemplateId,
                Quotes(topic.MetaKeywords),
                Quotes(topic.MetaDescription),
                Quotes(topic.MetaTitle),
                Bit(topic.SubjectToAcl),
                Bit(topic.LimitedToStores));

            return query;
        }

        private int Bit(bool value)
        {
            return value ? 1 : 0;
        }

        private string Quotes(string str)
        {
            str = str?.Replace("'", "''");
            return str == null ? string.Empty : str;
        }
    }
}
