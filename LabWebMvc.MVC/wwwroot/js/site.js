/*
   Todas as funções de formatações de campos com nomes genéricos
 */
/**
 * All functions
 */

/* 
     Máscaras utilizadas nas views e modais apenas nas exibições dos campos
     Exemplo utilizado em ConsultarPaciente.cshtml 
*/
$(function () { //formato compacto para verificar se o documento está pronto, o mesmo que "$(document).ready(function()"

    /* máscaras */
    $(".data").mask("99/99/9999", { placeholder: " ", autoclear: false });
    $(".datahora").mask("99/99/9999 00:00", { placeholder: " ", autoclear: false });
    $(".datahorasegundos").mask("99/99/9999 00:00:00", { placeholder: " ", autoclear: false });
    $(".telefone").mask("99 9-9999-9999", { placeholder: " ", autoclear: false });
    $(".cpf").mask("999.999.999-99", { placeholder: " ", autoclear: false });
    $(".cnpj").mask("99.999.999/9999-99", { placeholder: " ", autoclear: false });
    $(".cns").mask("999.999.999.999", { placeholder: " ", autoclear: false });
    $(".sus").mask("###.###.###.###.###", { placeholder: " ", autoclear: false });
    $(".cep").mask("99.999-999", { placeholder: " ", autoclear: false });
    $(".crm").mask("99-99999999", { placeholder: " ", autoclear: false });
    /* fim das máscaras */

    /**
       Carrega a função de MODAL e deixa disponível para
       quando um ACTION CONTROLLER fizer o acionamento da tela de mensagem.
       Qualquer tela de mensagem HTML deve conter o "id" com nome "myModal"
    */
    var modal = document.getElementById("myModal");
    if (modal != null && modal.id != null && modal.id == "myModal") {
        //controla o fechamento pelo botão close "X" com CLASS="close"
        var span1 = document.getElementsByClassName("close")[0];
        span1.onclick = function () {
            modal.style.display = "none";
        }
        //controla o fechamento pelo botão de Fechar com CLASS="myFechar"
        var span2 = document.getElementsByClassName("myFechar")[0];
        span2.onclick = function (event) {
            modal.style.display = "none";
        }
    }
    //...


}); //Finaliza fechamento do "$(document).ready(function()"


/**
 * FUNCIONA, NÃO ALTERAR!
 * Formata um valor numérico ou string para o formato de moeda em REAL (R$).
 * ex: formatarMoeda("1000.00") ou formatarMoeda(1000.00)
 *     retorna: "1.000,00"
 * @param {any} valor
 * @returns {string}
 */
function formatarMoeda(valor) {
    // Garante que o valor seja um número
    if (typeof valor === "string") {
        valor = valor.replace(/[^\d.-]/g, ""); // Remove caracteres não numéricos
    }
    const numero = parseFloat(valor);

    if (isNaN(numero)) {
        return "0,00"; // Retorna um valor padrão caso o número não seja válido
    }

    // Formata para moeda brasileira
    return numero
        .toFixed(2) // Garante duas casas decimais
        .replace(".", ",") // Substitui o ponto por vírgula
        .replace(/\B(?=(\d{3})+(?!\d))/g, "."); // Adiciona os pontos dos milhares
}
//..

/**
 * MontaUrl ::: Monta uma URL de acordo com a Action enviada
 * @param {any} action
 * @returns
 */
function MontaUrl(action) {
    var url = window.location.protocol + '//' + window.location.hostname + (window.location.port ? ":" + window.location.port + '/' : '');
    url = url + action;
    console.log("url: ", url);
    return url;
}
//..


/** FUNCIONANDO, NÃO ALTERAR!
 * clickConfirm ::: função genérica Javascript para realizar diversos serviços e também mensagens ajax
 * @param {any} x
 * @param {any} titulo
 * @param {any} pergunta
 * @param {any} mensagemSucesso
 * @param {any} icone
 * @param {any} action
 * @param {any} variavel
 */
