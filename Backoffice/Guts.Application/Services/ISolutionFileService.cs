using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;

namespace Guts.Application.Services;

public interface ISolutionFileService
{
    Task<byte[]> CreateZipFromFiles(IReadOnlyList<SolutionDto> solutions);
}