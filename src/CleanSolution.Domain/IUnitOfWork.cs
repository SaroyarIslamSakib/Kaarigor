using System;
using System.Collections.Generic;
using System.Text;

namespace CleanSolution.Domain
{
    public interface IUnitOfWork
    {
        void Save();
        Task SaveAsync();
    }
}
