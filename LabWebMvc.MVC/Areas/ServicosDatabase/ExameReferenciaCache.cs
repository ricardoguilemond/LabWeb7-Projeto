using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using LabWebMvc.MVC.Models;
using System.Text.RegularExpressions;

namespace LabWebMvc.MVC.Areas.ServicosDatabase
{
    //Feito pelo Kiro em 11/07/2025
    public class ExameReferenciaCache : IExameReferenciaCache
    {
        private readonly IMemoryCache _cache;
        private readonly IDbFactory _dbFactory;
        private readonly IConnectionService _connectionService;

        public ExameReferenciaCache(IMemoryCache cache, IDbFactory dbFactory, IConnectionService connectionService)
        {
            _cache = cache;
            _dbFactory = dbFactory;
            _connectionService = connectionService;
        }

        public List<ExameReferenciaItem>? ObterReferencias(string contaExame)
        {
            string chaveCache = ObterChaveCache();
            if (string.IsNullOrWhiteSpace(chaveCache))
                return null;

            if (!_cache.TryGetValue(chaveCache, out Dictionary<string, List<ExameReferenciaItem>>? dict))
                return null;

            if (dict == null || !dict.TryGetValue(contaExame, out var lista))
                return null;

            return lista;
        }

        public async Task CarregarCacheAsync(string nomeBanco)
        {
            string chaveCache = $"ExameRef_{nomeBanco}";

            using var db = _dbFactory.Create();

            var registros = await db.ExameReferencia
                .AsNoTracking()
                .OrderBy(r => r.ContaExame)
                .ThenBy(r => r.DataCriacao)
                .ToListAsync();

            var dict = new Dictionary<string, List<ExameReferenciaItem>>();

            foreach (var reg in registros)
            {
                if (!dict.ContainsKey(reg.ContaExame))
                    dict[reg.ContaExame] = new List<ExameReferenciaItem>();

                dict[reg.ContaExame].Add(new ExameReferenciaItem
                {
                    ConteudoBinario = reg.ConteudoBinario,
                    FormatoOrigem = reg.FormatoOrigem,
                    AlinhaLaudo = reg.AlinhaLaudo
                });
            }

            // Cache sem expiração — permanece até próximo login
            _cache.Set(chaveCache, dict);
        }

        public bool CacheCarregado(string nomeBanco)
        {
            string chaveCache = $"ExameRef_{nomeBanco}";
            return _cache.TryGetValue(chaveCache, out _);
        }

        private string ObterChaveCache()
        {
            try
            {
                var connStr = _connectionService.GetConnectionString();
                if (string.IsNullOrWhiteSpace(connStr))
                    return "";

                var match = Regex.Match(connStr, @"Database=([^;]+)", RegexOptions.IgnoreCase);
                return match.Success ? $"ExameRef_{match.Groups[1].Value}" : "";
            }
            catch
            {
                return "";
            }
        }
    }
    //..Kiro
}
