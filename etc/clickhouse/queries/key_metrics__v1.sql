SELECT uniqExact(user_id) / (date_diff('day', min(min), max(max)) + 1) as DailyUsers,
       uniqExact(session_id) as Sessions,
       if(isNaN(median(max - min)), 0, median(max - min)) as DurationSeconds,
       sum(count) as Events
FROM (
    SELECT min(timestamp) AS min,
           max(timestamp) AS max,
           user_id,
           session_id,
           count(*) AS count
    FROM events
    PREWHERE app_id = {app_id}
    AND (timestamp >= {date_from} OR {date_from} IS NULL)
    AND (timestamp < {date_to} OR {date_to} IS NULL)
    AND (country_code = {country_code} OR {country_code} IS NULL)
    AND (event_name = {event_name} OR {event_name} IS NULL)
    AND (os_name = {os_name} OR {os_name} IS NULL)
    AND (app_version = {app_version} OR {app_version} IS NULL)
    GROUP BY user_id, session_id
)