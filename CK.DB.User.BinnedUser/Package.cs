using CK.Core;

namespace CK.DB.User.BinnedUser;

/// <summary>
/// Package that adds a <c>BinDate</c> to <c>CK.tUser</c> and supports archiving (binning) and
/// restoring users: archiving sets <c>BinDate</c> to the current UTC time, restoring clears it.
/// </summary>
[SqlPackage( Schema = "CK", ResourcePath = "Res" )]
[Versions( "1.0.0" )]
public abstract partial class Package : SqlPackage
{
    void StObjConstruct( Actor.Package actor )
    {
    }
}
