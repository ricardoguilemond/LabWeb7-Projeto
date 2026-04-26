// Executa imediatamente — este script é carregado dinamicamente via $.load() junto com o HTML do modal
(function () {
    var medicosConfig = document.getElementById('modalMedicosConfig');
    if (!medicosConfig) return; // Proteção caso o elemento não exista
    var urlRetornoModalMedico = medicosConfig.dataset.urlRetornoModalMedico;

    //Constrói o Modal
    configTableModal('#tabelaMedicos');

    function preencherFormularioMedico(vm) {
        var $conteudo = $("#conteudoMedico");
        $conteudo.find("#buscaNomeMedico").val(vm.nomeMedico);
        $conteudo.find("#buscaCRM").val(vm.crm);
        $conteudo.find("#medicoId").val(vm.medicoId);  //o valor "id" aqui é o Id da primary key retornado!
    }

    //AJAX
    function ajaxFunction(valorSelecionado) {
        if (valorSelecionado.length > 0) {
            fetch(`${urlRetornoModalMedico}?id=${encodeURIComponent(valorSelecionado)}`, { //Javascript puro: substitui uso de Ajax, mais moderno e simples.
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data.vm.nomeMedico) {
                    clickAviso('Atenção', 'Não trouxe dados nesta busca', 'falha');
                    return;
                }
                // Preenchendo os campos após retornar do Controller
                preencherFormularioMedico(data.vm);

                ModalManager.fechar('modeloTableModalMedicos');
            })
            .catch(function () {
                clickAviso('Interrompido', 'Falha no carregamento dos dados', 'falha');
            });
        }
    }
    //..

    //Captura o que foi digitado no campo "search" ("input[type='search']") do Datatable e entrega no campo certo do formulário...
    $(document).on("keydown", "input[type='search']", function (event) {
        console.log("keydown");
        if (event.keyCode === 13) { // ENTER
            var valorSelecionado = event.target.value.trim(); // remove espaços extras
            var palavras = valorSelecionado.split(/\s+/).filter(p => p.length > 0); // remove strings vazias
            // Se vazio ou apenas uma palavra
            if (palavras.length <= 1) {
                ajaxFunction(valorSelecionado);
                ModalManager.fechar('modeloTableModalMedicos');
            }
        }
    });
    //..

    //Captura o que foi digitado no campo "search" ("input[type='search']") do Datatable e entrega no campo certo do formulário...
    $("input[type='search']").on("keydown", function (event) {
        if (event.keyCode === 13) {
            var table = $('#tabelaMedicos').DataTable();
            var linhasVisiveis = table.rows({ filter: 'applied' }).nodes();

            if (linhasVisiveis.length > 0) {
                // O campo certo é o da primeira coluna da linha da primeira célula da linha (td:eq(0))
                var siglaTabela = $(linhasVisiveis[0]).find("td:eq(0)").text().trim();

                ajaxFunction(siglaTabela);
                ModalManager.fechar('modeloTableModalMedicos');
            }
        }
    });
    //..

    //Controlando o input checkbox pelo TR e TD (marcando somente de um único por vez!)
    $(document).on('click', 'tr', function (event) { //marca o input em qualquer posição que o cursor estiver dentro do TR.

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
        ModalManager.fechar('modeloTableModalMedicos');

        $(document).off('click.modalTriggerMedico');  //Desligamos o chamamento do último "click" de Ajax, PARA EVITAR QUE AS REQUISIÇÕES DE AJAX FIQUEM REPETINDO A CADA ACIONAMENTO.
    });
    //..
})();
