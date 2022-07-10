SELECT streams.start INTO @START_DATE
FROM `streams`
WHERE `end` IS NULL
ORDER BY `id` DESC
LIMIT 1;

SELECT username,
       command.command AS "Command",
       count(*) AS "Command Usage By User"
FROM users
INNER JOIN commands AS command ON command.userid = users.id
WHERE command.date >= @START_DATE
GROUP BY command;

