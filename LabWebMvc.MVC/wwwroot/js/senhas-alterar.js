$(document).ready(function () {
    $(document).on('click', '#clickSubmit', function (event) {
        //event.preventDefault() : permite que a função "CallMethodJson" no Javascript possa ser modificada no caminho, desviando uma rota por exemplo.
        event.preventDefault();  //OBRIGATÓRIO AQUI! Cancela o evento se ele for cancelável, ou seja, que a ação padrão que pertence ao evento não ocorrerá.
        var dados = $('#dados').val();
        var dadosForm = [];
        dadosForm.push('SenhaUsuario:' + $('#SenhaUsuario').val());
        dadosForm.push('SenhaRepete:' + $('#SenhaRepete').val());
        dadosForm.push('BoxGerarSenhaAutomatica:' + $('#BoxGerarSenhaAutomatica').val());
        dadosForm.push('BoxEnviarEmail:' + $('#BoxEnviarEmail').val());
        CallMethodJson('UsuarioSalvarSenha', dados, dadosForm);
    })
});
