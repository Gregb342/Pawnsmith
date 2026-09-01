namespace Pawnsmith.Domain;

/// <summary>
/// What the caller asks for: a geometry, a paper format, and a list of items.
/// </summary>
/// <remarks>
/// This is the domain's view of the manifest of B.3, stripped of everything to
/// do with files and parsing. The geometry applies to the whole request
/// (DEC-001).
/// </remarks>
/// <param name="Geometry">Geometry of every pawn on the sheet.</param>
/// <param name="PaperFormat">Paper the sheet is laid out for.</param>
/// <param name="Items">Items to lay out, in the order they were requested.</param>
public sealed record SheetRequest(
    Geometry Geometry,
    PaperFormat PaperFormat,
    IReadOnlyList<SheetItem> Items);

/// <summary>
/// One requested pawn, in a quantity.
/// </summary>
/// <remarks>
/// Image files are named, not opened: the domain never touches the file
/// system. The names travel through to the layout so the renderer knows what
/// to draw, and their resolution to actual bytes is Infrastructure's problem.
/// </remarks>
/// <param name="Name">Name of the item, used in error messages.</param>
/// <param name="Size">Size of the pawn, which decides the page it lands on.</param>
/// <param name="Quantity">How many copies to print. At least one.</param>
/// <param name="FrontImageFile">File name of the front artwork.</param>
/// <param name="BackImageFile">File name of the back artwork.</param>
public sealed record SheetItem(
    string Name,
    Size Size,
    int Quantity,
    string FrontImageFile,
    string BackImageFile);
