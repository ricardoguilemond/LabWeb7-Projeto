using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LabWebMvc.MVC.Areas.ExpressionCombiner
{
    /*
         EXEMPLO DE COMO USAR:
                    using LabWebMvc.MVC.Areas.ExpressionCombiner.Filtros;

                    dados = await db.Tabela.AsNoTracking()
                                           .FiltrarPorConteudo(Conteudo, "", x => x.RefExame!, x => x.Id.ToString())
                                           .OrderByDescending(x => x.Id)
                                           .ToListAsync();

         Lembrando que esse collate "Latin1_General_100_CI_AI_SC_UTF8" é nosso padrão, mas podemos modificar conforme necessidade.

    */

    public static class Expressions
    {
        /* Expressões para facilitar as pesquisas em diversas consultas em tabelas na base de dados */

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T));
            BinaryExpression body = Expression.OrElse(
                Expression.Invoke(expr1, parameter),
                Expression.Invoke(expr2, parameter));
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T));
            BinaryExpression body = Expression.AndAlso(
                Expression.Invoke(expr1, parameter),
                Expression.Invoke(expr2, parameter));
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }//Fim da classe Expressions

    public static class Filtros
    {
        public static IQueryable<T> FiltrarPorConteudo<T>(this IQueryable<T> source, string? conteudo, string myCollate,
                                                          params Expression<Func<T, string?>>[] camposTexto)
        {
            if (string.IsNullOrWhiteSpace(conteudo))
                return source;

            string termo = $"%{conteudo.Trim()}%";

            Expression<Func<T, bool>>? filtro = null;

            foreach (Expression<Func<T, string?>> campo in camposTexto)
            {
                Expression<Func<T, bool>> exp = BuildLike(campo, termo, myCollate);
                filtro = filtro == null ? exp : filtro.Or(exp);
            }

            return source.Where(filtro!);
        }

        public static IQueryable<T> FiltrarPorConteudo<T>(this IQueryable<T> source, string? conteudo, params Expression<Func<T, string?>>[] camposTexto)
        {
            // Collation padrão: case-insensitive, accent-insensitive, UTF-8
            const string defaultCollate = "Latin1_General_100_CI_AI_SC_UTF8";
            return FiltrarPorConteudo(source, conteudo, defaultCollate, camposTexto);
        }

        public static Expression<Func<T, bool>> BuildLike<T>(Expression<Func<T, string?>> campo, string padraoSqlLike, string myCollate)
        {
            ParameterExpression param = campo.Parameters[0];
            Expression body = campo.Body;

            // EF.Functions.Collate(campo, "Latin1_General_100_CI_AI_SC_UTF8")
            MethodCallExpression collateCall = Expression.Call(
                typeof(RelationalDbFunctionsExtensions),
                nameof(RelationalDbFunctionsExtensions.Collate),
                new Type[] { typeof(string) },
                Expression.Constant(EF.Functions),
                body,
                Expression.Constant(myCollate)
            );

            // EF.Functions.Like(collated, padraoSqlLike)
            MethodCallExpression likeCall = Expression.Call(
                typeof(DbFunctionsExtensions),
                nameof(DbFunctionsExtensions.Like),
                Type.EmptyTypes,
                Expression.Constant(EF.Functions),
                collateCall,
                Expression.Constant(padraoSqlLike)
            );

            return Expression.Lambda<Func<T, bool>>(likeCall, param);
        }
    }// Fim da classe Filtros
}