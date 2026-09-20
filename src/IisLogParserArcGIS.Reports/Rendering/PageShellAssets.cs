namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Writes the shared static assets every Dashboard page's shell references: the stylesheet, and (ticket 20)
/// <c>dashboard.js</c>, the debounced window-resize handler every Google Chart's own inline script registers
/// itself into (see <see cref="GoogleChartsRenderer"/>) so a chart repaints at its card's new width on resize,
/// rather than staying frozen at whatever width it first drew at.
/// </summary>
public static class PageShellAssets
{
    private const string SiteCss = """
        :root {
          color-scheme: light;
          /* Approved brand palette (Stone/Sky/Dawn/Sunset/Prairie/Pasture). Sky family carries the sidebar's
             header/hover/highlight so all three share one hue instead of an unrelated ad-hoc blue. */
          --muted: #6b7280;
          --line: #d7dbe0;
          --brand-dark: #002c4e;                    /* Sky Dark (7463 CP) */
          --text-default: #545860;                  /* Stone Mid (Cool Grey 11C) */
          --text-highlight: #0077cd;                 /* Sky Mid (3005 CP) - distinct hue+saturation from text-default, not just darker */
          --highlight-bg: #9ad7f9;                   /* Sky Light (2975 CP), exact swatch from docs/ColorPalette.png */
          --header-bg: var(--highlight-bg);          /* the header uses the exact same value as hover, tied to one variable */
          --header-fg: var(--brand-dark);
        }
        * { box-sizing: border-box; }
        /* body is a flex column so .dashboard-layout (flex: 1 0 auto) grows to fill any leftover viewport
           height - pinning the footer to the bottom of the screen on a short page - without ever shrinking
           below its own content's height, so a tall page just pushes the footer down and scrolls normally. */
        body { margin: 0; min-height: 100vh; display: flex; flex-direction: column; font-family: system-ui, -apple-system, Segoe UI, Arial, sans-serif; color: #1a1a1a; background: #eef0f2; }
        .dashboard-header { padding: 0.75rem 1.25rem; background: var(--header-bg); border-bottom: 1px solid var(--line); flex: none; }
        .dashboard-header .brand { color: var(--header-fg); text-decoration: none; font-weight: 700; font-size: 1.1rem; }
        /* min-height: 0 on every nested flex container in this chain - a flex item defaults to never shrinking
           below its own content's natural height, which otherwise lets a growing chart card's content push
           .dashboard-layout (and the whole page) taller than the viewport instead of being capped by it. */
        .dashboard-layout { display: flex; align-items: stretch; flex: 1 0 auto; min-height: 0; }
        /* padding-right insets every row's own box - including its hover/active background, not just its
           content - from the border, so the highlight never touches it regardless of nesting depth. */
        .dashboard-sidebar { width: 260px; flex-shrink: 0; background: #fff; border-right: 1px solid var(--line); padding: 1rem 0.6rem 1rem 0; }
        .dashboard-sidebar ul { list-style: none; margin: 0; padding-left: 0.9rem; }
        .dashboard-sidebar > ul { padding-left: 0.75rem; }
        .dashboard-sidebar li { margin: 0.05rem 0; }
        .dashboard-sidebar a, .dashboard-sidebar summary {
          display: flex; align-items: center; justify-content: space-between; gap: 0.5rem;
          /* Asymmetric on purpose: the chevron sits on the right now (space-between), so the row needs a bit
             more breathing room there than on the left, where nesting already indents. */
          padding: 0.35rem 0.6rem 0.35rem 0.4rem; border-radius: 6px; text-decoration: none;
          color: var(--text-default); font-weight: 700; font-size: 0.88rem; cursor: pointer;
          transition: background-color 120ms ease, color 120ms ease;
        }
        .dashboard-sidebar summary { list-style: none; }
        .dashboard-sidebar summary::-webkit-details-marker { display: none; }
        /* Chevron on ::after, pushed to the row's right edge by justify-content: space-between - every row's
           edge lines up regardless of nesting depth, since only the left indent (nested <ul> padding) varies. */
        .dashboard-sidebar summary::after {
          content: ""; display: inline-block; width: 6px; height: 6px;
          border-right: 2px solid currentColor; border-bottom: 2px solid currentColor;
          transform: rotate(-45deg); flex: none; opacity: 0.6; transition: transform 150ms ease;
        }
        .dashboard-sidebar details[open] > summary::after { transform: rotate(45deg); }
        .dashboard-sidebar details[open] > summary.is-open::after { opacity: 1; }
        /* Hover: background only, font color is left exactly as it already is - a plain item stays
           text-default on hover, an already-open/active item stays text-highlight. Shares the active state's
           background, so clicking an item just makes its hover background persist plus its font color change. */
        .dashboard-sidebar a:hover, .dashboard-sidebar summary:hover { background: var(--highlight-bg); }
        .dashboard-sidebar details[open] > summary.is-open { color: var(--text-highlight); }
        .dashboard-sidebar a.active { background: var(--highlight-bg); color: var(--text-highlight); }
        .dashboard-sidebar a.active:hover { background: var(--highlight-bg); }
        .dashboard-content { flex: 1; padding: 1.5rem; min-width: 0; min-height: 0; display: flex; flex-direction: column; }
        /* Direct-child only: a single-widget page's title card + chart/table div sit right under
           .dashboard-content with nothing else, so it can grow to fill the leftover height (min-height keeps it
           usable on a short viewport). A Detail Page's charts sit inside .detail-range-panel instead (never a
           direct child here), so multi-chart pages are untouched. flex-basis: 0 (not auto) is deliberate -
           verified via headless measurement that auto's "preferred size" baseline, four flex levels deep (chart
           in content in layout in body), let this card's actual height creep past .dashboard-layout's own
           explicit height, overflowing the viewport by the excess. A Table doesn't read its container's height
           at draw time the way a chart does, so growing .chart-table just leaves blank space below its rows
           within the same card - no equivalent clientHeight-vs-padding bug to worry about there. */
        .dashboard-content > .chart-line, .dashboard-content > .chart-annotated-time-line, .dashboard-content > .chart-table, .dashboard-content > .chart-bar { flex: 1 1 0; min-height: 320px; }
        /* .chart's own margin-bottom exists to space multiple stacked charts apart (Detail Pages); on a page
           with just one, it doubles up with .dashboard-content's own bottom padding, making the gap below the
           chart bigger than the one above the title card. :last-child alone doesn't catch this - a chart's own
           trailing <script> tag is the real DOM-last child, not the chart div - so this looks past any later
           sibling (script, or another chart on a Detail Page) for a later .chart specifically. */
        .chart:not(:has(~ .chart)) { margin-bottom: 0; }
        .dashboard-footer { padding: 0.75rem 1.25rem; font-size: 0.8rem; color: #6b7280; background: #fff; border-top: 1px solid #d7dbe0; flex: none; }
        .dashboard-footer p { margin: 0.1rem 0; }
        .chart {
          background: #fff; border-radius: 10px; padding: 1.25rem; margin-bottom: 1.5rem;
          box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08), 0 1px 2px rgba(15, 23, 42, 0.06);
        }
        .chart-empty { display: flex; align-items: center; justify-content: center; color: var(--muted); font-size: 0.95rem; }
        .chart-title-card {
          background: #f8e19a; border-radius: 10px; padding: 0.9rem 1.25rem; margin-bottom: 1rem;
          box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08), 0 1px 2px rgba(15, 23, 42, 0.06);
        }
        .chart-title-card h2 { margin: 0; font-size: 1.05rem; font-weight: 700; color: #000; line-height: 1.4; }
        .detail-range-toggle { margin-bottom: 1rem; display: flex; align-items: center; }
        .range-toggle-button {
          padding: 0.35rem 0.9rem; margin-right: 0.5rem; border: 1px solid #d7dbe0; border-radius: 4px;
          background: #fff; cursor: pointer; font: inherit; display: inline-block; color: inherit; text-decoration: none;
        }
        .range-toggle-button.is-active, .range-toggle-button:hover { background: var(--brand-dark); color: #fff; border-color: var(--brand-dark); }
        /* Back is a navigation action, not part of the All/Last 7 Days state toggle it sits beside - pushed to
           the row's far right (margin-left: auto on a flex row) keeps that distinction visible instead of
           reading as a third, equal option in the same group. */
        .detail-back-link { margin-left: auto; margin-right: 0; }
        """;

