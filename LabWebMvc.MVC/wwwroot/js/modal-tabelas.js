// Executa imediatamente — este script é carregado dinamicamente via $.load() junto com o HTML do modal
(function () {
    var tabelasConfig = document.getElementById('modalTabelasConfig');
    if (!tabelasConfig) return; // Proteção caso o elemento não exista
    var urlRetornoModalTabela = tabelasConfig.dataset.urlRetornoModalTabela;
    var urlPartialLancarExames = tabelasConfig.dataset.urlPartialLancarExames;
    var urlPartialMontarItensCupom = tabelasConfig.dataset.urlPartialMontarItensCupom;

    //Constrói o Modal
    configTableModal('#tabelaPreco');

    function preencherFormularioTabela(vm) {
        var $conteudo = $("#conteudoTabela");
        $conteudo.find("#buscaSiglaTabela").val(vm.siglaTabela);
        $conteudo.find("#buscaNomeTabela").val(vm.nomeTabela);
        $conteudo.find("#tabelaExamesId").val(vm.tabelaExamesId);  //o valor "id" aqui é o Id da primary key retornado!
    }   

    function ajaxFunction(valorSelecionado) {
        if (valorSelecionado.length > 0) {
            fetch(`${urlRetornoModalTabela}?id=${encodeURIComponent(valorSelecionado)}`, { //Javascript puro: substitui uso de Ajax, mais moderno e simples.
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data.vm.siglaTabela || !data.vm.nomeTabela) {
                    clickAviso('Atenção', 'Não trouxe dados nesta busca', 'falha');
                    return;
                }
                // Preenchendo os campos após retornar do Controller
                preencherFormularioTabela(data.vm);

                // Montando/atualizando o grid de exames referente a Tabela de Exames (para lançamento no paciente)
                $('#conteudoListaDeExames').load(`${urlPartialLancarExames}?tabelaExamesId=${data.vm.tabelaExamesId}`);  //obs: é com crase, pois tem interpolação javascript!

                //Atualizando o grid de Cupom, cada vez que reabrir a tabela e preços, para que o usuário possa escolher os itens.
                $('#conteudoItensCupomCaixa').load(urlPartialMontarItensCupom);

                ModalManager.fechar('modeloTableModalTabelas');
            })
            .catch(function () {
                clickAviso('Interrompido', 'Falha no carregamento dos dados', 'falha');
            });
        }
    }
    //..

    //Feito pelo Kiro em 01/05/2026
    // Captura ENTER no campo "search" do DataTables do modal de Tabelas (escopo restrito ao modal)
    // IMPORTANTE: usar escopo '#modeloTableModalTabelas' para não capturar o search de outros modais
    // (ex: modal de Postos), evitando carregamento indevido da tabela de exames.
    $(document).on("keydown", "#modeloTableModalTabelas input[type='search']", function (event) {
        if (event.keyCode === 13) { // ENTER
            event.preventDefault();
            var table = $('#tabelaPreco').DataTable();
            var linhasVisiveis = table.rows({ filter: 'applied' }).nodes();

            if (linhasVisiveis.length > 0) {
                var siglaTabela = $(linhasVisiveis[0]).find("td:eq(0)").attr('id');
                ajaxFunction(siglaTabela);
                ModalManager.fechar('modeloTableModalTabelas');
            } else {
                clickAviso('Atenção', 'Nenhuma tabela encontrada com essa busca', 'falha');
            }
        }
    });
    //..Kiro

    //Controlando o input checkbox pelo TR e TD (marcando somente de um único por vez!)
    $(document).on('click', '#modeloTableModalTabelas tr', function (event) { //marca o input em qualquer posição que o cursor estiver dentro do TR.

        /*  Esta linha verifica se a tag clicada contém algum checkbox dentro dela.
            $(this) refere-se ao elemento que foi clicado, e .find('input[type="checkbox"]') procura por checkboxes dentro desse
            elemento. Se houver um ou mais checkboxes, a condição será verdadeira.
         */
        if ($(this).find('input[type="checkbox"]').length > 0) {
            $('input[type="checkbox"]').prop('checked', false);
            $(this).find('input[type="checkbox"]').prop('checked', true);
        }
        else if ($(this).is('input[type="checkbox"]')) {
            $('input[type="checkbox"]').prop('checked', false);
            $(this).prop('checked', true);
        }
        //event.preventDefault();//não pode conter esta linha, pois trava o checkbox!!!
        var valorSelecionado = event.target.id;

        ajaxFunction(valorSelecionado);

        ModalManager.fechar('modeloTableModalTabelas');
        $(document).off('click.modalTriggerTabela');  //Desligamos o chamamento do último "click" de Ajax, PARA EVITAR QUE AS REQUISIÇÕES DE AJAX FIQUEM REPETINDO A CADA ACIONAMENTO.
    });
    //..
})();
