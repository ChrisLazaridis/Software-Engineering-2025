using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SoftEng2025.TagHelpers
{
    [HtmlTargetElement("map", Attributes = "lat,lon")]
    public class MapTagHelper : TagHelper
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Width { get; set; } = "250px";
        public string Height { get; set; } = "150px";
        public bool Selectable { get; set; } = false;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // 1) Container DIV
            var id = $"map_{Guid.NewGuid():N}";
            output.TagName = "div";
            output.Attributes.SetAttribute("id", id);
            output.Attributes.SetAttribute("style", $"width:{Width};height:{Height};");

            // 2) Hidden inputs if selectable
            if (Selectable)
            {
                var hidden = @"
<input type=""hidden"" id=""Lat"" name=""Lat"" />
<input type=""hidden"" id=""Lon"" name=""Lon"" />";
                output.PreElement.AppendHtml(hidden);
            }

            // 3) Initialize map + marker
            //    Fix: marker options object passed _into_ L.marker(...)
            var script = $@"
<script>
  (function() {{
    var map = L.map('{id}').setView([{Lat}, {Lon}], 15);
    L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors'
    }}).addTo(map);

    // Correct instantiation:
    var marker = L.marker(
      [{Lat}, {Lon}],
      {{ draggable: {(Selectable ? "true" : "false")} }}
    ).addTo(map);

    {(Selectable
        ? @"
    function updateFields(latlng) {
      document.getElementById('Lat').value = latlng.lat.toFixed(6);
      document.getElementById('Lon').value = latlng.lng.toFixed(6);
    }
    updateFields(marker.getLatLng());
    map.on('click', function(e) {
      marker.setLatLng(e.latlng);
      updateFields(e.latlng);
    });
    marker.on('dragend', function(e) {
      updateFields(e.target.getLatLng());
    });"
        : "")}
  }})();
</script>";
            output.PostElement.AppendHtml(script);
        }
    }
}
