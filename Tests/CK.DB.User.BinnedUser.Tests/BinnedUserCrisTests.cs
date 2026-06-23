using CK.Core;
using CK.Cris;
using CK.IO.User.BinnedUser;
using CK.SqlServer;
using CK.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.User.BinnedUser.Tests;

[TestFixture]
public class BinnedUserCrisTests
{
    /// <summary>The well-known Administrators group identifier (from CK.DB.Actor).</summary>
    const int AdministratorsGroupId = 2;

    static int CreateUser( Actor.UserTable users, ISqlCallContext ctx )
        => users.CreateUser( ctx, 1, Guid.NewGuid().ToString( "N" ) );

    static int CreateAdmin( Actor.UserTable users, Actor.GroupTable groups, ISqlCallContext ctx )
    {
        int id = CreateUser( users, ctx );
        groups.AddUser( ctx, 1, AdministratorsGroupId, id );
        return id;
    }

    static object? ReadBinDate( BinnedUserTable binnedUsers, int userId )
        => binnedUsers.Database.ExecuteScalar( "select BinDate from CK.vUser where UserId = @0", userId );

    [Test]
    public async Task archive_users_command_archives_all_of_them_Async()
    {
        using var scope = SharedEngine.AutomaticServices.CreateScope();
        var services = scope.ServiceProvider;
        var pocoDir = services.GetRequiredService<PocoDirectory>();
        var exec = services.GetRequiredService<CrisExecutionContext>();
        var binnedUsers = services.GetRequiredService<BinnedUserTable>();
        var users = services.GetRequiredService<Actor.UserTable>();
        var groups = services.GetRequiredService<Actor.GroupTable>();

        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int admin = CreateAdmin( users, groups, ctx );
        int u1 = CreateUser( users, ctx );
        int u2 = CreateUser( users, ctx );

        var cmd = pocoDir.Create<IArchiveUsersCommand>( c =>
        {
            c.ActorId = admin;
            c.UserIds = new List<int> { u1, u2 };
        } );
        var executing = await exec.ExecuteRootCommandAsync( cmd );
        var res = executing.WithResult<ICrisBasicCommandResult>().Result;

        res.Success.ShouldBeTrue();
        ReadBinDate( binnedUsers, u1 ).ShouldBeOfType<DateTime>();
        ReadBinDate( binnedUsers, u2 ).ShouldBeOfType<DateTime>();
    }

    [Test]
    public async Task restore_users_command_restores_all_of_them_Async()
    {
        using var scope = SharedEngine.AutomaticServices.CreateScope();
        var services = scope.ServiceProvider;
        var pocoDir = services.GetRequiredService<PocoDirectory>();
        var exec = services.GetRequiredService<CrisExecutionContext>();
        var binnedUsers = services.GetRequiredService<BinnedUserTable>();
        var users = services.GetRequiredService<Actor.UserTable>();
        var groups = services.GetRequiredService<Actor.GroupTable>();

        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int admin = CreateAdmin( users, groups, ctx );
        int u1 = CreateUser( users, ctx );
        int u2 = CreateUser( users, ctx );

        await binnedUsers.ArchiveUserAsync( ctx, admin, u1 );
        await binnedUsers.ArchiveUserAsync( ctx, admin, u2 );

        var cmd = pocoDir.Create<IRestoreUsersCommand>( c =>
        {
            c.ActorId = admin;
            c.UserIds = new List<int> { u1, u2 };
        } );
        var executing = await exec.ExecuteRootCommandAsync( cmd );
        var res = executing.WithResult<ICrisBasicCommandResult>().Result;

        res.Success.ShouldBeTrue();
        ReadBinDate( binnedUsers, u1 ).ShouldBe( DBNull.Value );
        ReadBinDate( binnedUsers, u2 ).ShouldBe( DBNull.Value );
    }

    [Test]
    public async Task non_admin_archive_command_reports_an_error_and_changes_nothing_Async()
    {
        using var scope = SharedEngine.AutomaticServices.CreateScope();
        var services = scope.ServiceProvider;
        var pocoDir = services.GetRequiredService<PocoDirectory>();
        var exec = services.GetRequiredService<CrisExecutionContext>();
        var binnedUsers = services.GetRequiredService<BinnedUserTable>();
        var users = services.GetRequiredService<Actor.UserTable>();

        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int nonAdmin = CreateUser( users, ctx );
        int target = CreateUser( users, ctx );

        var cmd = pocoDir.Create<IArchiveUsersCommand>( c =>
        {
            c.ActorId = nonAdmin;
            c.UserIds = new List<int> { target };
        } );
        var executing = await exec.ExecuteRootCommandAsync( cmd );
        var res = executing.WithResult<ICrisBasicCommandResult>().Result;

        // The proc throws (not an administrator); the handler catches it and reports an error.
        // The transaction is rolled back, so the user is not archived.
        res.Success.ShouldBeFalse();
        ReadBinDate( binnedUsers, target ).ShouldBe( DBNull.Value );
    }

    [Test]
    public async Task archive_command_with_empty_list_reports_an_error_Async()
    {
        using var scope = SharedEngine.AutomaticServices.CreateScope();
        var services = scope.ServiceProvider;
        var pocoDir = services.GetRequiredService<PocoDirectory>();
        var exec = services.GetRequiredService<CrisExecutionContext>();
        var users = services.GetRequiredService<Actor.UserTable>();
        var groups = services.GetRequiredService<Actor.GroupTable>();

        int admin;
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            admin = CreateAdmin( users, groups, ctx );
        }

        var cmd = pocoDir.Create<IArchiveUsersCommand>( c =>
        {
            c.ActorId = admin;
            c.UserIds = new List<int>();
        } );
        var executing = await exec.ExecuteRootCommandAsync( cmd );
        var res = executing.WithResult<ICrisBasicCommandResult>().Result;

        res.Success.ShouldBeFalse();
    }
}
