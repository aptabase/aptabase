SELECT countMerge(events) as Count
FROM billing_usage_v1_mv
WHERE app_id IN ({app_ids})
AND year = {year}
AND month = {month}
