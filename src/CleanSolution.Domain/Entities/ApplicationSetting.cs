using System;
using System.Collections.Generic;
using System.Text;

namespace CleanSolution.Domain.Entities
{
    public class ApplicationSetting : IAggregateRoot<Guid>
    {
        public Guid Id { get; set; }
        public string? Description { get; set; }
    }
}
