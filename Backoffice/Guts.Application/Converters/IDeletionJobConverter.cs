using Guts.Application.Dtos;
using Guts.Domain.DeletionJobAggregate;

namespace Guts.Application.Converters;

public interface IDeletionJobConverter
{
    DeletionJobDto ToDto(DeletionJob job);
}
