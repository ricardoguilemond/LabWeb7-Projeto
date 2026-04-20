$(document).ready(function () {
    /**************************************************************************************** */
    // OK:SUBMIT NO ÚNICO FORM COM VÁRIOS EVENTOS EM OUTRAS "PARTIAL TELAS" DISTRIBUÍDAS
    /**************************************************************************************** */
    var formRequisitar = document.getElementById('formRequisitar');
    var urlSalvar = formRequisitar.dataset.urlSalvar;

    // Handler compartilhado para ambos os botões de salvar (clickSubmit e clickImprimeCupom)
    function salvarRequisicao() {
        var listaCupom = [];

        $('tr[name="itemLancarCupom"]').each(function(){
            var id = $(this).attr('id');
            var descricao = $(this).find('.td-descricao').text();
            var valor = $(this).find('.td-valor').text();
            listaCupom.push({ Id: id, Descricao: descricao, ValorItem: valor });
        });

        $.ajax({
            url: urlSalvar,
            type: "POST",
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            async: true,
            data: $('#formRequisitar').serialize(),
            success: function (data) {
                var titulo = data['titulo'] ?? 'Atenção';
                var mensagem = data['mensagem'];
                var actionPos = data['action'];
                var sucesso = data['sucesso'];
                var tipo = sucesso ? 'sucesso' : 'falha';

                //Atualiza a grid e exibe mensagem
                $('#modeloTableRequisitar').DataTable().ajax.reload();
                clickAviso(titulo, mensagem, tipo, actionPos);
            },
            error: function (request, status, error) {
                clickAviso('Interrompido', 'Falha na execução', 'critica', null);
            }
        });
    }

    $('#clickSubmit').on("click", function (event) {
        event.preventDefault();
        salvarRequisicao();
    });

    // Botão "Salvar e Imprimir Cupom" usa o mesmo handler
    $(document).on("click", "#clickImprimeCupom", function (event) {
        event.preventDefault();
        salvarRequisicao();
    });
    //..

});  //Fim do $(document).ready
