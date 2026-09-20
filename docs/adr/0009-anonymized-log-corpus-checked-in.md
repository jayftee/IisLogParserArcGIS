---
status: accepted
---

# The `2026/` log corpus is checked in, with every person's identifier replaced

The Reports and Regression test suites run against a real-world IIS log corpus (the `2026/` folder: 720 files, about 257 MB, about 20 MB compressed). It used to be git-ignored and kept on one machine, so a fresh clone could not run those suites. Checking the raw logs in was rejected: they contain the identifiers of real people (staff, contractors and a few private email addresses) in request paths, query strings and referers, and version control is permanent.

We check in a scrubbed copy instead. Every identifier that names a person is replaced by a stable pseudonym of the form `userNNNNN@somewhere`: full email addresses (any case, and URL-encoded `%40`), the bare usernames that appear in `community/users/<name>` and `content/users/<name>` paths, `owner:<name>` filters and SQL-style `created_user = '<name>'` filters. The mapping is deterministic and one-to-one, so the same person is always the same pseudonym and two different people never merge. Everything else, including timestamps, client and forwarded-for IP addresses, URIs, sizes, status codes, service and root names and User-Agent strings, is left as it was, and the line and field structure of every file is unchanged. Bot contact addresses in User-Agent strings (`anthropic.com`, `moz.com`, `bytedance.com`, `microsoft.com`) are not people and are kept. Esri's own built-in accounts (`esri`, `esri_apps`, ...) are vendor accounts, not people.

The tests derive their expected values from the data they run on (independent SQL oracles, raw line counts), so a consistent replacement does not change any test outcome: the full suite passed with identical counts on the scrubbed copy.

Consequences:

- **The raw logs and the real-to-pseudonym mapping are never in the repository.** They are kept outside it, together with the scrubber, so the corpus can be regenerated or extended. The mapping is the only thing that can undo the scrub and must never be committed.
- **Client IP addresses are deliberately left in.** They are public addresses and the tests and aggregates depend on their structure; this was a conscious decision, not an oversight.
- **New identifier shapes need a new scrub.** If a future log export contains a form of identifier the scrubber does not know (it reports leftovers instead of hiding them), the scrubber is extended and re-run before the files are added.
- **Fixtures that name people use fictional pseudonyms.** Test code and tickets use `userNNNNN@somewhere`, never a real name.
