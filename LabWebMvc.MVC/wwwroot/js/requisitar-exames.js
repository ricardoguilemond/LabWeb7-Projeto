const ModalManager = (() => {
    const modais = {};

    function abrir(idModal, campoFocusSelector = null) {
        const modalElement = document.getElementById(idModal);
        if (!modalElement) return;

        // Sempre recria a instância para garantir que o DOM esteja atualizado
        modais[idModal] = new bootstrap.Modal(modalElement);

        modalElement.addEventListener('shown.bs.modal', () => {
            if (campoFocusSelector) {
                const campo = modalElement.querySelector(campoFocusSelector);
                if (campo) campo.focus();
            }
        });
        modais[idModal].show();
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

    $("#buscaSiglaInstituicao, #buscaNomeInstituicao, #buscaSiglaTabela, #buscaNomeTabela, #buscaCRM, #buscaNomeMedico, #buscaNomePosto")
        .off('keydown')
        .on('keydown', function (event) {
            if (teclasPermitidas.includes(event.key)) {
                event.preventDefault(); // impede submit do formulário ao pressionar ENTER
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
                                            ModalManager.abrir('modeloTableModalInstituicao', 'input[type="search"]');
                                            aplicarBusca('#tabelaInstituicao', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalInstituicao', 'input[type="search"]');
                                    aplicarBusca('#tabelaInstituicao', inputEscrito);
                                }
                                break;

                            case "buscaNomePosto":
                                if (!modaisCarregados.Posto) {
                                    $('#modalTriggerPosto').load("ModalPostos", function () {
                                        modaisCarregados.Posto = true;
                                        setTimeout(() => {
                                            ModalManager.abrir('modeloTableModalPostos', 'input[type="search"]');
                                            aplicarBusca('#tabelaPostos', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalPostos', 'input[type="search"]');
                                    aplicarBusca('#tabelaPostos', inputEscrito);
                                }
                                break;

                            case "buscaSiglaTabela":
                            case "buscaNomeTabela":
                                if (!modaisCarregados.Tabela) {
                                    $('#modalTriggerTabela').load("ModalTabelas", function () {
                                        modaisCarregados.Tabela = true;
                                        setTimeout(() => {
                                            ModalManager.abrir('modeloTableModalTabelas', 'input[type="search"]');
                                            aplicarBusca('#tabelaPreco', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalTabelas', 'input[type="search"]');
                                    aplicarBusca('#tabelaPreco', inputEscrito);
                                }
                                break;

                            case "buscaCRM":
                            case "buscaNomeMedico":
                                if (!modaisCarregados.Medico) {
                                    $('#modalTriggerMedico').load("ModalMedicos", function () {
                                        modaisCarregados.Medico = true;
                                        setTimeout(() => {
                                            ModalManager.abrir('modeloTableModalMedicos', 'input[type="search"]');
                                            aplicarBusca('#tabelaMedicos', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalMedicos', 'input[type="search"]');
                                    aplicarBusca('#tabelaMedicos', inputEscrito);
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
