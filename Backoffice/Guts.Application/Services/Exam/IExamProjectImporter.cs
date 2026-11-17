using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Dtos.Exam;

namespace Guts.Application.Services.Exam;

public interface IExamProjectImporter
{
    Task<IReadOnlyList<ImportedProjectFileDto>> ExtractFromZipAsync(Stream zipStream, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportedProjectFileDto>> CloneFromGitAsync(string repositoryUrl, string branchOrRef = null, CancellationToken cancellationToken = default);
}
