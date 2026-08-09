using Backend.Models;

namespace Backend.Authentication;

public interface ITokenService
{
    string CreateToken(User user);
}
