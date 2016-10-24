using FluentValidation;
using Nop.Plugin.Development.TopicsOnGit.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Development.TopicsOnGit.Validators
{
    public class TopicsOnGitSettingsValidator : BaseNopValidator<TopicsOnGitSettingsModel>
    {
        public TopicsOnGitSettingsValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Repository).NotEmpty().WithMessage(localizationService.GetResource("Nop.Plugin.Development.TopicsOnGit.Repository.Required"));
            RuleFor(x => x.Name).NotEmpty().WithMessage(localizationService.GetResource("Nop.Plugin.Development.TopicsOnGit.Name.Required"));
            RuleFor(x => x.Email).NotEmpty().WithMessage(localizationService.GetResource("Nop.Plugin.Development.TopicsOnGit.Email.Required"));
        }
    }
}
