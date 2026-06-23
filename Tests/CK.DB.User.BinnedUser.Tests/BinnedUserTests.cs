using CK.Core;
using CK.SqlServer;
using CK.Testing;
using NUnit.Framework;
using Shouldly;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.User.BinnedUser.Tests;

[TestFixture]
public class BinnedUserTests
{
    /// <summary>The well-known Administrators group identifier (from CK.DB.Actor).</summary>
    const int AdministratorsGroupId = 2;

    static BinnedUserTable BinnedUsers => SharedEngine.Map.StObjs.Obtain<BinnedUserTable>()!;
    static Actor.UserTable Users => SharedEngine.Map.StObjs.Obtain<Actor.UserTable>()!;
    static Actor.GroupTable Groups => SharedEngine.Map.StObjs.Obtain<Actor.GroupTable>()!;

    static int CreateUser( ISqlCallContext ctx ) => Users.CreateUser( ctx, 1, Guid.NewGuid().ToString( "N" ) );

    static int CreateAdmin( ISqlCallContext ctx )
    {
        int id = CreateUser( ctx );
        Groups.AddUser( ctx, 1, AdministratorsGroupId, id );
        return id;
    }

    static object? ReadBinDate( ISqlCallContext ctx, int userId )
        => BinnedUsers.Database.ExecuteScalar( "select BinDate from CK.vUser where UserId = @0", userId );

    [Test]
    public async Task admin_archiving_a_user_sets_BinDate_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int admin = CreateAdmin( ctx );
        int target = CreateUser( ctx );

        await BinnedUsers.ArchiveUserAsync( ctx, admin, target );

        var binDate = ReadBinDate( ctx, target );
        binDate.ShouldBeOfType<DateTime>().ShouldBe( DateTime.UtcNow, tolerance: TimeSpan.FromMinutes( 1 ) );
    }

    [Test]
    public async Task admin_restoring_a_user_clears_BinDate_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int admin = CreateAdmin( ctx );
        int target = CreateUser( ctx );

        await BinnedUsers.ArchiveUserAsync( ctx, admin, target );
        ReadBinDate( ctx, target ).ShouldBeOfType<DateTime>();

        await BinnedUsers.RestoreUserAsync( ctx, admin, target );
        ReadBinDate( ctx, target ).ShouldBe( DBNull.Value );
    }

    [Test]
    public async Task archiving_twice_is_idempotent_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int admin = CreateAdmin( ctx );
        int target = CreateUser( ctx );

        await BinnedUsers.ArchiveUserAsync( ctx, admin, target );
        await BinnedUsers.ArchiveUserAsync( ctx, admin, target );

        ReadBinDate( ctx, target ).ShouldBeOfType<DateTime>();
    }

    [Test]
    public async Task restoring_a_non_archived_user_is_a_noop_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int admin = CreateAdmin( ctx );
        int target = CreateUser( ctx );

        await BinnedUsers.RestoreUserAsync( ctx, admin, target );

        ReadBinDate( ctx, target ).ShouldBe( DBNull.Value );
    }

    [Test]
    public async Task non_admin_actor_cannot_archive_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int nonAdmin = CreateUser( ctx );
        int target = CreateUser( ctx );

        await Should.ThrowAsync<SqlDetailedException>( () => BinnedUsers.ArchiveUserAsync( ctx, nonAdmin, target ) );
        ReadBinDate( ctx, target ).ShouldBe( DBNull.Value );
    }

    [Test]
    public async Task non_admin_actor_cannot_restore_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int admin = CreateAdmin( ctx );
        int nonAdmin = CreateUser( ctx );
        int target = CreateUser( ctx );

        await BinnedUsers.ArchiveUserAsync( ctx, admin, target );

        await Should.ThrowAsync<SqlDetailedException>( () => BinnedUsers.RestoreUserAsync( ctx, nonAdmin, target ) );
        ReadBinDate( ctx, target ).ShouldBeOfType<DateTime>();
    }

    [Test]
    public async Task archive_with_anonymous_actor_throws_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int target = CreateUser( ctx );

        await Should.ThrowAsync<SqlDetailedException>( () => BinnedUsers.ArchiveUserAsync( ctx, 0, target ) );
    }

    [Test]
    public async Task archive_with_invalid_userId_throws_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int admin = CreateAdmin( ctx );

        await Should.ThrowAsync<SqlDetailedException>( () => BinnedUsers.ArchiveUserAsync( ctx, admin, 0 ) );
    }

    [Test]
    public void BinDate_is_exposed_in_vUser()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int userId = CreateUser( ctx );
        // The column is present in the view and null for a freshly created (active) user.
        BinnedUsers.Database.ExecuteScalar( "select BinDate from CK.vUser where UserId = @0", userId ).ShouldBe( DBNull.Value );
    }

    [Test]
    public async Task archived_and_active_users_can_be_filtered_via_vUser_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int admin = CreateAdmin( ctx );
        int active = CreateUser( ctx );
        int archived = CreateUser( ctx );

        await BinnedUsers.ArchiveUserAsync( ctx, admin, archived );

        BinnedUsers.Database.ExecuteScalar(
            "select count(*) from CK.vUser where UserId = @0 and BinDate is null", active ).ShouldBe( 1 );
        BinnedUsers.Database.ExecuteScalar(
            "select count(*) from CK.vUser where UserId = @0 and BinDate is not null", archived ).ShouldBe( 1 );
    }
}
