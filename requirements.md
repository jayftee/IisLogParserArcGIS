# Requirement: IIS Log Parser for ArcGIS

This document defines how the log parsing occurs, the aggregate tables, parsing rules, and normalization behavior for a complete implementation.

## Scope

The implementation should store aggregate data only, not raw IIS log rows.

The implementation should process IIS logs in daily batches. For a given local day, it will:

- read each log line
- split and normalize the raw IIS fields
- generate all aggregate data for that day in memory
- persist the aggregate set for that day to the database as a complete daily batch

The aggregates requested are:

- by URI / endpoint
- by root path
- by user agent
- by referer
- forwarded-for IP
- referer + URI
- by ArcGIS Server service

---

## Shared rules across aggregates

- The input is a W3C IIS log line, split into fields by whitespace.
- Header lines beginning with `#` are ignored.
- A line is considered valid only if it has the minimum expected IIS log fields and field count in logs match header field count.
- `utc_date_time` stores the parsed IIS timestamp in UTC/GMT as part of the transient log-entry parsing result.
- `local_date_time` stores the same timestamp converted to the configured local time zone from the program configuration.
- `local_date` is derived from `local_date_time` as the local date-only value used for grouping.
- Aggregate grouping uses `local_date_time` only to derive `local_date`; aggregate rows do not reference `utc_date_time` and do not persist `local_date_time`.
- `time_taken_second` stores the total accumulated time taken in seconds across all hits in the grouped dimension.
- `hits` stores the total hit count for the grouped dimension.
- `id` is the primary key row identifier.
- Because the process runs daily, aggregate data for a given day is generated in memory as a fresh daily aggregate set and then committed as a batch.
- The daily aggregate set should be treated as replaceable for that local day rather than an incremental update process.

---

## Parsing contract for every log line

The parsing and normalization flow should be shared across all aggregate types:

1. Read the raw log line.
2. Split the line into IIS fields.
3. Normalize each required field into a structured request model.
4. Derive `utc_date_time` and `local_date_time` from the IIS `date` and `time` fields.
5. Derive `local_date` from `local_date_time` for all grouping.
6. Generate all aggregate update objects from the normalized request model.
7. Accumulate those updates in memory for the entire local day.
8. Commit the resulting set of daily aggregate rows to the database.

This shared parse-and-normalize step is intentionally centralized so each aggregate does not re-implement its own parsing rules.

The line is parsed from the W3C IIS fields in this order:

1. `date`
2. `time`
3. `s-ip`
4. `cs-method`
5. `cs-uri-stem`
6. `cs-uri-query`
7. `s-port`
8. `cs-username`
9. `c-ip`
10. `cs(User-Agent)`
11. `cs(Referer)`
12. `sc-status`
13. `sc-substatus`
14. `sc-win32-status`
15. `time-taken`
16. `X-Forwarded-For`

The following fields are required for the aggregate logic:

- `date` and `time` are combined into a `utc_date_time` value in UTC/GMT
- the configured local time zone from the program configuration is applied to derive `local_date_time`
- the local `local_date` value used for grouping is derived from `local_date_time`
- aggregate rows use `local_date` for grouping and do not reference `utc_date_time`
- `cs-uri-stem` is the basis for the URI and root aggregates
- `cs(Referer)` is the basis for the referer aggregates
- `cs(User-Agent)` is the basis for the user-agent aggregate
- `X-Forwarded-For` is the basis for the forwarded-for aggregate
- `time-taken` is converted from milliseconds to seconds for the aggregate metrics

---

## 1) Aggregate by URI / endpoint

Source entity: `AggregatedByUri`

### Parsing logic

- Read `cs-uri-stem` from the IIS log line.
- Normalize the value by:
  - replacing backslashes with `/`
  - ensuring it starts with `/`
  - truncating to 1024 characters if needed
