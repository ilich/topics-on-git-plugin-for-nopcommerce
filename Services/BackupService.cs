using System;
using System.IO;
using System.Text;
using LibGit2Sharp;
using Nop.Core;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Topics;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Topics;

namespace Nop.Plugin.Development.TopicsOnGit.Services
{
    public class BackupService : IBackupService
    {
        private readonly ISettingService _settingService;

        private readonly ILocalizedEntityService _localizedEntityService;

        private readonly ILanguageService _languageService;

        private readonly ITopicService _topicService;

        public BackupService(
            ISettingService settingService,
            ILocalizedEntityService localizedEntityService,
            ILanguageService languageService,
            ITopicService topicService)
        {
            _settingService = settingService;
            _localizedEntityService = localizedEntityService;
            _languageService = languageService;
            _topicService = topicService;
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
            var status = repo.RetrieveStatus();
            if (status.IsDirty)
            {
                Commands.Stage(repo, filename);
                var committer = new Signature(settings.Name, settings.Email, DateTime.Now);
                repo.Commit($"Topic {topic.SystemName} has created/updated", committer, committer);
            }
        }

        public void BackupAllTopics()
        {
            var settings = LoadSettings();
            var topics = _topicService.GetAllTopics(0, false, true);
            foreach(var topic in topics)
            {
                var filename = GetFilename(settings, topic);
                var query = CreateQuery(topic);
                File.WriteAllText(filename, query);
            }

            var repo = new Repository(settings.Repository);
            var status = repo.RetrieveStatus();
            if (status.IsDirty)
            {
                Commands.Stage(repo, "*");
                var committer = new Signature(settings.Name, settings.Email, DateTime.Now);
                repo.Commit($"Backed up all topics", committer, committer);
            }
        }

        public void UpdateUserInfo(TopicsOnGitSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var repo = new Repository(settings.Repository);
            repo.Config.Set("user.name", settings.Name, ConfigurationLevel.Local);
            repo.Config.Set("user.email", settings.Email, ConfigurationLevel.Local);
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
            UpdateUserInfo(settings);
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
            query.AppendLine($"IF EXISTS(SELECT 1 FROM [dbo].[Topic] WHERE [SystemName] = '{topic.SystemName}') BEGIN");
            query.AppendLine(CreateUpdateQuery(topic));
            query.AppendLine(LocalizedPropertyUpdateQueries(topic));
            query.AppendLine("END");
            query.AppendLine("ELSE BEGIN");
            query.AppendLine(CreateInsertQuery(topic));
            query.AppendLine(LocalizedPropertyInsertQueries(topic));
            query.AppendLine("END");
            return query.ToString();
        }

        private string CreateUpdateQuery(Topic topic)
        {
            var finalQuery = new StringBuilder();

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

            finalQuery.Append(query);

            return finalQuery.ToString();
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

        private string LocalizedPropertyInsertQueries(Topic topic)
        {
            var query = new StringBuilder();
            query.AppendLine("DECLARE @topicId INT");
            query.AppendLine("SET @topicId = SCOPE_IDENTITY()");

            var languages = _languageService.GetAllLanguages();
            foreach (var language in languages)
            {
                var sql = LocalizedPropertyInsertQuery(topic, language, "Title");
                if (!string.IsNullOrEmpty(sql))
                {
                    query.Append(sql);
                }

                sql = LocalizedPropertyInsertQuery(topic, language, "Body");
                if (!string.IsNullOrEmpty(sql))
                {
                    query.Append(sql);
                }
            }

            return query.ToString();
        }

        private string LocalizedPropertyInsertQuery(Topic topic, Language language, string localeKey)
        {
            var value = _localizedEntityService.GetLocalizedValue(language.Id, topic.Id, "Topic", localeKey);
            if (value == null)
            {
                return null;
            }

            var sql = @"
                    INSERT INTO [dbo].[LocalizedProperty]
                           ([EntityId]
                           ,[LanguageId]
                           ,[LocaleKeyGroup]
                           ,[LocaleKey]
                           ,[LocaleValue])
                     VALUES
                           (@topicId
                           ,{0}
                           ,'Topic'
                           ,'{2}'
                           ,'{1}')
                ";
            var query = string.Format(sql, language.Id, Quotes(value), Quotes(localeKey));
            return query;
        }

        private string LocalizedPropertyUpdateQueries(Topic topic)
        {
            var query = new StringBuilder();

            var languages = _languageService.GetAllLanguages();
            foreach(var language in languages)
            {
                var sql = LocalizedPropertyUpdateQuery(topic, language, "Title");
                if (!string.IsNullOrEmpty(sql))
                {
                    query.Append(sql);
                }

                sql = LocalizedPropertyUpdateQuery(topic, language, "Body");
                if (!string.IsNullOrEmpty(sql))
                {
                    query.Append(sql);
                }
            }

            return query.ToString();
        }

        private string LocalizedPropertyUpdateQuery(Topic topic, Language language, string localeKey)
        {
            var query = new StringBuilder();

            var value = _localizedEntityService.GetLocalizedValue(language.Id, topic.Id, "Topic", localeKey);
            if (value == null)
            {
                return null;
            }

            query.AppendLine($"IF EXISTS(SELECT 1 FROM [dbo].[LocalizedProperty] WHERE EntityId = {topic.Id} AND LanguageId = {language.Id} AND LocaleKeyGroup = 'Topic' AND LocaleKey = '{localeKey}')");
            query.AppendLine($"\tUPDATE [dbo].[LocalizedProperty] SET LocaleValue = '{Quotes(value)}' WHERE EntityId = {topic.Id} AND LanguageId = {language.Id} AND LocaleKeyGroup = 'Topic' AND LocaleKey = '{localeKey}'");
            query.AppendLine("ELSE");
            var sql = @"
                    INSERT INTO [dbo].[LocalizedProperty]
                           ([EntityId]
                           ,[LanguageId]
                           ,[LocaleKeyGroup]
                           ,[LocaleKey]
                           ,[LocaleValue])
                     VALUES
                           ({0}
                           ,{1}
                           ,'Topic'
                           ,'{3}'
                           ,'{2}')
                ";
            query.AppendLine(string.Format(sql, topic.Id, language.Id, Quotes(value), Quotes(localeKey)));

            return query.ToString();
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
