using Microsoft.AspNetCore.Razor.TagHelpers;
using UOM.Web.Security;

namespace UOM.Web.TagHelpers;

[HtmlTargetElement("script")]
public sealed class CspNonceTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!output.Attributes.ContainsName("nonce"))
        {
            output.Attributes.SetAttribute("nonce", CspNonce.Placeholder);
        }
    }
}
