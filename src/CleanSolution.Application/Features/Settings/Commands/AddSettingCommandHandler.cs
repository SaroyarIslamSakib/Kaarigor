using CleanSolution.Domain;
using CleanSolution.Domain.Entities;
using CleanSolution.Domain.Utilities;
using Cortex.Mediator.Commands;
using MapsterMapper;

namespace CleanSolution.Application.Features.Settings.Commands
{
    public class AddSettingCommandHandler : ICommandHandler<AddSettingCommand, Guid>
    {
        private readonly IApplicationUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public AddSettingCommandHandler(IApplicationUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<Guid> Handle(AddSettingCommand command, CancellationToken cancellationToken)
        {
            var setting = _mapper.Map<ApplicationSetting>(command);
            setting.Id = IdentityGenerator.NewSequentialGuid();
            await _unitOfWork.SettingRepository.AddAsync(setting);
            await _unitOfWork.SaveAsync();

            return setting.Id;
        }
    }
}
