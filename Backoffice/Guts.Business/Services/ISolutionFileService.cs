using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Guts.Business.Dtos;

namespace Guts.Business.Services
{
    public interface ISolutionFileService
    {
        Task<byte[]> CreateZipFromFiles(IList<SolutionDto> solutions);
    }
}