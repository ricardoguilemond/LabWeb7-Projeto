/*
 * grid-navigate.js
 * Funcoes reutilizaveis para salvar/restaurar estado do DataTables
 * e implementar o botao "Voltar" nas telas de edicao/inclusao.
 */

/*
 * Salva o estado atual do DataTables (#modeloTable) em sessionStorage
 * antes de navegar para a tela de edicao/inclusao.
 * @param {string} chave - Identificador unico da tela chamadora (ex: 'Medicos')
 * @param {string|number} itemId - Id do registro selecionado para destacar ao retornar
 */
function salvarEstadoGrid(chave, itemId) {
    var table = $('#modeloTable').DataTable();
    var estado = {
        page: table.page(),
        search: table.search(),
        order: table.order(),
        colSearches: [],
        itemId: itemId,
        scrollY: window.scrollY,
        url: window.location.href
    };
    // Salvar buscas por coluna (inputs do tfoot)
    table.columns().every(function () {
        var val = $('input', this.footer()).val();
        estado.colSearches.push(val || '');
    });
    // Salvar folha selecionada, se houver (PlanoExames / PlanoExamesItens)
    var $folha = $('#id_folha');
    if ($folha.length) {
        estado.folhaId = $folha.val();
    }
    // Salvar tabela selecionada, se houver (PlanoExamesItens)
    var $tabela = $('#id_tabela');
    if ($tabela.length) {
        estado.tabelaId = $tabela.val();
    }
    sessionStorage.setItem('gridState_' + chave, JSON.stringify(estado));
}

/*
 * Restaura a folha selecionada (se aplicavel) e recarrega o grid via AJAX
 * antes de executar a callback. Usado em telas como PlanoExames, onde o
 * dropdown de folha remonta o DataTables dinamicamente.
 * @param {string} chave - Mesmo identificador usado em salvarEstadoGrid
 * @param {function} callback - Funcao chamada apos o grid ser reconstruido
 */
function restaurarFolhaEGrid(chave, callback) {
    var raw = sessionStorage.getItem('gridState_' + chave);
    if (!raw) {
        if (callback) callback();
        return;
    }
    var estado = JSON.parse(raw);
    var $folha = $('#id_folha');

    if (estado.folhaId && $folha.length) {
        // Sempre recarrega o grid para a folha salva, garantindo que o
        // dropdown e os dados estejam sincronizados ao voltar.
        $folha.val(estado.folhaId);
        $('#nome_folha').val(estado.folhaId);

        $.ajax({
            url: '/PlanoExames?numeroItemFolha=' + encodeURIComponent(estado.folhaId) + '&partial=true',
            type: 'GET',
            cache: false,
            success: function (data) {
                try {
                    var table = $('#modeloTable').DataTable();
                    if (table) table.destroy();
                } catch (e) {
                    // Tabela ainda nao inicializada; prossegue normalmente.
                }
                $('#modeloTable').html(data);
                configTable();
                if (callback) callback();
            },
            error: function (xhr, status, error) {
                console.warn('Falha ao restaurar folha ' + estado.folhaId + ': ' + status);
                if (callback) callback();
            }
        });
    } else {
        if (callback) callback();
    }
}

/*
 * Restaura a tabela e a folha selecionadas (PlanoExamesItens) e recarrega o
 * grid via AJAX antes de executar a callback. Usado quando dois dropdowns
 * controlam o conteudo do DataTables.
 * @param {string} chave - Mesmo identificador usado em salvarEstadoGrid
 * @param {function} callback - Funcao chamada apos o grid ser reconstruido
 */
