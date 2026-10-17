using CleanSolution.Domain.Entities;
using CleanSolution.Domain.Repositories;
using CleanSolution.Infrastructure.Data;

namespace CleanSolution.Infrastructure.Repositories
{
    public class ApplicationSettingRepository : Repository<ApplicationSetting, Guid>,IApplicationSettingRepository
    {
        public ApplicationSettingRepository(ApplicationDbContext context) : base(context)
        {

        }
    }
}