function clickConfirm(x, titulo, pergunta, icone, action, nomeVariavel, valor) {
    if (titulo == null) titulo = 'Atenção';
    titulo = '<span style="font: normal 28px calibri, arial, sans-serif; color: gray;">' + titulo + '</span>';
    if (pergunta == null) pergunta = 'Confirma?';
    pergunta = '<span style="font: normal 22px calibri, arial, sans-serif; color: #646464;">' + pergunta + '</span>';
    if (icone == null) icone = 'question';
    if (nomeVariavel == null) nomeVariavel = 'id';
    if (valor != null) {
        nomeVariavel = nomeVariavel + '=' + valor;
    } else {
        nomeVariavel = nomeVariavel + '=' + x.id;
    }
    Swal.fire({
        title: titulo,
        html: pergunta,
        icon: icone,
        showCancelButton: true,
        confirmButtonColor: '#008b3f',
        confirmButtonText: 'Sim',
        cancelButtonText: 'Não'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                async: false,
                cache: false,
                type: "Get",     //opções podem ser: Get, Post, Delete
                url: action + '?' + nomeVariavel,      //tem nome de rota definida na action, que vai executar o serviço!
                dataType: "json",
                contentType: 'application/json; charset=utf-8',
                success: function (data) {
                    //dados de retorno do Json!
                    var json = data;
                    var titulo = json['titulo'];
                    var mensagem = json['mensagem'];
                    var sucesso = json['sucesso'];
                    console.log("mensagem: ", mensagem); //se algo der errado a mensagem entra no console e fica visível no navegador sem o site carregado
                    var icone = '<img src="images/icones/error-icon.png" width="100px" border="0" style="background-color: white;">';
                    if (sucesso == true) icone = '<img src="images/icones/success-icon.png" width="100px" border="0" style="background-color: white;">';
                    titulo = (titulo == null) ? 'Atenção' : titulo;
                    Swal.fire({
                        title: titulo,
                        html: mensagem,
                        iconHtml: icone,
                        confirmButtonColor: '#008b3f',
                        confirmButtonText: 'Fechar',
                        timer: 7000,        //7 segundos!
                        didOpen: () => {
                            timerInterval = setInterval(() => {
                            }, 10)
                        },
                        willClose: () => {
                            clearInterval(timerInterval);
                            if (sucesso == true)
                                location.reload();
                        }
                    }).then(function () { //executar função pós sucesso
                        if (sucesso == true)
                            location.reload();
                    });
                },
                error: function () {//outros erros não esperados de fora da action
                    console.log("error, interrompido");
                    Swal.fire({
                        title: 'Interrompido',
                        html: 'Execução falhou',
                        iconHtml: '<img src="images/icones/error-icon.png" width="100px" border="0" style="background-color: white;">',
                        confirmButtonColor: '#008b3f',
                        confirmButtonText: 'Fechar',
                        timer: 7000,        //7 segundos!
                        didOpen: () => {
                            timerInterval = setInterval(() => {
                            }, 10)
                        },
                        willClose: () => {
                            clearInterval(timerInterval);
                            location.reload();
                        }
                    });
                }
            });
        }
        else {
            Swal.fire({
                title: 'Interrompido',
                html: 'Nada foi feito',
                icon: 'info',
                confirmButtonColor: '#008b3f',
                confirmButtonText: 'Fechar',
                timer: 7000,        //7 segundos!
                didOpen: () => {
                    timerInterval = setInterval(() => {
                    }, 10)
                },
                willClose: () => {
                    clearInterval(timerInterval);
                }
            });
        }
    });
};


/** FUNCIONANDO, NÃO ALTERAR!
 * clickAviso ::: função genérica Javascript para realizar diversos serviços de mensagens ajax
 * USO: clicakAviso("titulo da mensagem", "mensagem", "normal ou falha ou critica", action de redirecionamento")
 * 
 * @param {any} titulo
 * @param {any} mensagem
 * @param {any} tipo
 * @param {any} action
 */
function clickAviso(titulo, mensagem, tipo, action) {
    if (titulo == null) titulo = 'Atenção';
    titulo = '<span style="color: silver;">' + titulo + '</span>';
    if (mensagem == null) mensagem = '';
    //icon pode ser: 'success', 'error', 'warning', 'info' or 'question', got 'danger', ou podem ser imagens png
    if (tipo == null || tipo == 'normal') icone = '../images/icones/attention-icon.png';
    if (tipo == 'sucesso') icone = '../images/icones/success-icon.png';
    if (tipo == 'falha') icone = '../images/icones/attention-icon.png';
    if (tipo == 'critica') icone = '../images/icones/fatal-errors-icon.png';
    //console.log("clickAviso ::: action: ", action);
    Swal.fire({
        title: titulo,
        html: mensagem,
        //icon: 'success',
        imageUrl: icone,
        imageHeight: 120,
        showCancelButton: false,
        confirmButtonColor: '#008b3f',
        confirmButtonText: 'Fechar',
        timer: 7000,        //7 segundos!
        didOpen: () => {
            timerInterval = setInterval(() => {
            }, 10)
        },
        willClose: () => {
            clearInterval(timerInterval);
            if (action != null && action != "") {//após fechar a tela depois de 7 (timer) segundos, redireciona pela action enviada!
                location.replace(action);
            }
        }
    }).then(function (result) {//se fechou pelo botão, redireciona pela action enviada!
        //console.log("entrei no 'result': ", result.value);
        //console.log("action modificada: ", action);
        if (result.value && action != null && action != "") {
            location.replace(action);
        }
    });
};


