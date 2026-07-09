using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;
namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

/// <summary>
/// Hashes and verifies passwords using a secure algorithm.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Produces a secure hash of the given plain-text password.
    /// </summary>
    string Hash(string plainPassword);

    /// <summary>
    /// Verifies a plain-text password against a previously stored hash.
    /// </summary>
    bool Verify(string plainPassword, string hashedPassword);
}
