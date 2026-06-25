namespace MidgardStudio.Core.Validation;

/// <summary>
/// A one-click remedy for a validation issue. <see cref="Apply"/> mutates the model to resolve the
/// finding; the caller re-validates afterwards. Pure-data fixes are built in Core; fixes that need
/// client/GRF services are built in the App layer (capturing the service in the delegate).
/// </summary>
public sealed record QuickFix(string Title, Action Apply);