    /// <summary>
    /// Redraws every registered chart, debounced, on window resize - a Google Chart lays itself out once, at
    /// whatever width its container had when it first drew, and never revisits that on its own. Each chart's own
    /// inline script (see <see cref="GoogleChartsRenderer"/>) pushes its own no-argument redraw closure onto
    /// <c>window.__dashboardCharts</c> right after drawing itself the first time.
    /// </summary>
    private const string DashboardJs = """
        window.__dashboardCharts = [];

        // A chart's container (.chart) has its own CSS padding - clientHeight includes that padding, but a
        // Google Chart drawn at exactly clientHeight sits *inside* the padding too, so the container ends up
        // needing height + padding, overflowing its own box by the padding amount. Every chart render method
        // calls this instead of reading clientHeight directly, so the drawn chart actually fits.
        window.__chartContentHeight = function (container, fallback) {
          var style = getComputedStyle(container);
          var verticalPadding = parseFloat(style.paddingTop) + parseFloat(style.paddingBottom);
          var available = container.clientHeight - verticalPadding;
          return available > 0 ? available : fallback;
        };

        // A flex item's explicit flex-basis (e.g. .chart-bar's 0) always wins over its height property, so a
        // plain inline height does nothing while it stands - the container just stays at whatever flex-grow
        // gave it, and content taller than that silently spills out instead of the box actually growing.
        // flex: none resets flex-basis to auto (deferring to height) and turns off grow/shrink, so a chart
        // that needs more room than the fill-available space can actually become that tall. `height` is a
        // content height (the same value handed to Google as options.height), but every element is
        // box-sizing: border-box, so container.style.height sets the border-box - without adding the
        // padding back here, the container ends up padding-short of what the chart draws, and the drawn
        // SVG pokes out past the card's own bottom edge by exactly that padding amount.
        window.__growChartContainerIfNeeded = function (container, height, available) {
          if (height > available) {
            var style = getComputedStyle(container);
            var verticalPadding = parseFloat(style.paddingTop) + parseFloat(style.paddingBottom);
            container.style.flex = 'none';
            container.style.height = (height + verticalPadding) + 'px';
          } else {
            container.style.flex = '';
            container.style.height = '';
          }
        };

        // A BarChart's fixed chartArea.width leaves the same fraction of the container for row labels no
        // matter how long those labels actually are - too little for a long ArcGIS service path (Google
        // truncates it with an ellipsis) while leaving unused space past the bars' own right edge for a
        // short one. Measuring the longest label with a canvas 2d context (the same technique the browser
        // itself uses to lay out text) gives a first-guess pixel width, so the left margin starts close to
        // right instead of guessing a percentage. Capped at maxFraction of the container so one pathological
        // label can't starve the bars entirely - that one row still gets ellipsis-truncated, same as before.
        window.__barChartLeftMargin = function (labels, container, maxFraction) {
          var canvas = window.__dashboardMeasureCanvas || (window.__dashboardMeasureCanvas = document.createElement('canvas'));
          var ctx = canvas.getContext('2d');
          ctx.font = '16px Arial, sans-serif';
          var maxLabelWidth = 0;
          for (var i = 0; i < labels.length; i++) {
            var width = ctx.measureText(labels[i]).width;
            if (width > maxLabelWidth) { maxLabelWidth = width; }
          }
          var needed = Math.ceil(maxLabelWidth) + 24;
          var cap = container.clientWidth * (maxFraction || 0.5);
          return Math.min(needed, cap);
        };

        // The canvas measurement above is only ever a guess: Google draws vAxis category labels with its own
        // SVG text layout, which can come out wider than a canvas 2d context's measureText predicted - a gap
        // that showed up in practice (font substitution, browser zoom, and subpixel rounding all nudge it) and
        // left some labels truncated even after sizing the margin to the measured "widest" one. Rather than
        // chase exact parity with Google's own text metrics, this checks the chart Google actually drew for
        // any label it truncated (its ellipsis character in a vAxis <text>) and, if it finds one, redraws once
        // more at the full cap this chart is allowed (the same maxFraction __barChartLeftMargin used) instead
        // of nudging the margin a few pixels at a time - a truncated label means the estimate was wrong, not
        // slightly short, so jumping straight to the max available room converges in one extra draw instead of
        // guessing how many small steps it'll take. If it's still truncated at the cap, that one label
        // genuinely doesn't fit without starving the bars, and stays truncated by design.
        window.__fixTruncatedBarLabels = function (chart, data, options, container, maxFraction) {
          var svg = container.querySelector('svg');
          if (!svg) { return; }
          var texts = svg.querySelectorAll('text');
          var truncated = false;
          for (var i = 0; i < texts.length; i++) {
            if (texts[i].textContent.indexOf('…') !== -1) { truncated = true; break; }
          }
          var cap = container.clientWidth * (maxFraction || 0.5);
          if (!truncated || options.chartArea.left >= cap) { return; }
          options.chartArea.left = cap;
          chart.draw(data, options);
        };

        // The nested-flex chain (body > .dashboard-layout > .dashboard-sidebar/.dashboard-content) approximates
        // "fill the viewport below the header, above the footer" well enough for the sidebar/footer, but wasn't
        // landing pixel-exact once a chart card inside .dashboard-content also started flex-growing (ticket 20).
        // Measuring the header/footer's real rendered heights and setting .dashboard-layout's min-height from
        // that directly is exact regardless of how tall anything inside it ends up wanting to be.
        function __fitDashboardLayout() {
          var header = document.querySelector('.dashboard-header');
          var footer = document.querySelector('.dashboard-footer');
          var layout = document.querySelector('.dashboard-layout');
          if (!header || !footer || !layout) { return; }
          var available = window.innerHeight - header.offsetHeight - footer.offsetHeight;
          layout.style.minHeight = Math.max(available, 0) + 'px';
        }

        document.addEventListener('DOMContentLoaded', __fitDashboardLayout);

        (function () {
          var resizeTimer;
          window.addEventListener('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
              __fitDashboardLayout();
              window.__dashboardCharts.forEach(function (redraw) { redraw(); });
            }, 150);
          });
        })();
        """;

    /// <summary>
    /// Writes every shared asset under <paramref name="outputDirectory"/>, creating an <c>assets</c>
    /// subdirectory as needed.
    /// </summary>
    /// <param name="outputDirectory">The Dashboard's output root directory.</param>
    public static void WriteTo(string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(outputDirectory);

        var assetsDirectory = Path.Combine(outputDirectory, "assets");
        Directory.CreateDirectory(assetsDirectory);
        File.WriteAllText(Path.Combine(assetsDirectory, "site.css"), SiteCss);
        File.WriteAllText(Path.Combine(assetsDirectory, "dashboard.js"), DashboardJs);
    }
}
