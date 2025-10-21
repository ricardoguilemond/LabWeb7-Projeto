using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LabWebMvc.MVC.HtmlHelpers
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
    {
        public override bool IsValid(object? value) => value is true;

        public void AddValidation(ClientModelValidationContext context)
        {
            string errorMessage = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
            context.Attributes.TryAdd("data-val", "true");
            context.Attributes.TryAdd("data-val-must-be-true", errorMessage);
        }
    }

    public static class HtmlHelpers
    {
        /*
         * Trabalha para as VMs, até resolver o problema do @Html.ValidationMessageFor que não está formatando com "span" (bug?) !!
         * Acrescenta a tag "div" com a classe pertinente a formatação correta
          */

        public static string HtmlErrorMessage(string texto)
        {
            return "div class='has-error'" + texto + "</div>";
        }

        /* Exemplos de Uso do Image():
         * Html.Image("img1", ResolveUrl("~/Content/XBox.jpg"), "XBox Console")
         * Html.Image("img1", ResolveUrl("~/Content/XBox.jpg"), "XBox Console", new {border="4px"})
         */

        public static string? Image(this HtmlHelper helper, string id, string url, string alternateText)
        {
            ArgumentNullException.ThrowIfNull(helper);
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(url);

            return Image(helper, id, url, alternateText, null);
        }

        public static string? Image(this HtmlHelper helper, string id, string url, string alternateText, object? htmlAttributes)
        {
            ArgumentNullException.ThrowIfNull(helper);
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(url);

            // Create tag builder
            TagBuilder builder = new("img");

            // Create valid id
            builder.GenerateId(id, "");

            // Add attributes
            builder.MergeAttribute("src", url);
            builder.MergeAttribute("alt", alternateText);
            builder.MergeAttributes(new RouteValueDictionary(htmlAttributes));

            // Render tag
            return builder.RenderSelfClosingTag().ToString();     //era?: ToString(TagRenderMode.SelfClosing);
        }

        //..

        //Remove ocorrências duplicadas/recorrentes mantendo SOMENTE a primeira ocorrência encontrada (palavras ou textos que estejam repetidos na string)
        private static string RemoveDuplicateOccurrences(string input, string? textoRecorrente)
        {
            if (string.IsNullOrEmpty(textoRecorrente)) return input;

            // Localiza a primeira ocorrência da palavra ou texto recorrente
            int firstOccurrenceIndex = input.IndexOf(textoRecorrente, StringComparison.OrdinalIgnoreCase);

            if (firstOccurrenceIndex == -1)
            {
                // Se a palavra não for encontrada, retorna a string original
                return input;
            }

            // Parte antes da primeira ocorrência
            string beforeFirstOccurrence = input.Substring(0, firstOccurrenceIndex + textoRecorrente.Length);

            // Parte após a primeira ocorrência
            string afterFirstOccurrence = input.Substring(firstOccurrenceIndex + textoRecorrente.Length);

            // Remove todas as ocorrências da palavra na parte restante
            string pattern = @"\b" + Regex.Escape(textoRecorrente) + @"\b";
            string cleanedAfterFirstOccurrence = Regex.Replace(afterFirstOccurrence, pattern, "", RegexOptions.IgnoreCase);

            // Concatenar as duas partes
            return beforeFirstOccurrence + cleanedAfterFirstOccurrence;
        }

        //..

        /* Gerando um link <a href */

        private static string GetTagLink(string valueRef, string? innerHtml = null, string? classe = null)
        {
            /* USO:
             *     var linha = GetTagString("Clique aqui neste exemplo", "a", "success", valueRef = "https://exemplo")
             */
            ArgumentNullException.ThrowIfNull(valueRef);

            if (string.IsNullOrEmpty(innerHtml)) innerHtml = "";

            TagBuilder container = new("a");     // Construct an <a> tag

            container.MergeAttribute("href", valueRef);
            if (!string.IsNullOrEmpty(classe)) container.MergeAttribute("class", classe);
            if (!string.IsNullOrEmpty(innerHtml)) container.InnerHtml.Append(innerHtml);

            using (StringWriter sw = new())
            {
                container.WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default);
                return sw.ToString();
            }
        }

        //..

        /* Gerando uma DIV */

        private static string GetTagDiv(string? innerHtml = null, string? classe = null)
        {
            /* USOS:
             *       var linha = GetTagString("Exemplo de uma linha qualquer", "has-error")
             */
            if (string.IsNullOrEmpty(innerHtml)) innerHtml = "";
            TagBuilder container = new("div");

            if (!string.IsNullOrEmpty(classe)) container.MergeAttribute("class", classe);
            if (!string.IsNullOrEmpty(innerHtml)) container.InnerHtml.Append(innerHtml);

            using (StringWriter sw = new())
            {
                container.WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default);
                return sw.ToString();
            }
        }

        //..

        /* Gerando uma tag Span */

        private static string GetTagSpan(string? innerHtml = null, string? classe = null)
        {
            /* USOS:
             *       var linha = GetTagSpan("Exemplo de uma linha qualquer", "has-error")
             */
            if (string.IsNullOrEmpty(innerHtml)) innerHtml = "";
            TagBuilder container = new("span");

            if (!string.IsNullOrEmpty(classe)) container.MergeAttribute("class", classe);
            if (!string.IsNullOrEmpty(innerHtml)) container.InnerHtml.Append(innerHtml);

            using (StringWriter sw = new())
            {
                container.WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default);
                return sw.ToString();
            }
        }

        //..

        /*
         * Gerando uma ValidationMessageFor personalizada
         * Expression<Func<TModel, TProperty>> expression -->> isto aqui é uma forma de receber uma expressão lambda (por exemplo: linq de sql como vm => vm.NomeCampo),
         * quando se deseja obter a expressão lambda inteira para poder olhar dentro dela.
         */

        //IHtmlContent
        public static IHtmlContent MyValidationMessageFor<TModel, TValue>(this IHtmlHelper<TModel> htmlHelper,
                                                                             Expression<Func<TModel, TValue>> expression,
                                                                             string? customErrorMessage = null,
                                                                             string trocaTag = "span")
        {
            ArgumentNullException.ThrowIfNull(htmlHelper);
            ArgumentNullException.ThrowIfNull(expression);

            //tag montada ErrorMessage: <span class="field-validation-valid" data-valmsg-for="ViewModel.NomeCampo" data-valmsg-replace="true">Blá Blá Blá</span>

            //var oldMessage = expression;
            //var newMessage = oldMessage.ToString().Replace("$NomePaciente$", customErrorMessage);
            //return new HtmlString(newMessage);

            PropertyInfo? propertyInfo = typeof(vmPacientes).GetProperty("NomePaciente", typeof(string));

            RequiredAttribute attribute = new()
            {
                ErrorMessageResourceType = typeof(vmPacientes),    // "Your resource file";
                ErrorMessageResourceName = propertyInfo?.Name,  //"NomePaciente";         // "Keep the key name in resource file as "PropertyValueRequired";
                ErrorMessage = "Olá"
            };
            IHtmlContent originalValidationMessage = htmlHelper.ValidationMessageFor(expression);
            return originalValidationMessage;

            //var originalValidationMessage = htmlHelper.ValidationMessageFor(expression);
            //using (var writer = new StringWriter())
            //{
            //    originalValidationMessage.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
            //    var validationMessageHtml = writer.ToString();
            //    //Substituindo a mensagem original da View Model pela mensagem passada por parâmetro...
            //    var customValidationMessageHtml = validationMessageHtml.Replace(">", $">{customErrorMessage}");
            //    //adiciona a classe "has-error" criada dinamicamente para o erro...
            //    //a tag "has-error" é criada dinamicamente aqui: styleProvider.GetStyle(); colocando direto na página razor...
            //    customValidationMessageHtml = customValidationMessageHtml.Replace("field-validation-valid", "field-validation-valid has-error");
            //    //substitui pela nova tag enviada por parâmetro...
            //    if (trocaTag != "span")
            //    {
            //        customValidationMessageHtml = customValidationMessageHtml.Replace("<span", "<" + trocaTag);
            //        customValidationMessageHtml = customValidationMessageHtml.Replace("</span>", "</" + trocaTag + ">");
            //    }
            //    customValidationMessageHtml = RemoveDuplicateOccurrences(customValidationMessageHtml, customErrorMessage);

            //    return new HtmlString(customValidationMessageHtml);

            //}

            /*

             da classe interna ValidationAttribute

            /// <summary>
            ///     Gets or sets the explicit error message string.
            /// </summary>
            /// <value>
            ///     This property is intended to be used for non-localizable error messages.  Use
            ///     <see cref="ErrorMessageResourceType" /> and <see cref="ErrorMessageResourceName" /> for localizable error messages.
            /// </value>
            public string? ErrorMessage
            {
                // If _errorMessage is not set, return the default. This is done to preserve
                // behavior prior to the fix where ErrorMessage showed the non-null message to use.
                get => _errorMessage ?? _defaultErrorMessage;
                set
                {
                    _errorMessage = value;
                    _errorMessageResourceAccessor = null;
                    CustomErrorMessageSet = true;

                    // Explicitly setting ErrorMessage also sets DefaultErrorMessage if null.
                    // This prevents subsequent read of ErrorMessage from returning default.
                    if (value == null)
                    {
                        _defaultErrorMessage = null;
                    }
                }
            }

             */

            //            TagBuilder containerDivBuilder = new TagBuilder("div");
            //            containerDivBuilder.AddCssClass("field-error-box");

            //    string campo = "p => p.VmPacientes.NomePaciente";
            //campo = campo.Split(".").LastOrDefault();

            //PropertyInfo propertyInfo = typeof(vmPacientes).GetProperty(campo, typeof(string));
            //RequiredAttribute? attribute = (RequiredAttribute?)propertyInfo.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();
            //if (attribute != null)
            //{
            //    attribute.ErrorMessage = "Olá";
            //}

            //TagBuilder container = new TagBuilder("div");
            //container.AddCssClass("has-error");

            //containerSpan.AddCssClass("has-error");
            //containerSpan.MergeAttribute("class", "has-error");

            //container.InnerHtml.SetHtmlContent(containerDiv);

            //return htmlHelper.ValidationMessageFor(expression, null, null, "div");
        }
    }
}