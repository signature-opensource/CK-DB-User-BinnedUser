using CK.Core;
using CK.SqlServer;
using System.Threading.Tasks;

namespace CK.DB.User.BinnedUser;

/// <summary>
/// Extends <c>CK.tUser</c> with a <c>BinDate</c> column and exposes the archive/restore behavior.
/// The <c>vUser</c> view is transformed to expose <c>BinDate</c>.
/// </summary>
[SqlTable( "tUser", Package = typeof( Package ) )]
[Versions( "1.0.0" )]
[SqlObjectItem( "transform:vUser" )]
public abstract class BinnedUserTable : SqlTable
{
    void StObjConstruct( Actor.UserTable userTable ) { }

    /// <summary>
    /// Archives (bins) a user by setting its <c>BinDate</c> to the current UTC time.
    /// By default, only members of the Administrators group (GroupId 2) are allowed.
    /// </summary>
    /// <param name="ctx">The call context.</param>
    /// <param name="actorId">The acting actor identifier. Must be a platform administrator.</param>
    /// <param name="userId">The identifier of the user to archive.</param>
    /// <returns>The awaitable.</returns>
    [SqlProcedure( "sUserArchive" )]
    public abstract Task ArchiveUserAsync( ISqlCallContext ctx, int actorId, int userId );

    /// <summary>
    /// Restores a previously archived user by clearing its <c>BinDate</c>.
    /// By default, only members of the Administrators group (GroupId 2) are allowed.
    /// </summary>
    /// <param name="ctx">The call context.</param>
    /// <param name="actorId">The acting actor identifier. Must be a platform administrator.</param>
    /// <param name="userId">The identifier of the user to restore.</param>
    /// <returns>The awaitable.</returns>
    [SqlProcedure( "sUserRestore" )]
    public abstract Task RestoreUserAsync( ISqlCallContext ctx, int actorId, int userId );
}
