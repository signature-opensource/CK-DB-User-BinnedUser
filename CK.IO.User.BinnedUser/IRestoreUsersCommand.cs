using CK.Auth;
using CK.Cris;

namespace CK.IO.User.BinnedUser;

/// <summary>
/// Restores one or more previously archived users. The acting <see cref="ICommandAuthNormal.ActorId"/>
/// must be allowed to restore (by default a member of the Administrators group, enforced by CK.sUserRestore).
/// </summary>
public interface IRestoreUsersCommand : ICommand<ICrisBasicCommandResult>, ICommandAuthNormal
{
    /// <summary>
    /// Gets or sets the identifiers of the users to restore.
    /// </summary>
    List<int> UserIds { get; set; }
}
