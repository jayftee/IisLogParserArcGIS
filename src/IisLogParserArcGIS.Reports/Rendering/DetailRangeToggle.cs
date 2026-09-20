using System.Net;

namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Renders the All / Last 7 Days in-page toggle every Detail Page shares (tickets 13, 16, 21): both ranges'
/// charts are embedded in the one page, the Last 7 Days variant sitting inert inside a <c>&lt;template&gt;</c>
/// until its button is first clicked, so no Google Chart is ever drawn into a hidden zero-size container.
/// </summary>
internal static class DetailRangeToggle
{
    /// <summary>
    /// The <c>data-range</c> value of the All range.
    /// </summary>
    public const string AllSlug = "all";

    /// <summary>
    /// The <c>data-range</c> value of the Last 7 Days range.
    /// </summary>
    public const string Last7DaysSlug = "last-7-days";

    /// <summary>
    /// Renders the toggle buttons, back link, both range panels, and the click-handling script.
    /// </summary>
    /// <param name="backHref">The page-relative href of the Back link (the section's Complete View).</param>
    /// <param name="allChartsHtml">The All range's chart markup, drawn immediately.</param>
    /// <param name="last7ChartsHtml">The Last 7 Days range's chart markup, drawn on first click.</param>
    /// <returns>The toggle's embeddable body HTML.</returns>
    public static string Render(string backHref, string allChartsHtml, string last7ChartsHtml)
    {
        ArgumentNullException.ThrowIfNull(backHref);
        ArgumentNullException.ThrowIfNull(allChartsHtml);
        ArgumentNullException.ThrowIfNull(last7ChartsHtml);

        return $$"""
            <div class="detail-range-toggle">
              <button type="button" class="range-toggle-button is-active" data-range="{{AllSlug}}">All</button>
              <button type="button" class="range-toggle-button" data-range="{{Last7DaysSlug}}">Last 7 Days</button>
              <a href="{{WebUtility.HtmlEncode(backHref)}}" class="range-toggle-button detail-back-link">Back</a>
            </div>
            <div class="detail-range-panel" data-range="{{AllSlug}}">
            {{allChartsHtml}}
            </div>
            <div class="detail-range-panel" data-range="{{Last7DaysSlug}}" hidden></div>
            <template class="detail-range-template" data-range="{{Last7DaysSlug}}">
            {{last7ChartsHtml}}
            </template>
            <script>
            (function () {
              var toggleButtons = document.querySelectorAll('.range-toggle-button');
              var panels = document.querySelectorAll('.detail-range-panel');
              var templates = document.querySelectorAll('.detail-range-template');
              var activatedRanges = {};

              toggleButtons.forEach(function (button) {
                button.addEventListener('click', function () {
                  var range = button.dataset.range;

                  if (!activatedRanges[range]) {
                    templates.forEach(function (template) {
                      if (template.dataset.range === range) {
                        document.querySelector('.detail-range-panel[data-range="' + range + '"]').appendChild(template.content.cloneNode(true));
                      }
                    });
                    activatedRanges[range] = true;
                  }

                  toggleButtons.forEach(function (b) { b.classList.toggle('is-active', b === button); });
                  panels.forEach(function (p) { p.hidden = p.dataset.range !== range; });
                });
              });
            })();
            </script>
            """;
    }
}
