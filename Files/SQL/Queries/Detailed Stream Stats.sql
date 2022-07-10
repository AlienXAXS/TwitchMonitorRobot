SELECT streams.start INTO @START_DATE
FROM `streams`
WHERE `end` IS NULL
ORDER BY `id` DESC
LIMIT 1;

SELECT 
      SubQuery.Username as "UserName"
    , (CASE WHEN MAX(SubQuery.UserIsMod)=1 AND LOCATE('BOT',UPPER(SubQuery.Username))>0 THEN "HiveBot" 
        ELSE (CASE WHEN MAX(SubQuery.UserIsMod)=1 THEN "ModTeam" 
        ELSE "HiveMbr" 
      END) END) AS "UserType"
    , COUNT(SubQuery.MsgID) as "ChatsSent"
    , CAST(SUM(CASE WHEN SubQuery.MsgGrp='message' THEN 1 ELSE 0 END) AS INTEGER) as "MsgSent"
    , CAST(SUM(CASE WHEN SubQuery.MsgGrp='command' THEN 1 ELSE 0 END) AS INTEGER) as "CmdsSent"
    , CAST(SUM(CASE WHEN SubQuery.MsgGrp='redeem' THEN 1 ELSE 0 END) AS INTEGER) as "RedeemsMade"
    , CAST(SUM(CASE WHEN SubQuery.MsgGrp='redeem' THEN CAST(SubQuery.MsgBody AS INTEGER) ELSE 0 END) AS INTEGER) as "RedeemSpend"
    , CAST(SUM(CASE WHEN mod_commands.command<>'' THEN 1 ELSE 0 END) AS INTEGER) as "ModCmds"
    , MIN(SubQuery.MsgDate) as "1stMsgOfStream"
    , MAX(SubQuery.MsgDate) as "LastMsgOfStream"
    FROM (
        SELECT 
              commands.id as "MsgID"
            , commands.date as "MsgDate"
            , commands.userid as "UserID"
            , users.username as "UserName"
            , commands.command as "CmdRedeemUsed"
            , commands.parameters as "MsgBody"
            , users.ismod as "UserIsMod"
            , users.lastseen as "UserLastSeen"
            , 'command' as 'MsgGrp'
        FROM commands
        INNER JOIN users 
            ON commands.userid=users.id
        WHERE commands.date>=@START_DATE
        UNION 
        SELECT 
              messages.id as "MsgID"
            , messages.date as "MsgDate"
            , messages.userid as "UserID"
            , users.username as "UserName"
            , '' as "CmdRedeemUsed"
            , messages.message as "MsgBody"
            , users.ismod as "UserIsMod"
            , users.lastseen as "UserLastSeen"
            , 'message' as 'MsgGrp'
        FROM messages
        INNER JOIN users 
            ON messages.userid=users.id
        WHERE messages.date>=@START_DATE
        UNION
        SELECT 
              bit_redeems.id as "MsgID"
            , bit_redeems.date as "MsgDate"
            , bit_redeems.userid as "UserID"
            , users.username as "UserName"
            , bit_redeems.name as "CmdRedeemUsed"
            , bit_redeems.cost as "MsgBody"
            , users.ismod as "UserIsMod"
            , users.lastseen as "UserLastSeen"
            , 'redeem' as 'MsgGrp'
        FROM bit_redeems
        INNER JOIN users 
            ON bit_redeems.userid=users.id
        WHERE bit_redeems.date>=@START_DATE
    ) SubQuery
    LEFT JOIN mod_commands 
        ON SubQuery.CmdRedeemUsed = mod_commands.command
GROUP BY SubQuery.Username
ORDER BY COUNT(SubQuery.MsgID) DESC