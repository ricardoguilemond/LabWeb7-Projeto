using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace LabWebMvc.MVC.UtilHelper
{
    [HtmlTargetElement("status-menu", Attributes = "Conteudo")]
    public class MyTagHelpers : TagHelper
    {
        public string? esteConteudo { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "status-menu";
            output.TagMode = TagMode.StartTagAndEndTag;
            StringBuilder sb = new();
            sb.AppendFormat("<span>{0}</span>", esteConteudo);
            output.PreContent.SetHtmlContent(sb.ToString());
        }
    }
}