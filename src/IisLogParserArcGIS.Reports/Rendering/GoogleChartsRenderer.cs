using System.Globalization;
using System.Net;
using System.Text.Json;

namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Renders the inline <c>&lt;script&gt;</c> blocks that draw a Google Chart from data embedded directly in the
/// page (per ticket 01: data is embedded inline as JS, not fetched from a shared JSON file). Loaded from Google's
/// CDN via the standard <c>google.charts.load</c> + <c>setOnLoadCallback</c> pattern on every chart-bearing page.
/// Each chart's <c>redraw</c> closure is also registered into the shared <c>window.__dashboardCharts</c> array
/// (see <c>PageShellAssets</c>'s <c>dashboard.js</c>), so every chart on the page - a card's own resize doesn't
/// otherwise reach a fixed-layout Google Chart - repaints at its new width on window resize (ticket 20).
/// Every value serialized here ultimately derives from request data (a URI's folder/service/type segments, a
/// user agent, a referer) with no character allow-listing at ingest time, so every <c>JsonSerializer.Serialize</c>
/// call in this class deliberately keeps the default, HTML-safe encoder (which escapes <c>&lt;</c>/<c>&gt;</c>/
/// <c>&amp;</c> as <c>\uXXXX</c>) rather than a relaxed one - a JS engine reads those escapes identically to the
/// literal characters, so nothing is lost at runtime, and this is the only thing standing between a crafted
/// request path and a <c>&lt;/script&gt;</c>-breaking payload landing in a page nobody authenticates to view
/// (per ADR-0006).
/// </summary>
public static class GoogleChartsRenderer
{
    /// <summary>
    /// The row count per page for a <see cref="RenderTable"/> call with <see cref="TablePaging.Enabled"/>.
    /// </summary>
    private const int TablePageSize = 50;

    /// <summary>
    /// Renders a multi-series <c>google.visualization.LineChart</c> - the chart type ticket 01 reserves for the
    /// Summary section's all-Roots-overlaid total-requests chart. An empty <paramref name="data"/> (a real
    /// site/range with no qualifying hits at all) renders a plain "no data" card instead of attempting to draw -
    /// same reasoning as <see cref="RenderBarChart"/>: Google's own validation rejects a zero-row table outright
    /// rather than drawing an empty chart (ticket 20).
    /// </summary>
    /// <param name="elementId">The HTML id of the <c>&lt;div&gt;</c> the chart draws into.</param>
    /// <param name="data">The chart's explicitly-typed columns and row-major data.</param>
    /// <param name="title">The chart's own title, shown in its own card directly above the chart's card.</param>
    /// <returns>The title card, the chart's container element, and its bootstrap script, ready to embed in a page body.</returns>
    public static string RenderLineChart(string elementId, LineChartData data, string title)
    {
        ArgumentNullException.ThrowIfNull(elementId);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(title);

        var titleCardHtml = RenderTitleCard(title);

        if (data.Rows.Count == 0)
        {
            return $"""{titleCardHtml}<div class="chart chart-line chart-empty">No data for this range.</div>""";
        }

        var addColumnStatements = string.Join(
            "\n",
            data.Columns.Select(column => $"  data.addColumn({JsonSerializer.Serialize(column.DataType)}, {JsonSerializer.Serialize(column.Label)});"));
        var rowsJson = JsonSerializer.Serialize(data.Rows);

        return $$"""
            {{titleCardHtml}}
            <div id="{{elementId}}" class="chart chart-line"></div>
            <script>
            google.charts.load('current', { packages: ['corechart'] });
            google.charts.setOnLoadCallback(function () {
              var data = new google.visualization.DataTable();
            {{addColumnStatements}}
              data.addRows({{rowsJson}});
              // Fixed pixel chartArea insets, not Google's percentage-based defaults - a percentage margin scales
              // up right along with a container that's now free to grow tall (ticket 20), leaving more and more
              // blank space around the plot instead of the plot actually using the extra room.
              var options = { legend: { position: 'right' }, chartArea: { left: 60, top: 24, right: 170, bottom: 80 } };
              var container = document.getElementById('{{elementId}}');
              var chart = new google.visualization.LineChart(container);
              function redraw() { options.height = window.__chartContentHeight(container, 480); chart.draw(data, options); }
              redraw();
              window.__dashboardCharts.push(redraw);
            });
            </script>
            """;
    }

