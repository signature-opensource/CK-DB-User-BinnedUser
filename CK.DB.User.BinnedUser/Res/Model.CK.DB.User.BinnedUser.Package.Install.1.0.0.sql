--[beginscript]

-- Adds the BinDate column to CK.tUser only if it does not already exist
-- (the column may have been created independently, e.g. by the Web layer).
if not exists(
  select *
  from sys.columns
  where object_id = object_id( N'CK.tUser' )
        and name = 'BinDate'
)
begin
    alter table CK.tUser add BinDate datetime2( 2 ) null;
end

--[endscript]
