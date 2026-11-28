using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TaxManager.Web.Accessibility;

/// <summary>Expose server validation errors, including when JavaScript is unavailable.</summary>
[HtmlTargetElement("input", Attributes = "asp-for")]
[HtmlTargetElement("select", Attributes = "asp-for")]
[HtmlTargetElement("textarea", Attributes = "asp-for")]
[HtmlTargetElement("textarea", Attributes = "name")]
public sealed class FieldErrorTagHelper : TagHelper
{
    [HtmlAttributeName("asp-for")] public ModelExpression? For { get; set; }
    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = null!;
    public override int Order => 1000;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (output.Attributes["type"]?.Value?.ToString() == "hidden") return;
        var name = For is null ? output.Attributes["name"]?.Value?.ToString() ?? ""
            : ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name);
        if (ViewContext.ViewData.ModelState[name]?.Errors.Count is not > 0) return;
        output.Attributes.SetAttribute("aria-invalid", "true");
        var errorId = "error-" + TagBuilder.CreateSanitizedId(name, "_");
        var existing = output.Attributes["aria-describedby"]?.Value?.ToString();
        output.Attributes.SetAttribute("aria-describedby", string.IsNullOrWhiteSpace(existing) ? errorId : existing + " " + errorId);
    }
}
