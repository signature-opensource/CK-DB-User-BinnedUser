using CK.Core;
using CK.Cris;
using CK.IO.User.BinnedUser;
using CK.SqlServer;
using System;
using System.Threading.Tasks;

namespace CK.DB.User.BinnedUser;

public partial class Package
{
    /// <summary>
    /// Handles <see cref="IArchiveUsersCommand"/>: archives every requested user in a single
    /// transaction. The per-user security check (Administrators group by default) is enforced by
    /// CK.sUserArchive; a failure rolls back the whole batch and is reported as a user message.
    /// </summary>
    [CommandHandler]
    public async Task<ICrisBasicCommandResult> HandleArchiveUsersCommandAsync( ISqlTransactionCallContext ctx,
                                                                               UserMessageCollector collector,
                                                                               IArchiveUsersCommand cmd,
                                                                               BinnedUserTable table )
    {
        int actorId = cmd.ActorId.GetValueOrDefault();
        using( ctx.Monitor.OpenInfo( $"Handling {nameof( IArchiveUsersCommand )}. (ActorId: {actorId}, Count: {cmd.UserIds.Count})" ) )
        {
            var res = cmd.CreateResult();
            if( cmd.UserIds.Count == 0 )
            {
                collector.Error( "No user identifier provided.", "BinnedUser.NoUserId" );
                res.SetUserMessages( collector );
                return res;
            }
            try
            {
                using( var transaction = ctx.GetConnectionController( table ).BeginTransaction() )
                {
                    foreach( var id in cmd.UserIds )
                    {
                        await table.ArchiveUserAsync( ctx, actorId, id );
                        ctx.Monitor.Info( $"User successfully archived. (UserId: {id})" );
                    }
                    transaction.Commit();
                }
                collector.Info( $"{cmd.UserIds.Count} user(s) successfully archived.", "BinnedUser.UsersArchived" );
            }
            catch( SqlDetailedException ex ) when( ex.InnerSqlException is not null )
            {
                ctx.Monitor.Error( $"Error while handling {nameof( IArchiveUsersCommand )}.", ex );
                collector.Error( "Users could not be archived.", "BinnedUser.ArchiveFailed" );
            }
            catch( Exception e )
            {
                ctx.Monitor.Error( e );
                collector.Error( "An error occurred while archiving users.", "BinnedUser.ArchiveFailed" );
            }
            res.SetUserMessages( collector );
            return res;
        }
    }

    /// <summary>
    /// Handles <see cref="IRestoreUsersCommand"/>: restores every requested user in a single
    /// transaction. The per-user security check (Administrators group by default) is enforced by
    /// CK.sUserRestore; a failure rolls back the whole batch and is reported as a user message.
    /// </summary>
    [CommandHandler]
    public async Task<ICrisBasicCommandResult> HandleRestoreUsersCommandAsync( ISqlTransactionCallContext ctx,
                                                                               UserMessageCollector collector,
                                                                               IRestoreUsersCommand cmd,
                                                                               BinnedUserTable table )
    {
        int actorId = cmd.ActorId.GetValueOrDefault();
        using( ctx.Monitor.OpenInfo( $"Handling {nameof( IRestoreUsersCommand )}. (ActorId: {actorId}, Count: {cmd.UserIds.Count})" ) )
        {
            var res = cmd.CreateResult();
            if( cmd.UserIds.Count == 0 )
            {
                collector.Error( "No user identifier provided.", "BinnedUser.NoUserId" );
                res.SetUserMessages( collector );
                return res;
            }
            try
            {
                using( var transaction = ctx.GetConnectionController( table ).BeginTransaction() )
                {
                    foreach( var id in cmd.UserIds )
                    {
                        await table.RestoreUserAsync( ctx, actorId, id );
                        ctx.Monitor.Info( $"User successfully restored. (UserId: {id})" );
                    }
                    transaction.Commit();
                }
                collector.Info( $"{cmd.UserIds.Count} user(s) successfully restored.", "BinnedUser.UsersRestored" );
            }
            catch( SqlDetailedException ex ) when( ex.InnerSqlException is not null )
            {
                ctx.Monitor.Error( $"Error while handling {nameof( IRestoreUsersCommand )}.", ex );
                collector.Error( "Users could not be restored.", "BinnedUser.RestoreFailed" );
            }
            catch( Exception e )
            {
                ctx.Monitor.Error( e );
                collector.Error( "An error occurred while restoring users.", "BinnedUser.RestoreFailed" );
            }
            res.SetUserMessages( collector );
            return res;
        }
    }
}
