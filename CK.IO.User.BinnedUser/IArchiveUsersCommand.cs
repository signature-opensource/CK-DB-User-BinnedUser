using CK.Auth;
using CK.Cris;

namespace CK.IO.User.BinnedUser;

/// <summary>
/// Archives (bins) one or more users. The acting <see cref="ICommandAuthNormal.ActorId"/> must be
/// allowed to archive (by default a member of the Administrators group, enforced by CK.sUserArchive).
/// </summary>
public interface IArchiveUsersCommand : ICommand<ICrisBasicCommandResult>, ICommandAuthNormal
{
    /// <summary>
    /// Gets or sets the identifiers of the users to archive.
    /// </summary>
    List<int> UserIds { get; set; }
}