function restaurarTabelaEFolhaEGrid(chave, callback) {
    var raw = sessionStorage.getItem('gridState_' + chave);
    if (!raw) {
        if (callback) callback();
        return;
    }
    var estado = JSON.parse(raw);
    var $folha = $('#id_folha');
    var $tabela = $('#id_tabela');

    if (estado.folhaId && estado.tabelaId && $folha.length && $tabela.length) {
        $folha.val(estado.folhaId);
        $('#nome_folha').val(estado.folhaId);
        $tabela.val(estado.tabelaId);
        $('#nome_tabela').val(estado.tabelaId);

        $.ajax({
            url: '/PlanoExamesItens?numeroItemFolha=' + encodeURIComponent(estado.folhaId) +
                 '&numeroTabela=' + encodeURIComponent(estado.tabelaId) + '&partial=true',
            type: 'GET',
            cache: false,
            success: function (data) {
                try {
                    var table = $('#modeloTable').DataTable();
                    if (table) table.destroy();
                } catch (e) {
                    // Tabela ainda nao inicializada; prossegue normalmente.
                }
                $('#modeloTable').html(data);
                configTable();
                if (callback) callback();
            },
            error: function (xhr, status, error) {
                console.warn('Falha ao restaurar tabela/folha: ' + status);
                if (callback) callback();
            }
        });
    } else {
        if (callback) callback();
    }
}

/*
 * Restaura o estado salvo do DataTables apos a inicializacao via configTable().
 * Deve ser chamado apos o DataTables estar pronto (usar setTimeout se necessario).
 * @param {string} chave - Mesmo identificador usado em salvarEstadoGrid
 */
function restaurarEstadoGrid(chave) {
    var raw = sessionStorage.getItem('gridState_' + chave);
    if (!raw) return;
    var estado = JSON.parse(raw);

    var table = $('#modeloTable').DataTable();
    // Restaurar buscas por coluna
    if (estado.colSearches && estado.colSearches.length) {
        table.columns().every(function (idx) {
            if (estado.colSearches[idx]) {
                this.search(estado.colSearches[idx]);
            }
        });
    }
    // Restaurar busca global
    if (estado.search) table.search(estado.search);
    // Restaurar ordenacao
    if (estado.order) table.order(estado.order);
    // Ir para a pagina salva sem recarregar
    table.page(estado.page).draw(false);

    // Atualizar a linha alterada sem refresh e depois destacar
    if (estado.itemId && estado.url) {
        setTimeout(function () {
            atualizarLinhaGrid(chave, function () {
                sessionStorage.removeItem('gridState_' + chave); // limpa apos usar
                destacarLinhaGrid(estado.itemId);
            });
        }, 350);
    } else {
        sessionStorage.removeItem('gridState_' + chave); // limpa apos usar
        // Rolar a janela se necessario
        if (estado.scrollY) {
            setTimeout(function () {
                window.scrollTo(0, estado.scrollY);
            }, 200);
        }
    }
}

/*
 * Atualiza os dados da linha alterada no DataTables sem recarregar a pagina.
 * Busca a pagina do grid via AJAX, extrai a linha correspondente ao itemId
 * e substitui o HTML da linha atual, invalidando o cache do DataTables.
 * @param {string} chave - Identificador da tela chamadora
 * @param {function} callback - Funcao chamada ao final (sucesso ou nao)
 */
