use DevelopmentDashboard
go

create table UtilsDbLog
(
	Moment DateTime not null,
	Sender varchar(128),
	MessageType varchar(12),
	MessageText varchar(1024)
)
go

grant SELECT, INSERT, DELETE, UPDATE on UtilsDbLog to DDUser
go
