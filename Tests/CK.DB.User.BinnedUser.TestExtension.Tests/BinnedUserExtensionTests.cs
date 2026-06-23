using CK.Core;
using CK.DB.User.BinnedUser.TestExtension;
using CK.SqlServer;
using CK.Testing;
using NUnit.Framework;
using Shouldly;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.User.BinnedUser.TestExtension.Tests;

/// <summary>
/// Validates that the ArchiveSecurityCheck / RestoreSecurityCheck injection points work: this test
/// assembly's model includes <see cref="TestExtensionPackage"/>, whose transformers force
/// <c>@CanContinue = 1</c>. A non-administrator actor — which would be rejected by the default
/// behavior tested in CK.DB.User.BinnedUser.Tests — is now allowed to archive and restore.
/// </summary>
[TestFixture]
public class BinnedUserExtensionTests
{
    static BinnedUserTable BinnedUsers => SharedEngine.Map.StObjs.Obtain<BinnedUserTable>()!;
    static Actor.UserTable Users => SharedEngine.Map.StObjs.Obtain<Actor.UserTable>()!;

    // Sanity check: the TestExtensionPackage transformer is actually part of the model.
    static TestExtensionPackage TheExtension => SharedEngine.Map.StObjs.Obtain<TestExtensionPackage>()!;

    static int CreateUser( ISqlCallContext ctx ) => Users.CreateUser( ctx, 1, Guid.NewGuid().ToString( "N" ) );

    static object? ReadBinDate( ISqlCallContext ctx, int userId )
        => BinnedUsers.Database.ExecuteScalar( "select BinDate from CK.vUser where UserId = @0", userId );

    [Test]
    public void the_extension_package_is_registered()
    {
        TheExtension.ShouldNotBeNull();
    }

    [Test]
    public async Task extension_allows_a_non_admin_to_archive_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int nonAdmin = CreateUser( ctx );
        int target = CreateUser( ctx );

        // No membership in the Administrators group, yet the injected "set @CanContinue = 1" allows it.
        await BinnedUsers.ArchiveUserAsync( ctx, nonAdmin, target );

        ReadBinDate( ctx, target ).ShouldBeOfType<DateTime>();
    }

    [Test]
    public async Task extension_allows_a_non_admin_to_restore_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int nonAdmin = CreateUser( ctx );
        int target = CreateUser( ctx );

        await BinnedUsers.ArchiveUserAsync( ctx, nonAdmin, target );
        ReadBinDate( ctx, target ).ShouldBeOfType<DateTime>();

        await BinnedUsers.RestoreUserAsync( ctx, nonAdmin, target );
        ReadBinDate( ctx, target ).ShouldBe( DBNull.Value );
    }

    [Test]
    public async Task anonymous_actor_still_throws_despite_the_extension_Async()
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        int target = CreateUser( ctx );

        // The guard runs before the injection point, so it is not bypassed.
        await Should.ThrowAsync<SqlDetailedException>( () => BinnedUsers.ArchiveUserAsync( ctx, 0, target ) );
    }
}
