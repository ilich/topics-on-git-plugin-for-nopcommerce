using Nop.Core.Domain.Topics;

namespace Nop.Plugin.Development.TopicsOnGit.Services
{
    public interface IBackupService
    {
        void Install(TopicsOnGitSettings settings);

        void Uninstall(TopicsOnGitSettings settings);

        void Delete(Topic topic);

        void Save(Topic topic);
    }
}
