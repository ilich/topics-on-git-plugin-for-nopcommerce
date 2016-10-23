using Nop.Core.Configuration;

namespace Nop.Plugin.Development.TopicsOnGit
{
    public class TopicsOnGitSettings : ISettings
    {
        public string Repository { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }
    }
}
