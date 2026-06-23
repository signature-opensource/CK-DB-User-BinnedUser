-- SetupConfig: {}
--
-- Archives (bins) a user by stamping CK.tUser.BinDate with the current UTC time.
create procedure CK.sUserArchive
(
    @ActorId int,
    @UserId int
)
as
begin
    if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;
    if @UserId <= 0 throw 50000, 'User.InvalidUserId', 1;

    --[beginsp]

    -- By default, only members of the Administrators group (GroupId 2) can archive a user.
    declare @CanContinue bit = 0;
    if exists( select 1 from CK.tActorProfile where ActorId = @ActorId and GroupId = 2 )
        set @CanContinue = 1;

    -- Extension point: a consuming package can transform this procedure to inject SQL here
    -- that adjusts @CanContinue (to re-open or to harden the access) before the final check.
    --<ArchiveSecurityCheck revert />

    if @CanContinue = 0 throw 50000, 'Security.PlatformAdministratorOnly', 1;

    update CK.tUser
        set BinDate = sysutcdatetime()
        where UserId = @UserId;

    --[endsp]
end
