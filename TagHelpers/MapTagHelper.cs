using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SoftEng2025.TagHelpers
{
    [HtmlTargetElement("map", Attributes = "lat,lon")]
    public class MapTagHelper : TagHelper
    {
        /// <summary>Latitude to center on.</summary>
        public double Lat { get; set; }

        /// <summary>Longitude to center on.</summary>
        public double Lon { get; set; }

        /// <summary>CSS width (e.g. "250px" or "100%").</summary>
        public string Width { get; set; } = "250px";

        /// <summary>CSS height (e.g. "150px").</summary>
        public string Height { get; set; } = "150px";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Generate a unique div ID so multiple maps can coexist
            var id = $"map_{Guid.NewGuid():N}";

            // Turn <map> into a <div id="..." style="..."></div>
            output.TagName = "div";
            output.Attributes.SetAttribute("id", id);
            output.Attributes.SetAttribute("style", $"width:{Width};height:{Height};");

            // Append the inline Leaflet init script _after_ the div
            var script = $@"
<script>
  // initialize the map
  var map = L.map('{id}').setView([{Lat}, {Lon}], 15);
  // use OpenStreetMap tiles
  L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
    maxZoom: 19,
    attribution: '&copy; OpenStreetMap contributors'
  }}).addTo(map);
  // marker at the exact spot
  L.marker([{Lat}, {Lon}]).addTo(map);
</script>";
            output.PostElement.AppendHtml(script);
        }
    }
}