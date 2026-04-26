$(document).ready(function () {
    var config = document.getElementById('planoExamesConfig');
    var urlIndex = config.dataset.urlIndex;
    var urlModelo = config.dataset.urlModelo;
    var urlAlterar = config.dataset.urlAlterar;

    var numeroItemFolha = $(".itemFolhaExame").val(1);   //declara no carregamento da página, o número da folha que está DEFAULT no ComboBox!

    //Carrega novo grid de acordo com os boxes
    $('.itemFolhaExame').change(function (event) {//class from RadioButton
        event.preventDefault();
        numeroItemFolha = $(".itemFolhaExame").val();  //obrigatório porque aqui pega em realtime o número da folha que ESTÁ selecionado no ComboBox!

        //Vamos atualizar o número da folha e chamar depois a partial view que vai atualizar AJAX somente a DIV chamada "contaDiv"
        fetch(urlIndex + '?numeroItemFolha=' + numeroItemFolha + "&partial=true", { //Javascript puro: substitui uso de Ajax, mais moderno e simples.
            method: 'GET'
        })
        .then(function (r) { return r.text(); })
        .then(function (data) {
            $('#modeloTable').DataTable().destroy();  //destrói o DataTables para conseguir atualizar o "data" paginando corretamente!
            document.getElementById('modeloTable').innerHTML = data; //a tabela construída/remontada/atualizada

            configTable();                            //reconstrói o DataTable com "draw()" com as configurações determinadas
        })
        .catch(function () { alert("Falhou carregamento do Grid"); });
        numeroItemFolha = $(".itemFolhaExame").val(); //atualiza em realtime o número da folha que FOI selecionado no ComboBox!

    });
});

//Declara os dois boxes com o default, e vamos sempre atualizar os dois Ids.
var FolhaId = 1;
//Atualiza os dois Combobox do DropDownList
function atualizaBoxFolha(id) {//recebe o valor e atualiza os combobox da folha de exame
    FolhaId = id.value;
    $("#id_folha").val(FolhaId);
    $("#nome_folha").val(FolhaId);
}
//
function clickModelo(x) {
    //"x" é a linha toda da tag (tem que passar a linha TODA de "x")
    //x.id é somente o "id" na tag da linha
    var config = document.getElementById('planoExamesConfig');
    window.location.href = config.dataset.urlModelo + '?registroID=' + x.id;
}
function clickConsulta(x) {
    var url = "ConsultarPlanoExames?id=" + x.id;
    window.open(url, "_self")
}
function clickAlterar(x) { //OK
    var config = document.getElementById('planoExamesConfig');
    window.location.href = config.dataset.urlAlterar + '?id=' + x.id;
}
function clickDelete(x) {
    return clickConfirm(x, null, "Excluir item de exame do Plano?", null, "ExcluirPlanoExames");
}