    /// <summary>
    /// Renders a single-series <c>google.visualization.AnnotatedTimeLine</c> - the chart type ticket 01 reserves
    /// for every single-series time chart (every ArcGIS Server, Portal, and Detail Page evolution chart).
    /// Requires a real JavaScript <c>Date</c> per point, not merely a formatted string, for its zoom/range-
    /// selector controls to work - so, unlike <see cref="RenderLineChart"/>, rows are built as hand-written
    /// <c>new Date(y, m, d)</c> literals rather than JSON-serialized. An empty <paramref name="data"/> (a real
    /// site/range with no qualifying hits at all) renders a plain "no data" card instead of attempting to draw -
    /// the same "Table has no rows." error <see cref="RenderBarChart"/> guards against for a zero-row
    /// <c>arrayToDataTable</c>, surfaced here too since <c>AnnotatedTimeLine</c> shares the same underlying core
    /// validation regardless of widget type (ticket 20).
    /// </summary>
    /// <param name="elementId">The HTML id of the <c>&lt;div&gt;</c> the chart draws into.</param>
    /// <param name="data">The chart's dates, values, and series label.</param>
    /// <param name="title">The chart's own title, shown in its own card directly above the chart's card.</param>
    /// <param name="tone">
    /// Whether the line draws in Google Charts' own default color or in red, for an evolution chart scoped to a
    /// failed outcome (ticket 20). Defaults to <see cref="ChartTone.Default"/>.
    /// </param>
    /// <returns>The title card, the chart's container element, and its bootstrap script, ready to embed in a page body.</returns>
#pragma warning disable CC0042 // Four independent rendering inputs (id, data, title, tone); bundling would just repackage them without benefit.
    public static string RenderAnnotatedTimeLine(string elementId, AnnotatedTimeLineData data, string title, ChartTone tone = ChartTone.Default)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(elementId);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(title);

        var titleCardHtml = RenderTitleCard(title);

        if (data.Dates.Count == 0)
        {
            return $"""{titleCardHtml}<div class="chart chart-annotated-time-line chart-empty">No data for this range.</div>""";
        }

        var rowsJs = string.Join(",\n    ", data.Dates.Select((date, index) => FormatRow(date, data.Values[index])));
        var seriesLabelJson = JsonSerializer.Serialize(data.SeriesLabel);
        var colorsOption = ColorsOption(tone);

        return $$"""
            {{titleCardHtml}}
            <div id="{{elementId}}" class="chart chart-annotated-time-line"></div>
            <script>
            google.charts.load('current', { packages: ['annotatedtimeline'] });
            google.charts.setOnLoadCallback(function () {
              var data = new google.visualization.DataTable();
              data.addColumn('date', 'Date');
              data.addColumn('number', {{seriesLabelJson}});
              data.addRows([
                {{rowsJs}}
              ]);
              var options = {
                {{colorsOption}}
                displayAnnotations: false,
                displayZoomButtons: true,
                displayRangeSelector: true,
                displayExactValues: true,
                scaleType: 'maximized',
                thickness: 2,
                dateFormat: 'MMM d, yyyy',
              };
              var container = document.getElementById('{{elementId}}');
              var chart = new google.visualization.AnnotatedTimeLine(container);
              function redraw() { options.height = window.__chartContentHeight(container, 640); chart.draw(data, options); }
              redraw();
              window.__dashboardCharts.push(redraw);
            });
            </script>
            """;
    }

    /// <summary>
    /// Renders a <c>google.visualization.BarChart</c> - the chart type ticket 01 reserves for every Top-50
    /// Leaderboard. Rows are rendered in the order given, with no secondary sort applied here - the caller's
    /// query already ranks them by the Leaderboard's own measure (per ticket 06: an unresolved tie among
    /// adjacent bars isn't misleading). An empty <paramref name="entries"/> (a real site/range with no
    /// qualifying hits at all) renders a plain "no data" card instead of attempting to draw - Google's
    /// <c>arrayToDataTable</c> can't infer the value column's type from zero data rows, and throws rather than
    /// drawing an empty chart if asked to (ticket 20).
    /// </summary>
    /// <param name="elementId">The HTML id of the <c>&lt;div&gt;</c> the chart draws into.</param>
    /// <param name="entries">The chart's bars, in the order they should be drawn (already ranked by the caller).</param>
    /// <param name="valueLabel">The ranked measure's display label (the chart's horizontal axis title).</param>
    /// <param name="title">The chart's own title, shown in its own card directly above the chart's card.</param>
    /// <param name="tone">
    /// Whether the bars draw in Google Charts' own default color or in red, for a Leaderboard scoped to a failed
    /// outcome (ticket 20). Defaults to <see cref="ChartTone.Default"/>.
    /// </param>
    /// <returns>The title card, the chart's container element, and its bootstrap script, ready to embed in a page body.</returns>
