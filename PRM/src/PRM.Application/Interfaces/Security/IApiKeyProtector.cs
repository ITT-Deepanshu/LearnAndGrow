namespace PRM.Application.Interfaces.Security;

public interface IApiKeyProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
    string Mask(string? protectedText);
}
