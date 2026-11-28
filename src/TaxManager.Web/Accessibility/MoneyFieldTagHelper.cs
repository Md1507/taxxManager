using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TaxManager.Web.Accessibility;

/// <summary>Keep the currency unit and format help attached to visible money fields.</summary>
[HtmlTargetElement("input", Attributes = "asp-for,inputmode")]
public sealed class MoneyFieldTagHelper : TagHelper
{
    public override int Order => 1100;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (output.Attributes["inputmode"]?.Value?.ToString() != "decimal" ||
            output.Attributes["type"]?.Value?.ToString() == "hidden") return;
        var id = output.Attributes["id"]?.Value?.ToString();
        if (string.IsNullOrEmpty(id)) return;
        var hintId = "money-" + id + "-hint";
        var describedBy = output.Attributes["aria-describedby"]?.Value?.ToString();
        output.Attributes.SetAttribute("aria-describedby", string.IsNullOrWhiteSpace(describedBy) ? hintId : describedBy + " " + hintId);
        if (!output.Attributes.ContainsName("placeholder"))
            output.Attributes.SetAttribute("placeholder", id.Contains("Insurance", StringComparison.Ordinal) ? "Ví dụ: 3.000.000" : "Ví dụ: 30.000.000");
        output.PreElement.AppendHtml("<div class=\"money-field\">");
        output.PostElement.AppendHtml("<span class=\"money-unit\" aria-hidden=\"true\">đ</span></div>");
        var hint = new TagBuilder("p");
        hint.Attributes["id"] = hintId;
        hint.AddCssClass("hint money-hint");
        hint.InnerHtml.Append("Tách hàng nghìn bằng dấu chấm, phần lẻ bằng dấu phẩy. Ô trống là chưa nhập; 0 là đã nhập.");
        output.PostElement.AppendHtml(hint);
    }
}
