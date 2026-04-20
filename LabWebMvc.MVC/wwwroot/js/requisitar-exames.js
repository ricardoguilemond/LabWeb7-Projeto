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
        Medico: false
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

    $("#buscaSiglaInstituicao, #buscaNomeInstituicao, #buscaSiglaTabela, #buscaNomeTabela, #buscaCRM, #buscaNomeMedico")
        .off('keydown')
        .on('keydown', function (event) {
            if (teclasPermitidas.includes(event.key)) {
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
                                            aplicarBusca('#modeloTableModalInstituicao', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalInstituicao', 'input[type="search"]');
                                    aplicarBusca('#modeloTableModalInstituicao', inputEscrito);
                                }
                                break;

                            case "buscaNomePosto":
                                if (!modaisCarregados.Posto) {
                                    $('#modalTriggerPosto').load("ModalPostos", function () {
                                        modaisCarregados.Posto = true;
                                        setTimeout(() => {
                                            ModalManager.abrir('modeloTableModalPostos', 'input[type="search"]');
                                            aplicarBusca('#modeloTableModalPostos', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalPostos', 'input[type="search"]');
                                    aplicarBusca('#modeloTableModalPostos', inputEscrito);
                                }
                                break;

                            case "buscaSiglaTabela":
                            case "buscaNomeTabela":
                                if (!modaisCarregados.Tabela) {
                                    $('#modalTriggerTabela').load("ModalTabelas", function () {
                                        modaisCarregados.Tabela = true;
                                        setTimeout(() => {
                                            ModalManager.abrir('modeloTableModalTabelas', 'input[type="search"]');
                                            aplicarBusca('#modeloTableModalTabelas', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalTabelas', 'input[type="search"]');
                                    aplicarBusca('#modeloTableModalTabelas', inputEscrito);
                                }
                                break;

                            case "buscaCRM":
                            case "buscaNomeMedico":
                                if (!modaisCarregados.Medico) {
                                    $('#modalTriggerMedico').load("ModalMedicos", function () {
                                        modaisCarregados.Medico = true;
                                        setTimeout(() => {
                                            ModalManager.abrir('modeloTableModalMedicos', 'input[type="search"]');
                                            aplicarBusca('#modeloTableModalMedicos', inputEscrito);
                                        }, 100);
                                    });
                                } else {
                                    ModalManager.abrir('modeloTableModalMedicos', 'input[type="search"]');
                                    aplicarBusca('#modeloTableModalMedicos', inputEscrito);
                                }
                                break;
                        }
                    }
                }, 0);
            }
        });

    $("#buttonLimpaCupom").on("click", function (event) {
        event.preventDefault();
        $.ajax({
            url: urlMontarItensCupom + '?id=0',
            type: "GET",
            async: true,
            dataType: "html",
            success: function (data) {
                $("#conteudoItensCupomCaixa").html(data);
            },
            failure: function () {
                clickAviso('Interrompido', 'Falha ao tentar zerar os dados do cupom', 'falha');
            }
        });
    });
});
