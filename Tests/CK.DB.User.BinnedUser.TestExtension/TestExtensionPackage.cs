using CK.Core;

namespace CK.DB.User.BinnedUser.TestExtension;

/// <summary>
/// FOR TESTS ONLY. A sample "consumer" package that transforms <c>CK.sUserArchive</c> and
/// <c>CK.sUserRestore</c> through their injection points (<c>ArchiveSecurityCheck</c> /
/// <c>RestoreSecurityCheck</c>) to override the default Administrators-only security decision.
/// See the .tql transformers in Res. This package only exists to exercise the extension mechanism
/// of <see cref="CK.DB.User.BinnedUser.Package"/> from the test suite.
/// </summary>
[SqlPackage( Schema = "CK", ResourcePath = "Res" )]
[Versions( "1.0.0" )]
[SqlObjectItem( "transform:sUserArchive, transform:sUserRestore" )]
public abstract class TestExtensionPackage : SqlPackage
{
    void StObjConstruct( CK.DB.User.BinnedUser.Package binnedUserPackage )
    {
    }
}
