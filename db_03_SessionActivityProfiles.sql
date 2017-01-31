use DevelopmentDashboard
go

create table SessionActivityProfiles
(
	SessionId int references Logins(Id) not null,
	ChunkBegin DateTime not null,
	ChunkEnd DateTime not null,
	IsUserActive bit not null
)
go

grant SELECT, INSERT, DELETE, UPDATE on SessionActivityProfiles to DDUser
go

CREATE NONCLUSTERED INDEX [NonClusteredIndex-ChunkBegin] ON [dbo].[SessionActivityProfiles]
(
	[ChunkBegin] ASC
)
INCLUDE ( 	[IsUserActive], [SessionId]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO