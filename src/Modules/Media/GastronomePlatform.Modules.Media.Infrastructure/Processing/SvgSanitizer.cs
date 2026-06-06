using Ganss.Xss;
using GastronomePlatform.Modules.Media.Application.Abstractions;

namespace GastronomePlatform.Modules.Media.Infrastructure.Processing
{
    /// <summary>
    /// Реализация <see cref="ISvgSanitizer"/> на основе библиотеки <c>HtmlSanitizer</c>
    /// (Ganss.Xss). Удаляет <c>&lt;script&gt;</c>, <c>on*</c>-атрибуты и потенциально
    /// опасные URI-схемы (<c>javascript:</c>) из SVG-разметки.
    /// </summary>
    public sealed class SvgSanitizer : ISvgSanitizer
    {
        private static readonly HtmlSanitizer _sanitizer = BuildSanitizer();

        /// <inheritdoc/>
        public string Sanitize(string svgContent)
            => _sanitizer.Sanitize(svgContent);

        private static HtmlSanitizer BuildSanitizer()
        {
            var sanitizer = new HtmlSanitizer();

            // Базовые SVG-элементы, используемые в иконках и иллюстрациях.
            string[] svgTags =
            [
                "svg", "g", "defs", "symbol", "use",
                "path", "rect", "circle", "ellipse", "line", "polyline", "polygon",
                "text", "tspan", "textPath",
                "image", "pattern", "mask", "filter",
                "linearGradient", "radialGradient", "stop",
                "feBlend", "feColorMatrix", "feComposite", "feFlood", "feGaussianBlur",
                "feMerge", "feMergeNode", "feTile", "feTurbulence",
                "clipPath", "marker", "title", "desc", "metadata",
                "animate", "animateTransform", "animateMotion", "mpath", "set"
            ];

            foreach (var tag in svgTags)
            {
                sanitizer.AllowedTags.Add(tag);
            }

            // Основные SVG-атрибуты (пространство имён, геометрия, стиль).
            string[] svgAttributes =
            [
                "xmlns", "xmlns:xlink", "xlink:href", "href",
                "viewBox", "width", "height", "x", "y", "x1", "y1", "x2", "y2",
                "cx", "cy", "r", "rx", "ry", "d", "points",
                "fill", "fill-opacity", "fill-rule",
                "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin",
                "stroke-opacity", "stroke-dasharray", "stroke-dashoffset",
                "opacity", "display", "visibility",
                "transform", "clip-path", "mask", "filter",
                "style", "class", "id",
                "gradientUnits", "gradientTransform", "spreadMethod",
                "patternUnits", "patternTransform", "preserveAspectRatio",
                "markerWidth", "markerHeight", "markerUnits", "orient", "refX", "refY",
                "in", "in2", "result", "type", "values", "stdDeviation",
                "offset", "stop-color", "stop-opacity",
                "font-size", "font-family", "font-weight", "text-anchor",
                "attributeName", "attributeType", "begin", "dur", "end",
                "repeatCount", "repeatDur", "from", "to", "by",
                "calcMode", "keyTimes", "keySplines", "additive", "accumulate",
                "path", "keyPoints", "rotate", "origin"
            ];

            foreach (var attr in svgAttributes)
            {
                sanitizer.AllowedAttributes.Add(attr);
            }

            // Блокируем javascript:-ссылки в любых URI-атрибутах.
            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedSchemes.Add("http");
            sanitizer.AllowedSchemes.Add("https");
            sanitizer.AllowedSchemes.Add("data");

            return sanitizer;
        }
    }
}
