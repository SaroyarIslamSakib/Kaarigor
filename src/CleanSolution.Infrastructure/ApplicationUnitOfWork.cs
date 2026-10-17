using CleanSolution.Domain;
using CleanSolution.Domain.Repositories;
using CleanSolution.Infrastructure.Data;

namespace CleanSolution.Infrastructure
{
    public class ApplicationUnitOfWork : UnitOfWork, IApplicationUnitOfWork
    {
        public IApplicationSettingRepository SettingRepository { get; private set; }

        public ApplicationUnitOfWork(ApplicationDbContext context,
                        IApplicationSettingRepository settingRepository) : base(context)
        {
            SettingRepository = settingRepository;
        }
    }
}
