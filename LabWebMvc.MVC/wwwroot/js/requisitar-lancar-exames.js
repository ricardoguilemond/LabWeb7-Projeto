var lancarExamesConfig = document.getElementById('lancarExamesConfig');
var urlMontarItensCupom = lancarExamesConfig.dataset.urlMontarItensCupom;
var urlPlanoExamesItens = lancarExamesConfig.dataset.urlPlanoExamesItens;

function montarCupomPorId(id) {
    if (!id) {
        console.warn('ID inválido ou ausente.');
        return;
    }
    fetch(urlMontarItensCupom + '?id=' + id, { //Javascript puro: substitui uso de Ajax, mais moderno e simples.
        method: 'GET'
    })
    .then(function (r) { return r.text(); })
    .then(function (data) {
        document.getElementById('conteudoItensCupomCaixa').innerHTML = data;
    })
    .catch(function () {
        clickAviso('Interrompido', 'Falha no carregamento dos dados do cupom', 'falha');
    });
}

$(document).ready(function () {

    configTableCompacta();   //constrói o DataTable com parâmetros de uma tabela compacta/simples/reduzida

    //Seleciona os itens de exames e acumula para salvar e montar o mapa para lançamentos de resultados.
    //Delegação correta: captura clicks em qualquer linha com name="itemLancarExame", mesmo após renderizações do DataTables
    $(document).on('click', '[name="itemLancarExame"]', function (event) {
        event.preventDefault();

        const id = this.getAttribute("data-id");

        if ($(this).hasClass("noSelectedLinha")) {
            // Seleciona o item e adiciona ao cupom
            $(this).removeClass('noSelectedLinha').addClass('selectedLinha');
            $(this).find('[name="itemLE"]').prop('checked', true);
            montarCupomPorId(id);
        } else {
            // Desseleciona o item (não adiciona novamente ao cupom)
            $(this).removeClass('selectedLinha').addClass('noSelectedLinha');
            $(this).find('[name="itemLE"]').prop('checked', false);
        }
    });
    //..

    //Excuta Ajax no campo search para localizar o primeiro item visível do grid
    const campoSearch = document.getElementById('customSearchBox');
    campoSearch.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();

            const texto = campoSearch.value.trim();

            const primeiraLinha = document.querySelector('#modeloTableCompacta tbody tr:not(.dtr-hidden):not(.dataTables_empty)');
            if (!primeiraLinha) {
                //console.warn('Nenhuma linha visível após filtro.');
                return;
            }

            const id = primeiraLinha.dataset.id;
            if (!id) {
                //console.warn('ID não encontrado na linha. Verifique se há atributo data-id.');
                return;
            }

            montarCupomPorId(id);
        }
    });
    //..

    //declarações para controle de formulário no submmit ::: TROCA ENTRE "Formulário Completo" e "Formulário Simples"
    var numeroItemTabela = $(".itemTabelaExame").val(1); //declara apenas, porque aqui ainda fica o default do Combobox, colocamos 1 = SUS!
    var numeroItemFolha = $(".itemFolhaExame").val(1);   //declara no carregamento da página, o número da folha que está DEFAULT no ComboBox!
    //..
    //------------------------------------------------------------------------------------------
    //começa pelo formulário simples (default, formulário simples)
    var contaPills = 'pills-simples-ttab';
    $(".NotView").hide("fast");   //classe que controla o "esconde-mostra" campos do formulário
    $("#pills-completo-ttab").removeClass('active');
    $("#pills-simples-ttab").addClass('active');
    //------------------------------------------------------------------------------------------
    //..
    //Datatables: Carrega novo grid de acordo com os boxes
    $('.itemFolhaExame, .itemTabelaExame').change(function (event) {//class from RadioButton
        event.preventDefault();
        numeroItemTabela = $(".itemTabelaExame").val();  //obrigatório porque aqui pega em realtime o número da folha que ESTÁ selecionado no ComboBox!
        numeroItemFolha = $(".itemFolhaExame").val();    //obrigatório porque aqui pega em realtime o número da folha que ESTÁ selecionado no ComboBox!

        //Vamos atualizar o número da folha e chamar depois a partial view que vai atualizar AJAX somente a DIV chamada "contaDiv"
        fetch(urlPlanoExamesItens + '?numeroItemFolha=' + numeroItemFolha + "&numeroTabela=" + numeroItemTabela + "&partial=true", { //Javascript puro: substitui uso de Ajax, mais moderno e simples.
            method: 'GET'
        })
        .then(function (r) { return r.text(); })
        .then(function (data) {
            $('#modeloTable').DataTable().destroy();  //destrói o DataTables para conseguir atualizar o "data" paginando corretamente!
            document.getElementById('modeloTable').innerHTML = data; //contaDiv é do "tbody" que monta/substitui a partial grid na posição

            configTable();                       //reconstrói o DataTable com "draw()" com as configurações determinadas
        })
        .catch(function () {
            clickAviso('Interrompido', 'Falha no carregamento do grid de exames', 'falha');
        });
        numeroItemFolha = $(".itemFolhaExame").val(); //atualiza em realtime o número da folha que FOI selecionado no ComboBox!
        numeroItemTabela = $(".itemTabelaExame").val();
    });
    //..
    //OK:Troca de abas montando o formulário simples ou completo
    $('#pills-simples-ttab').on("click", function (event) {//class from button das abas
        event.preventDefault;
        var pills = event.target.getAttribute("id");

        //console.log("simples:pills: ", pills);
        //console.log("simples:contaPills: ", contaPills);

        if (contaPills != pills) {
            $(".NotView").hide(400);  //esconde e mostra o campo no formulário (200="fast" e 600="slow")
            contaPills = event.target.getAttribute("id");
        }
    });
    $('#pills-completo-ttab').on("click", function (event) {//class from button das abas
        event.preventDefault;
        var pills = event.target.getAttribute("id");

        //console.log("completo:pills: ", pills);
        //console.log("completo:contaPills: ", contaPills);

        if (contaPills != pills) {
            $(".NotView").show(400);   //esconde e mostra o campo no formulário (200="fast" e 600="slow")
            contaPills = event.target.getAttribute("id");
        }
    });
    //..
}); //FIM do $(document).ready
