use DevelopmentDashboard
go

create table Sessions
(
	Id int identity(1,1) primary key,
	UserName varchar(256),
	MachineName varchar(256),
	SessionBegin DateTime,
	SessionEnd DateTime,
	ActiveHours float,
	SessionState varchar (256),
	Comment varchar(max) NULL,
	LastInputTime datetime NULL,
	ClientName varchar(256) NULL,
	ClientDisplayDetails varchar(256) NULL,
	ClientReportedIPAddress varchar(256) NULL,
	ClientBuildNumber int NULL
)
go

grant SELECT, INSERT, DELETE, UPDATE on Logins to DDUser
go

select * from Logins order by Id desc
go