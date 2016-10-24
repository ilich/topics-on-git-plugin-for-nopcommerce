using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Services.Topics;

namespace Nop.Plugin.Development.TopicsOnGit.Services
{
    public class TrackableLocalizedEntityService : LocalizedEntityService
    {
        public TrackableLocalizedEntityService(ICacheManager cacheManager,
            IRepository<LocalizedProperty> localizedPropertyRepository,
            LocalizationSettings localizationSettings)
            : base(cacheManager, localizedPropertyRepository, localizationSettings)
        {
        }

        public override void InsertLocalizedProperty(LocalizedProperty localizedProperty)
        {
            base.InsertLocalizedProperty(localizedProperty);
            UpdateTopicOnGit(localizedProperty);
        }

        public override void UpdateLocalizedProperty(LocalizedProperty localizedProperty)
        {
            base.UpdateLocalizedProperty(localizedProperty);
            UpdateTopicOnGit(localizedProperty);
        }

        private void UpdateTopicOnGit(LocalizedProperty localizedProperty)
        {
            if (localizedProperty == null || localizedProperty.LocaleKeyGroup != "Topic")
            {
                return;
            }

            var topicService = EngineContext.Current.Resolve<ITopicService>();
            var backupService = EngineContext.Current.Resolve<IBackupService>();
            var topic = topicService.GetTopicById(localizedProperty.EntityId);
            if (topic != null)
            {
                backupService.Save(topic);
            }
        }
    }
}
