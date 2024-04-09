namespace FlashBlazor.Infrastructure;

/// <summary>
/// 用户会话服务接口
/// </summary>
public interface IUserSessionService
{
    string? Token { get; set; }
}

/// <summary>
/// 用户会话服务
/// </summary>
[AddScopedAsImplementedInterfaces]
public class UserSessionService : IUserSessionService
{
    public string? Token { get; set; } = null;
}
