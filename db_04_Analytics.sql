use DevelopmentDashboard
go

--количество активных часов пользователя в периоде
select SUM(DATEDIFF(minute, ChunkBegin, ChunkEnd) * IsUserActive) * 1.0 / 60 as WorkingMinutes from SessionActivityProfiles
left join Logins on Logins.Id = SessionActivityProfiles.SessionId
where UserName = 'AKolmakov' and ChunkBegin BETWEEN '2016-08-21' AND '2016-08-27 23:59:59'

