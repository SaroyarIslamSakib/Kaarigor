using System;
using System.Collections.Generic;
using System.Text;

namespace CleanSolution.Domain.Entities
{
    public interface IAggregateRoot<TKey>
    {
        TKey Id { get; set; } 
    }
}
