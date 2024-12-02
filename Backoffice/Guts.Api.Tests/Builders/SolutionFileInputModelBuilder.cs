using System;
using Guts.Api.Extensions;
using Guts.Api.Models;
using Guts.Common.Extensions;

namespace Guts.Api.Tests.Builders;

internal class SolutionFileInputModelBuilder
{
    private readonly SolutionFileInputModel _inputModel;

    public SolutionFileInputModelBuilder()
    {
        _inputModel = new SolutionFileInputModel
        {
            Content = Guid.NewGuid().ToString().TryConvertFromBase64(),
            FilePath = $"{Guid.NewGuid()}.cs"
        };
    }

    public SolutionFileInputModel Build()
    {
        return _inputModel;
    }
}