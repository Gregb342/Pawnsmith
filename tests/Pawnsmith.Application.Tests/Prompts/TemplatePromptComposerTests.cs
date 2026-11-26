using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Prompts;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Tests.Prompts;

/// <summary>
/// The v1 composer holds two files and delegates; these tests pin what it
/// checks on the way.
/// </summary>
public class TemplatePromptComposerTests
{
    [Fact]
    public void ItComposesExactlyWhatTheDomainRuleComposes()
    {
        IPromptComposer composer = PromptFixture.Composer();
        Blueprint blueprint = ProjectFixture.Blueprint();

        ComposedSubject composed = composer.ComposeSubject(blueprint, Universe.Fantasy);

        // Member by member: a record holding a list compares that member by
        // reference, the trap that cost T2 two tests.
        ComposedSubject direct = SubjectClause.Compose(blueprint, PromptFixture.Template(), PromptFixture.Catalog());

        composed.Clause.ShouldBe(ProjectFixture.Subject);
        composed.Clause.ShouldBe(direct.Clause);
        composed.Diagnostics.ShouldBe(direct.Diagnostics);
    }

    [Fact]
    public void TheTemplateAndTheCatalogueMustBeForTheSameUniverse()
    {
        // One value in v1, so this cannot fail today. The check is what makes
        // "the two files agree" a fact rather than an assumption once a second
        // universe exists (EVO-004).
        PromptTemplate template = PromptFixture.Template();
        Catalog catalog = PromptFixture.Catalog();

        Should.NotThrow(() => new TemplatePromptComposer(template, catalog));
        Should.Throw<ArgumentNullException>(() => new TemplatePromptComposer(template, (Catalog)null!));
    }

    [Fact]
    public void ThePortMentionsNeitherStyleNorFramingClause()
    {
        // DEC-066: the lock of DEC-028 is carried by the type. No method of the
        // port takes or returns a Style, and the only method takes a Blueprint
        // and a Universe.
        System.Reflection.MethodInfo[] methods = typeof(IPromptComposer).GetMethods();

        System.Reflection.MethodInfo only = methods.ShouldHaveSingleItem();
        only.Name.ShouldBe(nameof(IPromptComposer.ComposeSubject));
        only.GetParameters().Select(parameter => parameter.ParameterType)
            .ShouldBe([typeof(Blueprint), typeof(Universe)]);
        only.ReturnType.ShouldBe(typeof(ComposedSubject));
    }
}
