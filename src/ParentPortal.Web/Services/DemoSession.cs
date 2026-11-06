using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

using ParentPortal.Contracts;

namespace ParentPortal.Web.Services;

/// <summary>
/// The demo user selected in this browser circuit. Persisted in protected local storage so it
/// survives page reloads. Replace with OpenID Connect sign-in when Entra ID is configured.
/// </summary>
public sealed class DemoSession(ProtectedLocalStorage storage)
{
    private const string StorageKey = "parentportal.demo-user";
    private bool _loaded;

    public event Action? Changed;

    public DemoUserDto? CurrentUser { get; private set; }

    public bool IsSignedIn => CurrentUser is not null;

    public bool CanAuthor => CurrentUser?.Role is "Teacher" or "SchoolAdmin";

    public bool IsParent => CurrentUser?.Role == "Parent";

    public bool IsAdmin => CurrentUser?.Role == "SchoolAdmin";

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        try
        {
            var stored = await storage.GetAsync<DemoUserDto>(StorageKey);
            if (stored.Success && stored.Value is not null)
            {
                CurrentUser = stored.Value;
                Changed?.Invoke();
            }
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Data protection keys rotated - ignore the stale value.
            await storage.DeleteAsync(StorageKey);
        }
    }

    public async Task SignInAsync(DemoUserDto user)
    {
        CurrentUser = user;
        await storage.SetAsync(StorageKey, user);
        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        CurrentUser = null;
        await storage.DeleteAsync(StorageKey);
        Changed?.Invoke();
    }
}
