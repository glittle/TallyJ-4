using Backend.Domain.Entities;
using Backend.DTOs.Computers;
using Mapster;

namespace Backend.Mappings;

/// <summary>
/// Mapster profile for computer entity and DTO mappings.
/// </summary>
public class ComputerProfile : IRegister
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComputerProfile"/> class.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Computer, ComputerDto>();

        config.NewConfig<RegisterComputerDto, Computer>();

        config.NewConfig<UpdateComputerDto, Computer>();
    }
}
