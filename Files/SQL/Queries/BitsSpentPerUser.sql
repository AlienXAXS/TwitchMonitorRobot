SELECT count(bit_redeems.name),
       SUM(cost) AS RedeemCost,
       username
FROM bit_redeems
LEFT JOIN users ON bit_redeems.userid = users.id
GROUP BY users.username