namespace DA.KinHub.Business.Common;

public static class BusinessErrorCodes
{
    public const string FamilyAccessDenied = "family.accessDenied";
    public const string FamilyNameInvalid = "family.nameInvalid";
    public const string FamilyStateInconsistent = "family.stateInconsistent";
    public const string PaginationPageSizeInvalid = "pagination.pageSizeInvalid";
    public const string PaginationCursorInvalid = "pagination.cursorInvalid";
    public const string PostgreSqlUnavailable = "dependency.postgresqlUnavailable";
    public const string StorageUnavailable = "dependency.storageUnavailable";
    public const string ServiceAccessDenied = "service.accessDenied";
}