#pragma warning disable CC0042 // Five independent rendering inputs (id, bars, axis label, title, tone); bundling would just repackage them without benefit.
    public static string RenderBarChart(string elementId, IReadOnlyList<BarChartEntry> entries, string valueLabel, string title, ChartTone tone = ChartTone.Default)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(elementId);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(valueLabel);
        ArgumentNullException.ThrowIfNull(title);

        var titleCardHtml = RenderTitleCard(title);

        if (entries.Count == 0)
        {
            return $"""{titleCardHtml}<div class="chart chart-bar chart-empty">No data for this range.</div>""";
        }

        var header = new List<object?> { "Item", valueLabel };
        var rows = new List<IReadOnlyList<object?>> { header };
        rows.AddRange(entries.Select(entry => (IReadOnlyList<object?>)[entry.Label, entry.Value]));
        var dataJson = JsonSerializer.Serialize(rows);
        var labelsJson = JsonSerializer.Serialize(entries.Select(entry => entry.Label));
        var valueLabelJson = JsonSerializer.Serialize(valueLabel);
        var colorsOption = ColorsOption(tone);

        return $$"""
            {{titleCardHtml}}
            <div id="{{elementId}}" class="chart chart-bar"></div>
            <script>
            google.charts.load('current', { packages: ['corechart'] });
            google.charts.setOnLoadCallback(function () {
              var data = google.visualization.arrayToDataTable({{dataJson}});
              var labels = {{labelsJson}};
              var options = {
                chartArea: { top: 20, bottom: 50 },
                hAxis: { title: {{valueLabelJson}}, minValue: 0 },
                legend: { position: 'none' },
                {{colorsOption}}
              };
              var container = document.getElementById('{{elementId}}');
              var chart = new google.visualization.BarChart(container);
              function redraw() {
                var minHeight = {{entries.Count}} * 22 + 100;
                var available = window.__chartContentHeight(container, minHeight);
                var height = Math.max(minHeight, available);
                options.height = height;
                options.chartArea.left = window.__barChartLeftMargin(labels, container);
                window.__growChartContainerIfNeeded(container, height, available);
                chart.draw(data, options);
                window.__fixTruncatedBarLabels(chart, data, options, container);
              }
              redraw();
              window.__dashboardCharts.push(redraw);
            });
            </script>
            """;
    }

    /// <summary>
    /// Renders a sortable <c>google.visualization.Table</c> - the chart type ticket 01 settled on for every full
    /// list, including the Summary section's Complete View (native column-header sort, no hand-rolled script).
    /// Uses <c>arrayToDataTable</c>'s type inference rather than <see cref="ChartColumn"/>-style explicit typing,
    /// since a table's rows are always fully populated (a listing fills every column with a real, non-null
    /// value), unlike a line chart's per-date series gaps. Any column whose header ends in "(s)" (every
    /// "Total"/"Average Time Taken (s)" column, across every table this method renders) is always shown with
    /// exactly three decimal places via a <c>NumberFormat</c> - formatting only the display, not the underlying
    /// value, so the column still sorts numerically rather than as text (ticket 20). A <paramref name="rows"/>
    /// with nothing past its header row (a real site/range with no qualifying hits at all) renders a plain
    /// "no data" card instead of attempting to draw - the same "Table has no rows." error <see cref="RenderBarChart"/>
    /// guards against for a zero-row <c>arrayToDataTable</c> (ticket 20).
    /// </summary>
    /// <param name="elementId">The HTML id of the <c>&lt;div&gt;</c> the table draws into.</param>
    /// <param name="rows">
    /// The table's data, as row-major arrays: the first row is column headers, each following row is one
    /// record's values.
    /// </param>
    /// <param name="paging">
    /// Whether to turn on the widget's built-in <c>page: 'enable'</c> paging, so DOM cost stays bounded
    /// regardless of row count (per ticket 01/12: both uncapped Complete Views need this; the small, fixed-size
    /// Summary Complete View doesn't).
    /// </param>
    /// <param name="cellContent">
    /// Whether a cell may carry real HTML markup (per ticket 13: a Complete View row links to that entity's own
    /// Detail Page) rather than rendering as literal text.
    /// </param>
    /// <param name="title">
    /// An optional title, shown in its own card directly above the table's card - omitted (<see langword="null"/>)
    /// by default, since most callers rely on the page's own browser title instead.
    /// </param>
    /// <returns>The table's container element and bootstrap script (plus a title card, if given), ready to embed in a page body.</returns>