- Use the normalized `uri_stem` as the aggregate key.
- Derive `local_date` from `local_date_time` and use that as the grouping date.
- Convert `time-taken` from milliseconds to seconds and add it to the running total for the `(local_date, uri_stem)` group.
- Increment `hits` by 1 for that `(local_date, uri_stem)` group.

### Columns:

- `id` — primary key
- `local_date` — local date-only aggregation date derived from `local_date_time`
- `uri_stem` — URI stem, e.g. `/portal/sharing/rest/search`
- `time_taken_second` — total accumulated time taken in seconds across all hits for the group
- `hits` — total hits for that URI on that date

### Constraints:

- `uri_stem` must start with `/` and must not contain backslashes
- `time_taken_second` must be greater than 0
- `hits` must be greater than 0

---

## 2) Aggregate by root path

Source entity: `AggregatedByRoot`

### Parsing logic

- Read `cs-uri-stem` from the IIS log line.
- Split the URI stem on `/` and remove empty segments.
- Take the first remaining path segment as the root.
- Normalize the root by:
  - lowercasing it
  - converting backslashes to `/`
  - trimming spaces
  - truncating to 16 characters if needed
- If no path segment is found, use `-`.
- Derive `local_date` from `local_date_time` and use that as the grouping date.
- Convert `time-taken` from milliseconds to seconds and add it to the running total for the `(local_date, root)` group.
- Increment `hits` by 1 for that `(local_date, root)` group.

### Columns:

- `id` — primary key
- `local_date` — local date-only aggregation date derived from `local_date_time`
- `root` — root path value
- `time_taken_second` — total accumulated time taken in seconds across all hits for the group
- `hits` — total hits for that root on that date

### Constraints:

- `root` max length is 16
- `time_taken_second` must be greater than 0
- `hits` must be non-negative

---

## 3) Aggregate by user agent

Source entity: `AggregatedByUserAgent`

### Parsing logic

- Read `cs(User-Agent)` from the IIS log line.
- Normalize the value by:
  - replacing `+` with a space
  - lowercasing the string
  - trimming whitespace
  - truncating to 1024 characters if needed
- Use the normalized user-agent string as the aggregate key.
- Derive `local_date` from `local_date_time` and use that as the grouping date.
- Convert `time-taken` from milliseconds to seconds and add it to the running total for the `(local_date, user_agent)` group.
- Increment `hits` by 1 for that `(local_date, user_agent)` group.

### Columns:

- `id` — primary key
- `local_date` — local date-only aggregation date derived from `local_date_time`
- `user_agent` — full user-agent string
- `time_taken_second` — total accumulated time taken in seconds across all hits for the group
- `hits` — total hits for that user agent on that date

### Constraints:

- `user_agent` max length is 1024
- `time_taken_second` must be greater than 0
- `hits` must be non-negative

---

## 4) Aggregate by referer

Source entity: `AggregatedByReferer`

### Parsing logic

- Read `cs(Referer)` from the IIS log line.
- If the field is empty or `-`, represent the referer as `-`.
- Otherwise normalize the value by:
  - replacing `+` with a space
  - lowercasing the string
  - trimming whitespace
  - truncating to 4096 characters if needed
- Use the normalized referer string as the aggregate key.
- Derive `local_date` from `local_date_time` and use that as the grouping date.
- Convert `time-taken` from milliseconds to seconds and add it to the running total for the `(local_date, referer)` group.
- Increment `hits` by 1 for that `(local_date, referer)` group.

### Columns:

- `id` — primary key
- `local_date` — local date-only aggregation date derived from `local_date_time`
- `referer` — referer URL string
- `time_taken_second` — total accumulated time taken in seconds across all hits for the group
- `hits` — total hits for that referer on that date

### Constraints:

- `referer` max length is 4096
- `time_taken_second` must be greater than 0
- `hits` must be non-negative

---

## 5) Aggregate by forwarded-for IP

Source entity: `AggregatedByForwardedForIp`

### Parsing logic

