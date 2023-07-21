CREATE OR REPLACE VIEW get_billing_usage
AS

SELECT countMerge(events) as Count
FROM billing_usage_v1_mv
WHERE app_id IN ({p_app_ids:Array(String)})
AND year = {p_year:Int32}
AND month = {p_month:Int32}
