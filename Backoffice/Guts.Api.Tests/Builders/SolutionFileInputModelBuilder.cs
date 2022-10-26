using System;
using Guts.Api.Extensions;
using Guts.Api.Models;

namespace Guts.Api.Tests.Builders;

internal class SolutionFileInputModelBuilder
{
    private readonly SolutionFileInputModel _inputModel;

    public SolutionFileInputModelBuilder()
    {
        _inputModel = new SolutionFileInputModel
        {
            Content = Guid.NewGuid().ToString().TryFromBase64(),
            FilePath = Guid.NewGuid().ToString()
        };
    }

    public SolutionFileInputModel Build()
    {
        return _inputModel;
    }
}