//função genérica Javascript para realizar diversos serviços de mensagens ajax
function clickAction(x, titulo, mensagem, icone, action) {
    if (titulo == null) titulo = 'Atenção';
    titulo = '<span style="font: normal 28px calibri, arial, sans-serif; color: gray;">' + titulo + '</span>';
    if (mensagem == null) mensagem = 'Operação realizada com sucesso';
    if (icone == null) icone = 'info';

    $.ajax({
        async: false,
        cache: false,
        type: "Get",     //opções podem ser: Get, Post, Delete
        url: action,     //tem nome de rota definida na action, que vai executar o serviço!
        dataType: "text",
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            //dados de retorno do Json!
            var json = data;  // $.parseJSON(data);
            var sucesso = json['sucesso'];
            var mensagem = json['mensagem'];
            var titulo = json['titulo'];
            var icone = '<img src="images/icones/error-icon.png" width="100px" border="0" style="background-color: white;">';
            if (sucesso) icone = '<img src="images/icones/success-icon.png" width="100px" border="0" style="background-color: white;">';
            titulo = (titulo == null) ? 'Atenção' : json['titulo'];
            Swal.fire({
                title: titulo,
                html: mensagem,
                iconHtml: icone,
                confirmButtonColor: '#008b3f',
                confirmButtonText: 'Fechar'
            }).then(function () { //mensagem oriunda da action/controller
                //location.reload();
            });
        },
        error: function (req, status, error) {//outros erros de fora da action
            console.log("error: ", error);
            Swal.fire({
                title: 'Interrompido',
                html: 'Falha na execução',
                icon: 'danger',
                confirmButtonColor: '#008b3f',
                confirmButtonText: 'Fechar'
            });
        }
    });
};




/** FUNCIONANDO, NÃO ALTERAR! 
 * CallMethodJson ::: Aciona uma Action método e dele retorna um Json para mensagem e redirect:
 * 
 * @param {any} action
 * @param {any} dados
 * @param {any} dadosForm
 */
function CallMethodJson(action, dados, dadosForm) {
    var action = action;
    var dados = dados;
    var dadosForm = dadosForm;
    if (dados != null && dadosForm != null)
        action = action + '?dados=' + dados + '&dadosForm=' + dadosForm;
    $.ajax({
        async: false,       //não faremos assíncrono para esperarmos o resultado completo do método action.
        cache: false,       //não usar cache para evitar que fique guardando e mostrando os mesmos resultados.
        type: "Post",       //pode ser Post ou Get, porém no método action também deve ser igual ou não entrará nele.
        url: action,        //leva o nome de rota definida na action, que vai executar o serviço!
        dataType: 'json',   //Tipo de dado que deve ser retornado do servidor em "data", se colocar "text" terá que formatar json assim: var json = $.parseJSON(data);
        contentType: 'application/json; charset=utf-8',
        success: function (data) { //"data" traz os dados de retorno do Json do método action!
            var json = data;
            var titulo = json['titulo'];
            var mensagem = json['mensagem'];
            var sucesso = json['sucesso'];
            var actionPos = json['action'];
            titulo = (titulo == null) ? 'Atenção' : json['titulo'];
            var tipo = 'falha';
            if (sucesso == true) tipo = 'normal';

            clickAviso(titulo, mensagem, tipo, actionPos);  //mensagem normal com desvio para outra action
        },
        error: function () {//outros erros de fora da action
            clickAviso('Interrompido', 'Falha na execução', 'critica', actionPos);  //mensagem crítica e com desvio para outra action
        }
    });
};

