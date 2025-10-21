/*
   Script Módulo de Senhas
 */
$(document).ready(function () {
    //$('#TB_Senhas').DataTable();
    //carregarDados();


    /* Carregando todas as funções imediatas */
    carregaListaSenhas();

});

function carregarDados() {
    $.ajax({
        url: "/Senhas",
        type: "GET",
        contentType: "application/json;charset=utf-8",
        dataType: "json",
        success: function (retorno) {
            var html = '';
            $.each(retorno, function (key, html) {
                NickUsuario = retorno.NickUsuario,
                NomeCompleto = retorno.NomeCompleto
            });
            $('.tbody').html(html);
        },
        error: function () {
            alert("Falhou o retorno de dados.");
        }
    });
}

/* Deletar um registro */
function Delele(ID) {
    var ans = confirm("Deseja apagar o registro?");
    if (ans) {
        $.ajax({
            url: "/Senhas/Delete/" + ID,
            type: "POST",
            contentType: "application/json;charset=UTF-8",
            dataType: "json",
            success: function (result) {
                carregarDados();
            },
            error: function (errormessage) {
                alert(errormessage.responseText);
            }
        });
    }
}

/* carrega lista de usuários no grid de Senhas */
function carregaListaSenhas() {
    var param = {
        0: "primeiro",
        1: "segundo",
        2: "terceiro"
    };
    $("#NickUsuario").prepend(param);
}
