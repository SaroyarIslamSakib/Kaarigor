using CleanSolution.Domain.Repositories;

namespace CleanSolution.Domain
{
    public interface IApplicationUnitOfWork : IUnitOfWork
    {
        public IApplicationSettingRepository SettingRepository { get; }
    }
}
