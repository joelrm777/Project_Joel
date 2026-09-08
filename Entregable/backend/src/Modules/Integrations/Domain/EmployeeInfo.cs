namespace MileageClaims.Modules.Integrations.Domain;

/// <summary>Lo que trae el ERP de RH al buscar por cédula (RN-17).</summary>
public sealed record EmployeeInfo(
    string NationalId,
    string Name,
    string Email,
    string ApproverNationalId,
    string ApproverEmail,
    string Department,
    string JobTitle,
    bool IsActive);
