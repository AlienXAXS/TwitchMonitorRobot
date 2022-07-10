SELECT streams.start INTO @START_DATE
FROM `streams`
WHERE `end` IS NULL
ORDER BY `id` DESC
LIMIT 1;


SELECT @MODERATOR_CHAT_MESSAGES := count(*)
FROM users
INNER JOIN messages AS message ON message.userid = users.id
WHERE message.date >= @START_DATE
  AND NOT users.username LIKE '%bot%'
  AND users.ismod;


SELECT @TOTAL_CHAT_MESSAGES := count(*)
FROM users
INNER JOIN messages AS message ON message.userid = users.id
WHERE message.date >= @START_DATE
  AND NOT users.username LIKE '%bot%';


SELECT commands.command,
       count(*) AS magnitude INTO @MOST_USED_COMMAND,
                                  @MOST_USED_COMMAND_COUNT
FROM commands
WHERE commands.date >= @START_DATE
GROUP BY commands.command
ORDER BY magnitude DESC
LIMIT 1;


SELECT IF(@START_DATE IS NULL, "No Stream", @START_DATE) AS "Stream Start",
       IF(@START_DATE IS NULL, "No Stream", CONCAT(HOUR(TIMEDIFF(@START_DATE, NOW())), "h ", MINUTE(TIMEDIFF(@START_DATE, NOW())), "m")) AS "Stream Duration",
       @TOTAL_CHAT_MESSAGES AS "Total Chat Messages",
       @MODERATOR_CHAT_MESSAGES AS "Moderator Chat Messages",
       IF(@START_DATE IS NULL, "No Data", CONCAT(@MOST_USED_COMMAND, " (", @MOST_USED_COMMAND_COUNT, " times)")) AS "Most Commonly Used Command"