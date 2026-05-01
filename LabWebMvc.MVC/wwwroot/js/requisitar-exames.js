const ModalManager = (() => {
    const modais = {};
    const listenersRegistrados = {}; // evita acumulo de listeners shown.bs.modal

    function abrir(idModal) {
        const modalElement = document.getElementById(idModal);
        if (!modalElement) return;

        // Reutiliza instância existente ou cria nova
        let instancia = bootstrap.Modal.getInstance(modalElement);
        if (!instancia) {
            instancia = new bootstrap.Modal(modalElement);
        }
        modais[idModal] = instancia;
        instancia.show();
    }

    function fechar(idModal) {
        if (modais[idModal]) {
            modais[idModal].hide();
        }
    }

    return {
        abrir,
        fechar
    };
})();

$(document).ready(function () {
    var examesConfig = document.getElementById('examesConfig');
    var urlMontarItensCupom = examesConfig.dataset.urlMontarItensCupom;

    const teclasPermitidas = ['Enter', 'ArrowUp', 'ArrowDown'];
    const modaisCarregados = {
        Instituicao: false,
        Tabela: false,
        Medico: false,
        Posto: false
    };

    const aplicarBusca = (idTabela, texto) => {
        const tabela = $.fn.dataTable.isDataTable(idTabela) ? $(idTabela).DataTable() : null;

        if (tabela) {
            tabela.search(texto).draw();
        } else {
            const intervalo = setInterval(() => {
                const tabelaPronta = $.fn.dataTable.isDataTable(idTabela) ? $(idTabela).DataTable() : null;
                if (tabelaPronta) {
                    tabelaPronta.search(texto).draw();
                    clearInterval(intervalo);
                }
            }, 100);
        }
    };

    // Reseta flags e esvazia containers ao limpar campos manualmente
    $("#buscaSiglaInstituicao, #buscaNomeInstituicao").on('input', function () {
        if ($(this).val().trim() === '') {
            modaisCarregados.Instituicao = false;
            $('#modalTriggerInstituicao').empty();
        }
    });
    $("#buscaSiglaTabela, #buscaNomeTabela").on('input', function () {
        if ($(this).val().trim() === '') {
            modaisCarregados.Tabela = false;
            $('#modalTriggerTabela').empty();
        }
    });
    $("#buscaCRM, #buscaNomeMedico").on('input', function () {
        if ($(this).val().trim() === '') {
            modaisCarregados.Medico = false;
            $('#modalTriggerMedico').empty();
        }
    });
    $("#buscaNomePosto").on('input', function () {
        if ($(this).val().trim() === '') {
            modaisCarregados.Posto = false;
            $('#modalTriggerPosto').empty();
        }
    });

    //Feito pelo Kiro em 01/05/2026
    // Listener de ENTER para campos de médico com busca direta por nome parcial.
    // - Campo vazio → abre o modal de busca
    // - Campo com texto → tenta buscar diretamente sem abrir o modal
    $("#buscaNomeMedico, #buscaCRM")
        .off('keydown')
        .on('keydown', function (event) {
            if (!teclasPermitidas.includes(event.key)) return;
            event.preventDefault();
            event.stopImmediatePropagation();

            const inputEscrito = event.target.value.trim();

            if (inputEscrito.length === 0) {
                // Campo vazio: abre o modal
                if (!modaisCarregados.Medico) {
                    $('#modalTriggerMedico').load("ModalMedicos", function () {
                        modaisCarregados.Medico = true;
                        setTimeout(() => {
                            ModalManager.abrir('modeloTableModalMedicos');
                            aplicarBusca('#tabelaMedicos', '');
                        }, 100);
                    });
                } else {
                    ModalManager.abrir('modeloTableModalMedicos');
                    aplicarBusca('#tabelaMedicos', '');
                }
                return;
            }

            // Campo com texto: busca direta pelo nome/CRM parcial
            $.ajax({
                url: 'RetornoDoModalMedico',
                type: 'GET',
                data: { id: inputEscrito },
                cache: false,
                dataType: 'json',
                success: function (data) {
                    if (!data || !data.vm || !data.vm.nomeMedico) {
                        // Não encontrou: abre o modal com o texto já filtrado
                        if (!modaisCarregados.Medico) {
                            $('#modalTriggerMedico').load("ModalMedicos", function () {
                                modaisCarregados.Medico = true;
                                setTimeout(() => {
                                    ModalManager.abrir('modeloTableModalMedicos');
                                    aplicarBusca('#tabelaMedicos', inputEscrito);
                                }, 100);
                            });
                        } else {
                            ModalManager.abrir('modeloTableModalMedicos');
                            aplicarBusca('#tabelaMedicos', inputEscrito);
                        }
                        return;
                    }
                    // Encontrou: preenche os campos diretamente
                    var vm = data.vm;
                    var $conteudo = $("#conteudoMedico");
                    $conteudo.find("#buscaNomeMedico").val(vm.nomeMedico);
                    $conteudo.find("#buscaCRM").val(vm.crm);
                    $conteudo.find("#medicoId").val(vm.medicoId);

                    // Avança o foco para o botão de salvar (fim do fluxo)
                    setTimeout(function () {
                        var campo = document.getElementById('clickImprimeCupom');
                        if (campo) campo.focus();
                    }, 100);
                },
                error: function () {
                    clickAviso('Interrompido', 'Falha ao buscar médico', 'falha');
                }
            });
        });
    //..Kiro

    $("#buscaSiglaInstituicao, #buscaNomeInstituicao, #buscaSiglaTabela, #buscaNomeTabela, #buscaNomePosto")
        .off('keydown')
        .on('keydown', function (event) {
            if (teclasPermitidas.includes(event.key)) {
                event.preventDefault(); // impede submit do formulário ao pressionar ENTER
                event.stopImmediatePropagation(); // impede outros listeners no mesmo elemento (ex: _Layout)
                setTimeout(() => {
                    const inputEscrito = event.target.value.trim();
                    const palavras = inputEscrito.split(/\s+/).filter(p => p.length > 0);
                    const campoId = $(this).attr("id");

                    if (palavras.length <= 1) {
                        switch (campoId) {
                            case "buscaSiglaInstituicao":
                            case "buscaNomeInstituicao":
                                if (!modaisCarregados.Instituicao) {
                                    $('#modalTriggerInstituicao').load("ModalInstituicoes", function () {
                                        modaisCarregados.Instituicao = true;
                                        setTimeout(() => {
                                            ModalManager.abrir('modeloTableModalInstituicao');
                                            aplicarBusca('#tabelaInstituicao', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalInstituicao');
                                    aplicarBusca('#tabelaInstituicao', inputEscrito);
                                }
                                break;

                            case "buscaNomePosto":
                                if (!modaisCarregados.Posto) {
                                    $('#modalTriggerPosto').load("ModalPostos", function () {
                                        modaisCarregados.Posto = true;
                                        setTimeout(() => {
                                            ModalManager.abrir('modeloTableModalPostos');
                                            aplicarBusca('#tabelaPostos', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalPostos');
                                    aplicarBusca('#tabelaPostos', inputEscrito);
                                }
                                break;

                            case "buscaSiglaTabela":
                            case "buscaNomeTabela":
                                if (!modaisCarregados.Tabela) {
                                    $('#modalTriggerTabela').load("ModalTabelas", function () {
                                        modaisCarregados.Tabela = true;
                                        setTimeout(() => {
                                            ModalManager.abrir('modeloTableModalTabelas');
                                            aplicarBusca('#tabelaPreco', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalTabelas');
                                    aplicarBusca('#tabelaPreco', inputEscrito);
                                }
                                break;
                        }
                    }
                }, 0);
            }
        });

    $("#buttonLimpaCupom").on("click", function (event) {
        event.preventDefault();
        fetch(urlMontarItensCupom + '?id=0', { //Javascript puro: substitui uso de Ajax, mais moderno e simples.
            method: 'GET'
        })
        .then(function (r) { return r.text(); })
        .then(function (data) {
            document.getElementById('conteudoItensCupomCaixa').innerHTML = data;
        })
        .catch(function () {
            clickAviso('Interrompido', 'Falha ao tentar zerar os dados do cupom', 'falha');
        });
    });
});