- Read `X-Forwarded-For` from the IIS log line.
- If the field is empty or `-`, use the loopback placeholder value.
- Otherwise normalize the value by:
  - removing `+`
  - taking only the first IP from the comma-separated or colon-split value list
  - trimming whitespace
  - truncating to 512 characters if needed
- Validate that the resulting value is a valid IP address.
- Use the normalized forwarded-for IP as the aggregate key.
- Derive `local_date` from `local_date_time` and use that as the grouping date.
- Convert `time-taken` from milliseconds to seconds and add it to the running total for the `(local_date, forwarded_for_ip)` group.
- Increment `hits` by 1 for that `(local_date, forwarded_for_ip)` group.

### Columns:

- `id` — primary key
- `local_date` — local date-only aggregation date derived from `local_date_time`
- `forwarded_for_ip` — forwarded-for client IP
- `time_taken_second` — total accumulated time taken in seconds across all hits for the group
- `hits` — total hits for that forwarded-for IP on that date

### Constraints:

- `forwarded_for_ip` must be a valid IP address or `-`
- `forwarded_for_ip` max length is 48
- `time_taken_second` must be greater than 0
- `hits` must be non-negative

---

## 6) Aggregate by referer + URI

Source entity: `AggregatedByRefererAndUri`

**Optional (ticket 28):** this aggregate is computed only when the `ComputeByRefererAndUri` setting is `true` (default `false`). The table is always created, but with the setting off a harvest neither writes to it nor deletes rows an earlier run stored.

### Parsing logic

- Read `cs(Referer)` and `cs-uri-stem` from the IIS log line.
- Normalize the referer exactly as in the referer aggregate.
- Normalize the URI stem exactly as in the URI aggregate.
- Use the normalized referer and normalized URI stem together as the aggregate key.
- Derive `local_date` from `local_date_time` and use that as the grouping date.
- Convert `time-taken` from milliseconds to seconds and add it to the running total for the `(local_date, referer, uri_stem)` group.
- Increment `hits` by 1 for that `(local_date, referer, uri_stem)` group.

### Columns:

- `id` — primary key
- `local_date` — local date-only aggregation date derived from `local_date_time`
- `referer` — referer URL string
- `uri_stem` — URI stem
- `time_taken_second` — total accumulated time taken in seconds across all hits for the group
- `hits` — total hits for that referer + URI combination on that date

### Constraints:

- `referer` max length is 4096
- `uri_stem` must start with `/` and must not contain backslashes
- `time_taken_second` must be greater than 0
- `hits` must be non-negative

---

## 7) Aggregate by ArcGIS Server service

Source entity: `AggregatedByArcGisService`

### Parsing logic

- Read `cs-uri-stem` from the IIS log line.
- Only process requests whose normalized path is an actual ArcGIS Server REST service call in the form `/firstword/rest/services/...`, where:
  - `firstword` is the site/root value
  - the second path segment is `rest`
  - the third path segment is `services`
- Ignore query-string content when identifying the path shape; a URL such as `/test/service/?foo=bar` must not be treated as a valid ArcGIS REST service request just because it contains similar words.
- Exclude `/firstword/admin/*` and `/portal/*` requests from this aggregate.
- Normalize the URI stem by replacing backslashes with `/` and ensuring it starts with `/`.
- Parse the ArcGIS service request as follows:
  - `site` is the first path segment after the leading `/` and corresponds to the existing `root` concept.
  - The path must be validated as `/site/rest/services/...` before any further parsing is attempted.
  - `folder` is the service folder segment when present.
  - `service_name` is the segment immediately before `service_type`.
  - `service_type` is the final ArcGIS service type segment before the operation, such as `MapServer`, `FeatureServer`, `GeocodeServer`, `GPServer`, `ImageServer`, or any other ArcGIS service type.
