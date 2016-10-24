using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Development.TopicsOnGit.Services;
using Nop.Services.Localization;

namespace Nop.Plugin.Development.TopicsOnGit
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order => 1;

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<BackupService>().As<IBackupService>();
            builder.RegisterType<TrackableLocalizedEntityService>().As<ILocalizedEntityService>();
        }
    }
}
