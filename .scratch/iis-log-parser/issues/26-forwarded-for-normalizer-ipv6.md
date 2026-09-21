# 26 — `ForwardedForIpNormalizer` must not shred IPv6 addresses

**What to build:** `ForwardedForIpNormalizer.Normalize` split the `X-Forwarded-For` value on `,` **and** `:` and validated the first piece. The colon split exists for a real shape in the logs - an IPv4 with a port, e.g. `142.59.220.47:50169,10.87.7.4,+20.69.115.185` - but it also cut every IPv6 address at its first group: `2001:db8::1` became `2001`, which `IPAddress.TryParse` accepts as a numeric IPv4 address, so `2001` was stored as the client IP. Every IPv6 client sharing a first group collapsed into one by-forwarded-for-IP row and `::1` became `1`. Found by the code review after ticket 24.

**Blocked by:** none.

**Status:** done

## Behavior

After removing `+`, taking the first comma-separated value and trimming it:

1. **Bracketed IPv6** (`[addr]`, `[addr]:port`): the address inside the brackets, if it is a valid IPv6 address.
2. **Anything that is an IP address as a whole** (`IPAddress.TryParse`): kept as typed - IPv4, and IPv6 including `::1`, `fe80::1`, `::ffff:203.0.113.1`.
3. **Otherwise** the piece before the first `:`, but only if it is a dotted IPv4 address (`ip:port`, `ip:ip`; the previous behavior for the shapes that exist in the logs).
4. Anything else -> `"-"`. So an IPv6-looking value that fails validation (`2001:db8::zz`, `2001:zz`) is now `"-"` instead of its first group.

Absent (`""` / `"-"`) -> `127.0.0.1` and the 48-character truncation are unchanged. IPv6 text is **not** canonicalized (`2001:DB8::1` and `2001:db8::1` remain two keys); that would be a separate decision.

## Acceptance criteria

- [x] IPv6 addresses (`2001:db8::1`, a full form, `::1`, `fe80::1`, `::ffff:203.0.113.1`) are returned whole; two addresses sharing a first group stay distinct.
- [x] `[addr]`, `[addr]:port` -> the address; a comma list starting with an IPv6 address -> that address.
- [x] `203.0.113.1:8080`, `203.0.113.1:70.41.3.18` and the real `142.59.220.47:50169,10.87.7.4,+20.69.115.185` still yield the IPv4 address.
- [x] IPv6 lookalikes that fail validation and bad bracketed values give `"-"`; an overlong IPv6 value is truncated to 48 characters.
- [x] Build with 0 warnings; Domain suite passes (250 -> 267).

## Out of scope

- Canonicalizing IPv6 text; treating numeric-only tokens such as `2001` as invalid (a pre-existing quirk of `IPAddress.TryParse`, only reachable now by a bare token, not by IPv6 input).

## Comments

Latent in the current data, not active: sampling the `2026/` corpus (5 files across the year, plus every June file's colon-bearing forwarded-for values) found only the `ip:port,...` IPv4 shape and no IPv6 client addresses, so no existing aggregate row is known to be wrong. It would have started to bite the first time an IPv6 client appeared. Tests first (13 of the 25 then-existing cases red before the change).
