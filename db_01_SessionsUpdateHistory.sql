use DevelopmentDashboard
go

create table SessionsUpdateHistrory
(
    SessionId int references Logins(Id) not null,
    AgentMachineName varchar(256),
    AgentVersion varchar(256),
    UpdateMoment DateTime,
    UpdateDetails varchar(max)
)
go

grant SELECT, INSERT, DELETE, UPDATE on SessionsUpdateHistrory to DDUser
go

select * from SessionsUpdateHistrory order by Update Moment desc
go