/**
 * FUNCIONANDO, NÃO ALTERAR!
 * Formata dados enquanto usuário digita no campo, basta informar a máscara.
 *  (funiona também no @Html.TextBoxFor - tem um exemplo na View "AlterarPaciente.cshtml" )
 *  USO:   @onkeypress="formatar(this, '##/##/####', event)"
 */
function formatar(objeto, sMask, evtKeyPress) {
    var i, nCount, sValue, fldLen, mskLen, bolMask, sCod, nTecla;
    //funcao para formatar campo CPF, DATA, TEL, CEP, COD
    if (document) { // Internet Explorer
        nTecla = evtKeyPress.keyCode;
    } else if (document.layers) { // Nestcape
        nTecla = evtKeyPress.which;
    } else {
        nTecla = evtKeyPress.which;
        if (nTecla == 8) {
            return true;
        }
    }
    sValue = objeto.value;
    // Limpa todos os caracteres de formata‡ão que
    // j  estiverem no campo.
    sValue = sValue.toString().replace("-", "");
    sValue = sValue.toString().replace("-", "");
    sValue = sValue.toString().replace(".", "");
    sValue = sValue.toString().replace(".", "");
    sValue = sValue.toString().replace("/", "");
    sValue = sValue.toString().replace("/", "");
    sValue = sValue.toString().replace(":", "");
    sValue = sValue.toString().replace(":", "");
    sValue = sValue.toString().replace("(", "");
    sValue = sValue.toString().replace("(", "");
    sValue = sValue.toString().replace(")", "");
    sValue = sValue.toString().replace(")", "");
    sValue = sValue.toString().replace(" ", "");
    sValue = sValue.toString().replace(" ", "");
    fldLen = sValue.length;
    mskLen = sMask.length;
    i = 0;
    nCount = 0;
    sCod = "";
    mskLen = fldLen;
    while (i <= mskLen) {
        bolMask = ((sMask.charAt(i) == "-") || (sMask.charAt(i) == ".") || (sMask.charAt(i) == "/") || (sMask.charAt(i) == ":"))
        bolMask = bolMask || ((sMask.charAt(i) == "(") || (sMask.charAt(i) == ")") || (sMask.charAt(i) == " "))
        if (bolMask) {
            sCod += sMask.charAt(i);
            mskLen++;
        } else {
            sCod += sValue.charAt(nCount);
            nCount++;
        }
        i++;
    }
    objeto.value = sCod;
    if (nTecla != 8) { // backspace
        if (sMask.charAt(i - 1) == "9") { // apenas n£meros...
            return ((nTecla > 47) && (nTecla < 58));
        } else { // qualquer caracter...
            return true;
        }
    } else {
        return true;
    }
}

/**
 * Formata automaticamente a moeda Real ao digitar no campo 
 * USO:
 *         onKeyPress="return(formatarMoedaReal(this,'.',',',event))"
 */
function formatarMoedaReal(a, e, r, t) {
    let n = ""
        , h = j = 0
        , u = tamanho2 = 0
        , l = ajd2 = ""
        , o = window.Event ? t.which : t.keyCode;
    if (13 == o || 8 == o)
        return !0;
    if (n = String.fromCharCode(o),
        -1 == "0123456789".indexOf(n))
        return !1;
    for (u = a.value.length,
        h = 0; h < u && ("0" == a.value.charAt(h) || a.value.charAt(h) == r); h++)
        ;
    for (l = ""; h < u; h++)
        -1 != "0123456789".indexOf(a.value.charAt(h)) && (l += a.value.charAt(h));
    if (l += n,
        0 == (u = l.length) && (a.value = ""),
        1 == u && (a.value = "0" + r + "0" + l),
        2 == u && (a.value = "0" + r + l),
        u > 2) {
        for (ajd2 = "",
            j = 0,
            h = u - 3; h >= 0; h--)
            3 == j && (ajd2 += e,
                j = 0),
                ajd2 += l.charAt(h),
                j++;
        for (a.value = "",
            tamanho2 = ajd2.length,
            h = tamanho2 - 1; h >= 0; h--)
            a.value += ajd2.charAt(h);
        a.value += (r + l.slice(u - 2, u))    /* "substr" is deprecated, used "slice" now */
    }
    return !1

}

/**
 * FUNCIONA, NÃO ALTERAR!
 * USO: Javascript
 *       
 *       formatarMoedaDecimal(this, ".", event); // Format with decimal point
 *       formatarMoedaDecimal(this, "", event); // Format without decimal separator
 */
