using CleanSolution.Application.Features.Settings.Commands;
using CleanSolution.Domain.Entities;
using Mapster;

namespace CleanSolution.Infrastructure.Configuration
{
    public class MapsterConfiguration : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<AddSettingCommand, ApplicationSetting>();
        }
    }
}
