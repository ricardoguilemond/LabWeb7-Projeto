// Scripts de navegacao e estilizacao de botoes (o loading e encerrado no layout.js)

$(document).ready(function () {
    //-----------------------------------------------------------------------------------------------
    //Para saltar de campo em campo (somente input do type=text/TextBoxFor) nos formularios com ENTER
    //-----------------------------------------------------------------------------------------------
    $('body').on('keydown', 'input, select', function (e) {

        //IGNORA campos de busca de DataTables (permite ENTER funcionar normalmente neles)
        if ($('#modeloTableCompacta').closest($("input[type='search']")[0]).length > 0) {
            console.log("Ignorando campo de busca do DataTable no _Layout");
            return; // Nao interfere
        }
        if (e.which === 13) {
            var self = $(this),
            form = self.parents('form:eq(0)'),
            focusable,
            next;

            focusable = form.find('input[type=text], input[type=date], input[type=email], input[type=number], select').filter(':visible');
            next = focusable.eq(focusable.index(this) + 1);

            if (next.length) {
                next.focus();
            }

            return false; // bloqueia envio automatico do form
        }
    });
    //..
});
// coloca seta estilizada em todos os botoes de submit e botoes verdes dos formularios
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll('button[type="submit"], button.botao-verde').forEach(function (button) {
        if (!button.querySelector('.seta-gravacao')) {
            var seta = document.createElement('span');
            seta.classList.add('seta-gravacao');
            button.appendChild(seta);
        }
    });
});
//..
//Permite o salvamento atraves de F5 em todas as paginas, pois aciona o clique do botao de submit
document.addEventListener("keydown", function (event) {
    // Verifica se a tecla pressionada e F5
    if (event.key === "F5") {
        event.preventDefault(); // Impede o reload da pagina

        // Encontra o botao de submit e aciona o clique
        var botaoSalvar = document.querySelector(".botao-verde");
        if (botaoSalvar) {
            botaoSalvar.click();
        }
    }
});
//..