function formatarMoedaDecimal(campoInput, separadorDecimal, event, quantDecimais = 2) {
    let valor = campoInput.value;
    let valorFormatado = "";
    let key = event.which;   // event.Key; deprecated // Get the pressed key

    quantDecimais = quantDecimais - 1;
    if (quantDecimais < 1) quantDecimais = 2;

    // Remove caracteres não numéricos e o separador decimal original (se existir)
    valor = valor.replace(/\D+/g, "");

    // Verifica se a tecla pressionada é um dígito ou backspace
    if (/\d|[Bb]/.test(key)) {
        // Adiciona o separador decimal se for fornecido e a tecla pressionada for um dígito
        if (separadorDecimal && /\d/.test(key)) {
            valorFormatado = valor.slice(0, valor.length - quantDecimais) + separadorDecimal + valor.slice(-quantDecimais);
        } else {
            valorFormatado = valor;
        }

        // Atualiza o valor do campo de entrada
        campoInput.value = valorFormatado;

        // Evita que o navegador processe a tecla pressionada (formatação automática)
        return false;
    } else {
        // Se não for um dígito ou backspace, permite o comportamento padrão da tecla
        return true;
    }

}


/* 
 * FUNCIONA, NÃO ALTERAR!
 * Carrega uma imagem imediata e chama o controller que vai colocar a imagem
 * na variável que será gravada na tabela.
*/
function buttonFileClick(s, event) {
    $('#fileLoader').after();   //era "click" deprecated
    $('#fileLoader').on('change', function (event) {
        var files = event.target.files;
        if (files.length > 0) {
            if (window.FormData !== undefined) {
                var data = new FormData();
                for (var x = 0; x < event.target.files.length; x++) {
                    data.append("file" + x, files[x]);
                }
            }
        }
        $.ajax({
            url: '@Url.Action("UploadImages", "Instituicoes")',
            type: 'GET',
            contentType: false,
            processData: false,
            data: data,
            dataType: 'json'
        });
    });
}


//$(document).data(function () {
//    jQuery.extend(jQuery.validator.messages, {
//        required: "Campo obrigatório",
//        remote: "Please fix this field",
//        email: "Entre com um e-mail válido",
//        url: "Entre com uma URL válida",
//        date: "Entre com uma data válida",
//        dateISO: "Please enter a valid date (ISO).",
//        number: "Entre com um número válido",
//        digits: "Entre somente com números",
//        creditcard: "Entre com o número do cartão de crédito",
//        equalTo: "Entre com o mesmo valor",
//        accept: "Entre com uma extensão válida",
//        maxlength: jQuery.validator.format("Entre com o máximo de {0} caracteres"),
//        minlength: jQuery.validator.format("Entre com o mínimo de {0} caracteres"),
//        rangelength: jQuery.validator.format("Intervalo aceito vai de {0} até {1} caracteres"),
//        range: jQuery.validator.format("Entre com um valor entre {0} e {1}"),
//        max: jQuery.validator.format("Entre com um valor máximo de {0}"),
//        min: jQuery.validator.format("Entre com um valor mínimo de {0}")
//    });
//});

//$(document).ready(function () {  /* significa que o evento a seguir só funcionará quando a página estiver 100% carregada */
//    /* Rotina do click do botão no Formulário do Pacientes Cadastro */
//    $('#Pacientes').on('click', function () {
//        $.ajax({
//            url: '@Url.Action("RetornaJson","Pacientes")',     /* "action name", "controller name" */
//            //url: '/Cadastros/Pacientes/Pacientes?id=0',
//            type: 'POST',
//            cache: false,
//            contentType: 'application/json; charset=utf-8',    /* retornar JSon */
//            dataType: 'json',
//            data: id = 0,
//            success: function (data, status, jqXHR) {
//                //console.log(data);
//                alert('SUCESSO: ' + data.responseText);
//                //var response = $.parseJSON(data);
//                //var mensagem = status.responseText;

//                //$.getJSON('/Cadastros/Pacientes/Pacientes.cshtml', function (data) {
//                alert(mensagem);
//                //   //$.each(data, function (i, cep) {

//                //   //});
//                //});
//            },
//            error: function (xmlHttpRequest, status, err) {
//                //console.log('erro');
//                alert('FALHOU: ' + data.responseText);
//            },
//            statusCode: {
//                404: function () {
//                    alert("Página não encontrada!");
//                }
//            }
//        });
//    });
//});




