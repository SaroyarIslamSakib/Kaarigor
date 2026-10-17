using Cortex.Mediator.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanSolution.Application.Features.Settings.Commands
{
    public class AddSettingCommand : ICommand<Guid>
    {
        public string? Description { get; set; }
    }
}
