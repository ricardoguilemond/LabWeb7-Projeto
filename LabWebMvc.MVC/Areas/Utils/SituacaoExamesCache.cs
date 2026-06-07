using System.Collections.Concurrent;
using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Areas.Utils;

//Feito pelo Kiro em 07/06/2026
/// <summary>
/// Cache estático da tabela SituacaoExames.
/// Carregado uma única vez na primeira consulta e mantido em memória.
/// </summary>
public static class SituacaoExamesCache
{
    private static ConcurrentDictionary<int, string>? _cache;
    private static readonly object _lock = new();

    /// <summary>
    /// Retorna a descrição do status pelo código.
    /// Carrega o cache na primeira chamada.
    /// </summary>
    public static string ObterDescricao(int codigo, Db db)
    {
        if (_cache == null)
        {
            lock (_lock)
            {
                if (_cache == null)
                    Carregar(db);
            }
        }

        return _cache!.TryGetValue(codigo, out var descricao)
            ? descricao
            : "Pendente";
    }

    /// <summary>
    /// Carrega todos os registros da tabela SituacaoExames no cache.
    /// </summary>
    private static void Carregar(Db db)
    {
        var dados = db.SituacaoExames
            .AsNoTracking()
            .ToDictionary(s => s.Id, s => s.Descricao ?? "");
        _cache = new ConcurrentDictionary<int, string>(dados);
    }

    /// <summary>
    /// Invalida o cache para forçar recarga na próxima consulta.
    /// Usar caso a tabela seja alterada em runtime.
    /// </summary>
    public static void Invalidar()
    {
        _cache = null;
    }
}
//..Kiro
