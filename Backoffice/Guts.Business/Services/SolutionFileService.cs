using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Guts.Business.Dtos;

namespace Guts.Business.Services
{
    public class SolutionFileService : ISolutionFileService
    {
        public async Task<byte[]> CreateZipFromFiles(IReadOnlyList<SolutionDto> solutions)
        {
            await using var memoryStream = new MemoryStream();

            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var solution in solutions)
                {
                    foreach (var solutionFile in solution.SolutionFiles)
                    {
                        var entry = zipArchive.CreateEntry($@"{solution.WriterName}\{solutionFile.FilePath}");
                        await using StreamWriter writer = new StreamWriter(entry.Open());
                        await writer.WriteAsync(solutionFile.Content);
                    }
                }
            }

            memoryStream.Position = 0;
            return memoryStream.ToArray();
        }
    }
}