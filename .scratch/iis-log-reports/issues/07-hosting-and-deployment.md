Type: grilling
Status: resolved

## Question

Graduated from the map's "Hosting/deployment of the Regeneration Run's output" fog: where do the generated static files actually get served from for viewers to reach?

## Answer

No decision needed from this map — the user already has deployment infrastructure in place, and the static-HTML delivery choice (ADR-0005) makes the hosting target a non-issue architecturally: every page uses relative links, so navigating the generated output is identical whether it's opened from a plain shared folder or served by an IIS site. The Reports project's own job stops at producing the file tree; where that tree physically lands is the user's existing infrastructure concern, out of scope for this map to decide further.
