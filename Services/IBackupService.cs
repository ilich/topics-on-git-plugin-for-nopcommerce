namespace Nop.Plugin.Development.TopicsOnGit.Services
{
    public interface IBackupService
    {
        void Install(TopicsOnGitSettings settings);

        void Uninstall(TopicsOnGitSettings settings);
    }
}
