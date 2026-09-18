namespace LedgerLoop.Api.Security;

public sealed class ActorContext
{
    public const string RoleHeader = "X-LedgerLoop-Role";
    public const string UserHeader = "X-LedgerLoop-User";

    private readonly IHttpContextAccessor _accessor;

    public ActorContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? Role
    {
        get
        {
            var value = _accessor.HttpContext?.Request.Headers[RoleHeader].FirstOrDefault();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    public string User =>
        _accessor.HttpContext?.Request.Headers[UserHeader].FirstOrDefault() ?? "anonymous";
}
