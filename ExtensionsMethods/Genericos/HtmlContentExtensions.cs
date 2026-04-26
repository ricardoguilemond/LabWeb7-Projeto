using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;

namespace ExtensionsMethods.Genericos
{
    public static class HtmlContentExtensions
    {
        public static string GetString(this IHtmlContent content)
        {
            using StringWriter writer = new();
            content.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }
    }
}