#pragma warning disable CC0042 // Three independent, defaulted rendering-option values alongside the table's own id/rows; bundling them would just repackage unrelated values.
    public static string RenderTable(
        string elementId,
        IReadOnlyList<IReadOnlyList<object?>> rows,
        TablePaging paging = TablePaging.Disabled,
        TableCellContent cellContent = TableCellContent.PlainText,
        string? title = null)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(elementId);
        ArgumentNullException.ThrowIfNull(rows);

        var titleCardHtml = title is null ? string.Empty : RenderTitleCard(title);

        if (rows.Count <= 1)
        {
            return $"""{titleCardHtml}<div class="chart chart-table chart-empty">No data for this range.</div>""";
        }

        var dataJson = JsonSerializer.Serialize(rows);
        var pagingOptions = paging == TablePaging.Enabled ? $"page: 'enable', pageSize: {TablePageSize}, " : string.Empty;
        var allowHtmlOptions = cellContent == TableCellContent.Html ? "allowHtml: true, " : string.Empty;

        return $$"""
            {{titleCardHtml}}
            <div id="{{elementId}}" class="chart chart-table"></div>
            <script>
            google.charts.load('current', { packages: ['table'] });
            google.charts.setOnLoadCallback(function () {
              var data = google.visualization.arrayToDataTable({{dataJson}});
              var secondsFormatter = new google.visualization.NumberFormat({ pattern: '#,##0.000' });
              for (var col = 0; col < data.getNumberOfColumns(); col++) {
                if (/\(s\)$/.test(data.getColumnLabel(col))) {
                  secondsFormatter.format(data, col);
                }
              }
              var options = { {{pagingOptions}}{{allowHtmlOptions}}showRowNumber: false, width: '100%' };
              var table = new google.visualization.Table(document.getElementById('{{elementId}}'));
              function redraw() { table.draw(data, options); }
              redraw();
              window.__dashboardCharts.push(redraw);
            });
            </script>
            """;
    }

    private static string FormatRow(DateOnly date, double? value)
    {
        var dateLiteral = $"new Date({date.Year}, {date.Month - 1}, {date.Day})";
        var valueLiteral = value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null";

        return $"[{dateLiteral}, {valueLiteral}]";
    }

    /// <summary>
    /// Builds a Google Charts <c>colors</c> option fragment for <paramref name="tone"/> - empty for
    /// <see cref="ChartTone.Default"/> (Google's own default series color applies), or a single red for
    /// <see cref="ChartTone.Failure"/>. Every caller here draws exactly one series, so a one-element array is
    /// always enough.
    /// </summary>
    private static string ColorsOption(ChartTone tone) => tone == ChartTone.Failure ? "colors: ['#9e2b33']," : string.Empty;

    /// <summary>
    /// Renders a title as its own short card, the same width as (and directly above) a chart's card - rather
    /// than the chart widget's own built-in title, which draws inside the chart's plotted area at whatever size
    /// and style Google Charts picks.
    /// </summary>
    private static string RenderTitleCard(string title)
    {
        return $"<div class=\"chart-title-card\"><h2>{WebUtility.HtmlEncode(title)}</h2></div>";
    }
}