- Support both foldered and folderless ArcGIS REST service URLs:
  - foldered: `/mimas/rest/services/wildfire/firemap/MapServer/export`
    - `site = mimas`
    - `folder = wildfire`
    - `service_name = firemap`
    - `service_type = MapServer`
  - folderless: `/mimas/rest/services/watermap/FeatureServer/export`
    - `site = mimas`
    - `folder = empty / null`
    - `service_name = watermap`
    - `service_type = FeatureServer`
- Ignore the operation segment such as `export`, `query`, `layers`, or `applyEdits` for this aggregate key.
- Derive `local_date` from `local_date_time` and use that as the grouping date.
- Convert `time-taken` from milliseconds to seconds and add it to the running total for the `(local_date, site, folder, service_name, service_type)` group.
- Increment `hits` by 1 for that group.
- Increment `successful_hits` when `sc-status` is a successful status code, and increment `failed_hits` when `sc-status` is a failed status code.

### Columns:

- `id` — primary key
- `local_date` — local date-only aggregation date derived from `local_date_time`
- `site` — ArcGIS Server site/root value, equivalent to the existing `root` concept
- `folder` — ArcGIS service folder, or empty / null when the service has no folder
- `service_name` — ArcGIS service name
- `service_type` — ArcGIS service type such as `MapServer`, `FeatureServer`, `GeocodeServer`, `GPServer`, `ImageServer`, or any other ArcGIS service type segment returned by the parser
- `time_taken_second` — total accumulated time taken in seconds across all hits for the group
- `hits` — total hits for that service group on that date
- `successful_hits` — count of hits whose `sc-status` is considered successful
- `failed_hits` — count of hits whose `sc-status` is considered failed

### Success and failure rules

- Successful status codes are those with `sc-status` less than `400`, meaning `2xx`, `3xx`, and other non-error responses are counted as successful.
- Failed status codes are those with `sc-status` greater than or equal to `400`, meaning `4xx` and `5xx` responses are counted as failed.
- `hits` must equal `successful_hits + failed_hits` for each row.

### Constraints:

- `site` must be populated and should match the existing root normalization rules
- `folder` may be empty for folderless ArcGIS services
- `service_name` and `service_type` must be populated for valid ArcGIS REST service requests
- `time_taken_second` must be greater than 0
- `hits`, `successful_hits`, and `failed_hits` must be non-negative

---

## Proposed requirement summary

The fresh implementation should support these aggregate tables with the following logical schema:

- `aggregated_by_uri`
  - `id`
  - `local_date`
  - `uri_stem`
  - `time_taken_second`
  - `hits`

- `aggregated_by_root`
  - `id`
  - `local_date`
  - `root`
  - `time_taken_second`
  - `hits`

- `aggregated_by_user_agent`
  - `id`
  - `local_date`
  - `user_agent`
  - `time_taken_second`
  - `hits`

- `aggregated_by_referer`
  - `id`
  - `local_date`
  - `referer`
  - `time_taken_second`
  - `hits`

- `aggregated_by_forwarded_for_ip`
  - `id`
  - `local_date`
  - `forwarded_for_ip`
  - `time_taken_second`
  - `hits`

- `aggregated_by_referer_and_uri`
  - `id`
  - `local_date`
  - `referer`
  - `uri_stem`
  - `time_taken_second`
  - `hits`

- `aggregated_by_arcgis_service`
  - `id`
  - `local_date`
  - `site`
  - `folder`
  - `service_name`
  - `service_type`
  - `time_taken_second`
  - `hits`
  - `successful_hits`
  - `failed_hits`

---

## Notes

- This is a requirements extraction from the original analyzer, not a direct code reuse.
- The original solution stored aggregates as daily totals keyed by one or more dimensions.
- The fresh implementation can keep the same aggregate columns and rename the tables if desired.
- For reruns of the same local day, the implementation should replace the existing daily aggregate set for that `local_date` rather than insert duplicate rows.
- The aggregate tables are intended to be regenerated daily from normalized log data, not incrementally updated row by row.
