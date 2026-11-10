using System;
using System.Linq;
using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Guts.Api.Controllers;
using Guts.Bootstrapper;
using Guts.Application.Services;
using Guts.Common;
using Guts.Domain.CourseAggregate;
using Microsoft.AspNetCore.Authorization;
using NUnit.Framework;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Guts.Architecture.Tests;

[TestFixture]
public class ArchitectureTests
{
    private static readonly System.Reflection.Assembly CommonAssembly = typeof(Contracts).Assembly;
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Course).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(ICourseService).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = System.Reflection.Assembly.Load("Guts.Infrastructure");
    private static readonly System.Reflection.Assembly BootstrapperAssembly = typeof(StartUpExtensions).Assembly;
    private static readonly System.Reflection.Assembly ApiAssembly = typeof(CourseController).Assembly;

    private static readonly ArchUnitNET.Domain.Architecture GutsArchitecture = new ArchLoader()
        .LoadAssemblies(
            CommonAssembly,
            DomainAssembly,
            ApplicationAssembly,
            InfrastructureAssembly,
            BootstrapperAssembly,
            ApiAssembly)
        .Build();

    private static readonly IObjectProvider<IType> CommonLayer =
        Types().That().ResideInAssembly(CommonAssembly).As("Guts.Common Layer");

    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly(DomainAssembly).As("Guts.Domain Layer");

    private static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly(ApplicationAssembly).As("Guts.Application Layer");

    private static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInAssembly(InfrastructureAssembly).As("Guts.Infrastructure Layer");

    private static readonly IObjectProvider<IType> BootstrapperLayer =
        Types().That().ResideInAssembly(BootstrapperAssembly).As("Guts.Bootstrapper Layer");

    private static readonly IObjectProvider<IType> ApiLayer =
        Types().That().ResideInAssembly(ApiAssembly).As("Guts.Api Layer");

    [Test]
    public void CommonLayer_ShouldNotDependOnAnyOtherGutsLayer()
    {
        IArchRule rule = Types().That().Are(CommonLayer)
            .Should().NotDependOnAny(DomainLayer)
            .AndShould().NotDependOnAny(ApplicationLayer)
            .AndShould().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(BootstrapperLayer)
            .AndShould().NotDependOnAny(ApiLayer)
            .Because("Guts.Common is the lowest foundational layer.");

        AssertNoViolations(rule);
    }

    [Test]
    public void DomainLayer_ShouldOnlyDependOnCommonAndNeverOnOuterLayersOrEfCore()
    {
        IArchRule layerRule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(ApplicationLayer)
            .AndShould().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(BootstrapperLayer)
            .AndShould().NotDependOnAny(ApiLayer)
            .Because("Guts.Domain must remain a pure domain model independent of application, persistence, and API concerns.");

        AssertNoViolations(layerRule);

        var efCoreReferences = DomainAssembly.GetReferencedAssemblies()
            .Where(a => a.Name != null && a.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Name)
            .ToList();

        Assert.That(efCoreReferences, Is.Empty, "Guts.Domain must not reference Microsoft.EntityFrameworkCore.");
    }

    [Test]
    public void ApplicationLayer_ShouldNotDependOnInfrastructureBootstrapperOrApiLayers()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(BootstrapperLayer)
            .AndShould().NotDependOnAny(ApiLayer)
            .Because("Guts.Application orchestrates domain logic through abstractions and must not depend on Infrastructure or API layers.");

        AssertNoViolations(rule);
    }

    [Test]
    public void InfrastructureLayer_ShouldNotDependOnBootstrapperOrApiLayers()
    {
        IArchRule rule = Types().That().Are(InfrastructureLayer)
            .Should().NotDependOnAny(BootstrapperLayer)
            .AndShould().NotDependOnAny(ApiLayer)
            .Because("Guts.Infrastructure implements persistence adapters and must not depend on Bootstrapper or API layers.");

        AssertNoViolations(rule);
    }

    [Test]
    public void ApiLayer_ShouldNotDependDirectlyOnInfrastructureLayer()
    {
        IArchRule rule = Types().That().Are(ApiLayer)
            .And().DoNotHaveFullName("Guts.Api.Program")
            .Should().NotDependOnAny(InfrastructureLayer)
            .Because("Guts.Api controllers, models, and services must communicate through Guts.Application interfaces and never access Guts.Infrastructure directly.");

        AssertNoViolations(rule);
    }

    [TestCase("Factory", nameof(DomainAssembly))]
    [TestCase("Calculator", nameof(DomainAssembly))]
    [TestCase("Repository", nameof(InfrastructureAssembly))]
    [TestCase("Service", nameof(ApplicationAssembly))]
    [TestCase("Converter", nameof(ApplicationAssembly))]
    public void AutoRegisteredClasses_ShouldImplementMatchingInterfaceEndingWithSameSuffix(string suffix, string assemblyFieldName)
    {
        System.Reflection.Assembly targetAssembly = assemblyFieldName switch
        {
            nameof(DomainAssembly) => DomainAssembly,
            nameof(InfrastructureAssembly) => InfrastructureAssembly,
            nameof(ApplicationAssembly) => ApplicationAssembly,
            _ => throw new ArgumentOutOfRangeException(nameof(assemblyFieldName))
        };

        var candidateTypes = targetAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.Namespace != null &&
                        t.Namespace.StartsWith("Guts.", StringComparison.Ordinal) &&
                        t.Name.EndsWith(suffix, StringComparison.Ordinal))
            .ToList();

        Assert.That(candidateTypes, Is.Not.Empty, $"Expected at least one *{suffix} class in {targetAssembly.GetName().Name}.");

        var nonCompliantTypes = candidateTypes
            .Where(t => !t.GetInterfaces().Any(i => i.Name.EndsWith(suffix, StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToList();

        Assert.That(
            nonCompliantTypes,
            Is.Empty,
            () => $"StartUpExtensions auto-registers *{suffix} classes by matching an I*{suffix} interface. Violating types: {string.Join(", ", nonCompliantTypes)}");
    }

    [Test]
    public void InfrastructureDbContextAndEntityConfigurations_ShouldBeInternal()
    {
        IArchRule rule = Classes().That().ResideInAssembly(InfrastructureAssembly)
            .And().HaveNameEndingWith("Context")
            .Or().ResideInNamespace("Guts.Infrastructure.EntityConfigurations")
            .Should().NotBePublic()
            .Because("EF Core DbContext and entity configurations must be encapsulated as internal inside Guts.Infrastructure.");

        AssertNoViolations(rule);
    }

    [Test]
    public void ApiControllers_ShouldBeSecuredAndResideInControllersNamespace()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t))
            .ToList();

        Assert.That(controllerTypes, Is.Not.Empty);

        var wrongNamespaceControllers = controllerTypes
            .Where(t => t.Namespace != "Guts.Api.Controllers" || !t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.That(
            wrongNamespaceControllers,
            Is.Empty,
            () => $"All API controllers must reside in Guts.Api.Controllers and end with 'Controller': {string.Join(", ", wrongNamespaceControllers)}");

        var unsecuredControllers = controllerTypes
            .Where(t => !typeof(Guts.Api.Controllers.ControllerBase).IsAssignableFrom(t) && !HasExplicitAuthorizationAttributes(t))
            .Select(t => t.FullName)
            .ToList();

        Assert.That(
            unsecuredControllers,
            Is.Empty,
            () => $"All API controllers must either inherit from Guts.Api.Controllers.ControllerBase or explicitly declare [Authorize]/[AllowAnonymous] on the class or all actions: {string.Join(", ", unsecuredControllers)}");
    }

    [Test]
    public void ValueObjects_ShouldBeImmutableAndHaveNoPublicSetters()
    {
        var valueObjectTypes = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && IsSubclassOfGenericValueObject(t))
            .ToList();

        Assert.That(valueObjectTypes, Is.Not.Empty, "Expected ValueObject subclasses in Guts.Domain.");

        var violatingProperties = (from type in valueObjectTypes
                                   from prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                   where prop.SetMethod != null && prop.SetMethod.IsPublic
                                   select $"{type.FullName}.{prop.Name}").ToList();

        Assert.That(
            violatingProperties,
            Is.Empty,
            () => $"Value Objects must be immutable and declare no public setters: {string.Join(", ", violatingProperties)}");
    }

    [Test]
    public void DomainFactories_ShouldBeStateless()
    {
        var factoryTypes = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.Name.EndsWith("Factory", StringComparison.Ordinal) &&
                        t.GetInterfaces().Any(i => i.Name.EndsWith("Factory", StringComparison.Ordinal)))
            .ToList();

        Assert.That(factoryTypes, Is.Not.Empty, "Expected domain factory types in Guts.Domain.");

        var mutableFields = (from type in factoryTypes
                             from field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                             where !field.IsInitOnly && !field.Name.Contains("BackingField", StringComparison.Ordinal)
                             select $"{type.FullName}.{field.Name}").ToList();

        Assert.That(
            mutableFields,
            Is.Empty,
            () => $"Domain factories are registered as Singletons and must be stateless without mutable instance fields: {string.Join(", ", mutableFields)}");
    }

    private static bool IsSubclassOfGenericValueObject(System.Type type)
    {
        var current = type.BaseType;
        while (current != null && current != typeof(object))
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.StartsWith("ValueObject", StringComparison.Ordinal))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    private static bool HasExplicitAuthorizationAttributes(System.Type controllerType)
    {
        if (controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any() ||
            controllerType.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any())
        {
            return true;
        }

        var publicActions = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .ToList();

        return publicActions.Count > 0 && publicActions.All(m =>
            m.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any() ||
            m.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any());
    }

    private static void AssertNoViolations(IArchRule rule)
    {
        var violations = rule.Evaluate(GutsArchitecture)
            .Where(result => !result.Passed)
            .Select(result => result.Description)
            .ToList();

        Assert.That(
            violations,
            Is.Empty,
            () => $"Architecture rule '{rule.Description}' was violated:\n{string.Join("\n", violations)}");
    }
}
