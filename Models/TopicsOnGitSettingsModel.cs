using FluentValidation.Attributes;
using Nop.Plugin.Development.TopicsOnGit.Validators;
using Nop.Web.Framework;

namespace Nop.Plugin.Development.TopicsOnGit.Models
{
    [Validator(typeof(TopicsOnGitSettingsValidator))]
    public class TopicsOnGitSettingsModel
    {
        [NopResourceDisplayName("Nop.Plugin.Development.TopicsOnGit.Repository")]
        public string Repository { get; set; }

        [NopResourceDisplayName("Nop.Plugin.Development.TopicsOnGit.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Nop.Plugin.Development.TopicsOnGit.Email")]
        public string Email { get; set; }
    }
}
