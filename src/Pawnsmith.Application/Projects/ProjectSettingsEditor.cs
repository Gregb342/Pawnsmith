using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Projects;

/// <summary>The settings of a project, as the Projet step edits them.</summary>
public sealed record ProjectSettings(
    string Name,
    Universe Universe,
    Geometry Geometry,
    string PaperFormatName,
    Style Style,
    CalibrationOverrides CalibrationOverrides);

/// <summary>
/// Applies new settings to a project, under the rule of DEC-112: once a
/// blueprint has a proposal, the universe and the style no longer change.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why the rule lives here, and not in the save.</b> DEC-055 keeps the
/// repository blind to the previous state: <c>SaveAsync</c> receives a project,
/// never the one it replaces. Comparing belongs to the use case, which has
/// both — the project it loaded, inside the write gate, and the settings it
/// was asked to apply.
/// </para>
/// <para>
/// <b>What stays free.</b> Name, geometry, paper format and tab dimensions
/// only change how the pawns are cut, never the images (DEC-030 still governs
/// them). A project of one style is a coherent set of images; another style is
/// another project, which <see cref="ProjectDuplication"/> provides.
/// </para>
/// <para>
/// Not a confirmation, and not the consent mechanism DEC-030 rejected: there
/// is no warning to accept. The refusal says the field is frozen; the
/// interface shows it frozen before anyone tries.
/// </para>
/// </remarks>
public static class ProjectSettingsEditor
{
    /// <summary>Whether the project's universe and style are frozen: some blueprint has at least one proposal.</summary>
    public static bool IsFrozen(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return project.Blueprints.Any(blueprint => blueprint.Candidates.Count > 0);
    }

    /// <summary>The project with the requested settings.</summary>
    /// <exception cref="ProjectRuleException">
    /// <c>UNIVERSE_FROZEN</c> or <c>STYLE_FROZEN</c>: the project has
    /// proposals and the request changes one of them.
    /// </exception>
    public static Project Apply(Project project, ProjectSettings settings)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(settings);

        if (IsFrozen(project))
        {
            if (settings.Universe != project.Universe)
            {
                throw new ProjectRuleException(
                    ProjectRuleCode.UniverseFrozen,
                    $"The project has proposals; its universe stays {project.Universe}. Duplicate the project to change it.");
            }

            // The whole style, compared field by field and ordinally - a record
            // of strings compares that way. The palette counts too, although the
            // interface no longer shows it: the front sends back what it got.
            if (settings.Style != project.Style)
            {
                throw new ProjectRuleException(
                    ProjectRuleCode.StyleFrozen,
                    "The project has proposals; its style no longer changes. Duplicate the project to change it.");
            }
        }

        return project with
        {
            Name = settings.Name,
            Universe = settings.Universe,
            Geometry = settings.Geometry,
            PaperFormatName = settings.PaperFormatName,
            Style = settings.Style,
            CalibrationOverrides = settings.CalibrationOverrides,
        };
    }
}
