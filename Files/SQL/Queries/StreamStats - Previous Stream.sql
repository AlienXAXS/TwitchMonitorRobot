SELECT streams.start,
       streams.end INTO @START_DATE,
                        @END_DATE
FROM `streams`
WHERE `end` IS NOT NULL
ORDER BY `id` DESC
LIMIT 1;


SELECT @MODERATOR_CHAT_MESSAGES := count(*)
FROM users
INNER JOIN messages AS message ON message.userid = users.id
WHERE message.date BETWEEN @START_DATE AND @END_DATE
  AND NOT users.username LIKE '%bot%'
  AND users.ismod;


SELECT @TOTAL_CHAT_MESSAGES := count(*)
FROM users
INNER JOIN messages AS message ON message.userid = users.id
WHERE message.date BETWEEN @START_DATE AND @END_DATE
  AND NOT users.username LIKE '%bot%';


SELECT commands.command,
       count(*) AS magnitude INTO @MOST_USED_COMMAND,
                                  @MOST_USED_COMMAND_COUNT
FROM commands
WHERE commands.date BETWEEN @START_DATE AND @END_DATE
GROUP BY commands.command
ORDER BY magnitude DESC
LIMIT 1;


SELECT @START_DATE AS "Stream Start",
       @END_DATE AS "Stream End",
       CONCAT(HOUR(TIMEDIFF(@START_DATE, @END_DATE)), "h ", MINUTE(TIMEDIFF(@START_DATE, @END_DATE)), "m") AS "Stream Duration",
       @TOTAL_CHAT_MESSAGES AS "Total Chat Messages",
       @MODERATOR_CHAT_MESSAGES AS "Moderator Chat Messages",
       CONCAT(@MOST_USED_COMMAND, " (", @MOST_USED_COMMAND_COUNT, " times)") AS "Most Commonly Used Command"