function atualizarLinhaGrid(chave, callback) {
    var raw = sessionStorage.getItem('gridState_' + chave);
    if (!raw) {
        if (callback) callback();
        return;
    }
    var estado = JSON.parse(raw);
    if (!estado.itemId || !estado.url) {
        if (callback) callback();
        return;
    }

    // Monta URL de busca respeitando a folha/tabela selecionadas (PlanoExames / PlanoExamesItens)
    var urlBusca = estado.url || window.location.href;
    if (estado.folhaId && urlBusca.indexOf('PlanoExames') !== -1 && urlBusca.indexOf('numeroItemFolha=') === -1) {
        urlBusca += (urlBusca.indexOf('?') === -1 ? '?' : '&') + 'numeroItemFolha=' + encodeURIComponent(estado.folhaId);
    }
    if (estado.tabelaId && urlBusca.indexOf('PlanoExamesItens') !== -1 && urlBusca.indexOf('numeroTabela=') === -1) {
        urlBusca += (urlBusca.indexOf('?') === -1 ? '?' : '&') + 'numeroTabela=' + encodeURIComponent(estado.tabelaId);
    }

    $.ajax({
        url: urlBusca,
        type: 'GET',
        cache: false,
        success: function (html) {
            var itemIdStr = String(estado.itemId);
            var $novoTbody = $('<div>').html(html).find('#modeloTable tbody');
            var $novaLinha = $novoTbody.find('tr').filter(function () {
                var dataId = $(this).attr('data-id');
                if (dataId !== undefined) return String(dataId) === itemIdStr;
                return $.trim($(this).find('td:first').text()) === itemIdStr;
            }).first();

            if ($novaLinha.length) {
                var table = $('#modeloTable').DataTable();
                var $linhaAtual = $('#modeloTable tbody tr').filter(function () {
                    var dataId = $(this).attr('data-id');
                    if (dataId !== undefined) return String(dataId) === itemIdStr;
                    return $.trim($(this).find('td:first').text()) === itemIdStr;
                }).first();

                if ($linhaAtual.length) {
                    // Atualiza apenas o conteudo de cada <td>, preservando as classes
                    // de alinhamento (dt-right, dt-center etc.) aplicadas pelo DataTables.
                    var $tdsAtual = $linhaAtual.find('td');
                    var $tdsNovo = $novaLinha.find('td');
                    var count = Math.min($tdsAtual.length, $tdsNovo.length);
                    for (var i = 0; i < count; i++) {
                        $tdsAtual.eq(i).html($tdsNovo.eq(i).html());
                    }
                    table.row($linhaAtual).invalidate().draw(false);
                }
            }
        },
        error: function () {
            // Silencioso: se nao conseguir buscar, mantem os dados antigos
        },
        complete: function () {
            if (callback) callback();
        }
    });
}

/*
 * Destaca e rola ate a linha cuja primeira coluna contem o itemId.
 * @param {string|number} itemId - Id do registro a destacar
 */
function destacarLinhaGrid(itemId) {
    var itemIdStr = String(itemId);
    // Primeiro tenta match pelo data-id da linha (mais confiavel)
    var $linha = $('#modeloTable tbody tr').filter(function () {
        var dataId = $(this).attr('data-id');
        return dataId !== undefined && String(dataId) === itemIdStr;
    });
    // Fallback: match pelo texto da primeira coluna
    if (!$linha.length) {
        $linha = $('#modeloTable tbody tr').filter(function () {
            var $firstTd = $(this).find('td:first');
            return $firstTd.text().trim() === itemIdStr;
        });
    }
    if ($linha.length) {
        $('#modeloTable tbody tr').removeClass('detail-parent-highlight');
        $linha.addClass('detail-parent-highlight');
        $linha[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
}

/*
 * Inicializa o botao "Voltar" (#btnVoltar) em telas de edicao/inclusao.
 * Usa window.history.back() com fallback para forcar reload se bfcache estiver ativo.
 */
function initBtnVoltar() {
    $(document).off('click.btnVoltar').on('click.btnVoltar', '#btnVoltar', function (e) {
        e.preventDefault();
        window.history.back();
    });
}

/*
 * Fallback para bfcache: quando o navegador mostrar a pagina do cache
 * (botao Voltar sem refresh), restaura o estado do grid e atualiza a linha.
 * Este listener eh global porque o grid-navigate.js eh carregado em todas as paginas.
 */
window.addEventListener('pageshow', function (event) {
    if (event.persisted) {
        for (var i = 0; i < sessionStorage.length; i++) {
            var key = sessionStorage.key(i);
            if (key && key.indexOf('gridState_') === 0) {
                var chave = key.replace('gridState_', '');
                if ($('#modeloTable').length) {
                    restaurarEstadoGrid(chave);
                }
                break;
            }
        }
    }
});
