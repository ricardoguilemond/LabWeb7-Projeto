/* 
 * Método que constrói o DataTables com as configurações necessárias 
 */
function configTable() {
    //Colocar o Tooltip Balloon destacado fora da posição do mouse hover para ter melhor visão
    $('[data-toggle="tooltip"]').tooltip();
    //Configura os boxes de input para cada busca de célula da coluna
    $('#modeloTable tfoot th').each(function () {
        var title = $(this).text();
        //Se não controlar aqui com max-width ele não respeita a formatação do layout
        $(this).html('<input type="text" placeholder="' + title + '" style="max-width: 106px; padding-left: 4px; border: 1px solid gray; border-radius: 5px;" />');
    });
    $('#modeloTable').DataTable({
        initComplete: function () {//executa a busca nas células disponíveis no footer
            this.api()
                .columns()
                .every(function () {
                    var that = this;
                    $('input', this.footer()).on('keyup change clear', function () {
                        if (that.search() !== this.value) {
                            that.search(this.value).draw();
                        }
                    });
                });
        },
        fixedColumns: {
            left: -1,  //não permite fixar a primeira coluna
            right: 1,  //fixa a última coluna dos ícones de ações/botões de ações
        },
        autoWidth: true,
        responsive: true,
        layout: {
            topStart: {
                pageLength: {
                    menu: [[10, 25, 50, 100, 250, 500, -1], [10, 25, 50, 100, 250, 500, "Todos"]],
                }
            },
            //top2Start: 'pageLength',
            topEnd: {
                search: {
                    placeholder: 'Digite aqui sua busca'
                }
            },
            bottomStart: 'pageLength',
            bottomStart: 'info',
            bottomEnd: {
                paging: {
                    numbers: 5  //quantidade de números de páginas que ficam visíveis o tempo todo no rodapé
                }
            }
        },
        "language": {
            "search": "Busca:",
            "lengthMenu": "Mostrar _MENU_ resultados por página",
            "emptyTable": "<b style='color: red;'>Não haviam dados disponíveis para esta consulta</b>",
            "info": "Exibindo: _START_/_END_ de _TOTAL_ registros",
            "infoFiltered": "(Total de _MAX_ registros existentes na base de dados)",
            "infoThousands": ".",
            "thousands": ".",
            "decimal": ",",
            "loadingRecords": "Carregando...",
            "zeroRecords": "<b style='color: red;'>A consulta não retornou dados para o Grid</b>",
            //"infoEmpty": "<b style='color: red;'>Nenhum registro disponível</b>",
            "paginate": {
                "next": "<img src='images/icones/arrow_right_uno.png' width='28%' border='none' />",
                "previous": "<img src='images/icones/arrow_left_uno.png' width='28%' border='none' />",
                "first": "<img src='images/icones/arrow_left.png' width='28%' border='none' />",
                "last": "<img src='images/icones/arrow_right.png' width='28%' border='none' />",
            },
            //Fixa o header e o footer para auxiliar na identificação das colunas
            fixedHeader: {
                header: true,
                footer: true
            }
        }
    }).draw();
    $("input[type='search']")[0].focus(); //coloca o foco no search
}
//..
/* 
 * MÉTODO PARA MONTAR A TABELA DE FORMA COMPACTA SEM OS INPUTS DE PESQUISA E SEM PAGINAÇÕES  
 */
function configTableCompacta() {
    //Colocar o Tooltip Balloon destacado fora da posição do mouse hover para ter melhor visão
    $('[data-toggle="tooltip"]').tooltip();

    const tabela = $('#modeloTableCompacta').DataTable({
        //Configura os boxes de input para cada busca de célula da coluna (desabilita o search do header)
        dom: '<"top"i>rt<"bottom"lp><"clear">',
        language: {
            lengthMenu: "Mostrar _MENU_ resultados por página",
            emptyTable: "<b style='color: red;'>Não haviam dados disponíveis para esta consulta</b>",
            info: "Carregou: _TOTAL_ registros",
            infoFiltered: "(Total de _MAX_ registros existentes na base de dados)",
            infoThousands: ".",
            thousands: ".",
            decimal: ",",
            loadingRecords: "Carregando...",
            zeroRecords: "<b style='color: red;'>A consulta não retornou dados para o Grid</b>",
            paginate: {
                next: "<img src='images/icones/arrow_right_uno.png' width='28%' border='none' />",
                previous: "<img src='images/icones/arrow_left_uno.png' width='28%' border='none' />",
                first: "<img src='images/icones/arrow_left.png' width='28%' border='none' />",
                last: "<img src='images/icones/arrow_right.png' width='28%' border='none' />",
            },
            //Fixa o header e o footer para auxiliar na identificação das colunas
            fixedHeader: {
                header: true,
                footer: true
            }
        },
        searching: true, //deixa habilitado o campo de busca para que o campo personalizado de busca funcione!
        select: { /* Para os checkbox o JQuery exemplo está no "Index.cshtml" e corresponde ao "_PartialLancarExames.cshtml"  */
            selector: 'td, input.noSelectedLinha:not(:has(:checkbox))',
        },
        fixedColumns: {
            left: -1,  //não permite fixar a primeira coluna
            right: 1,  //fixa a última coluna dos ícones de ações/botões de ações
        },
        autoWidth: true,
        responsive: true,
        paging: false,           //para scrollY
        scrollCollapse: true,    //para scrollY
        scrollY: '50vh',         //para scrollY  //total de umas 17 linhas no grid da tabela
        scrollX: false,
    });   //.draw();
    //$("input[type='search']")[0].focus(); //coloca o foco no search
    //Configura o campo de busca personalizado, o input fica no cshtml com o id "customSearchBox".
    document.getElementById('customSearchBox').addEventListener('keyup', function () {
        tabela.search(this.value).draw();
    });
}
//..
/* 
 *  MODELO Exclusivo para as tabelas de MODAL 
 */
function configTableModal(modelo) {
    //Colocar o Tooltip Balloon destacado fora da posição do mouse hover para ter melhor visão
    $('[data-toggle="tooltip"]').tooltip();
    $(modelo).DataTable({
        initComplete: function () {//executa a busca nas células disponíveis no HEADER
            this.api()
                .columns()
                .every(function () {
                    var that = this;
                    $('input', this.header()).on('keyup change clear', function () {
                        if (that.search() !== this.value) {
                            that.search(this.value).draw();
                        }
                    });
                });
        },
        fixedColumns: {
            left: -1,  //não permite fixar a primeira coluna
            right: 1,  //fixa a última coluna dos ícones de ações/botões de ações
        },
        autoWidth: true,
        responsive: true,
        layout: {
            topStart: {
                search: {
                    placeholder: 'Digite aqui sua busca'
                }
            },
            topEnd: { /* obrigatório ter mesmo sem utilizar, porque desformata o Search */
            },
        },
        "language": {
            "search": "Busca:",
            "lengthMenu": "Mostrar _MENU_ resultados por página",
            "emptyTable": "<b style='color: red;'>Não haviam dados disponíveis para esta consulta</b>",
            "info": "Exibindo: _START_/_END_ de _TOTAL_ registros",
            "infoFiltered": "(Total de _MAX_ registros existentes na base de dados)",
            "infoThousands": ".",
            "thousands": ".",
            "decimal": ",",
            "loadingRecords": "Carregando...",
            "zeroRecords": "<b style='color: red;'>A consulta não retornou dados para o Grid</b>",
            "paginate": {
                "next": "<img src='images/icones/arrow_right_uno.png' width='28%' border='none' />",
                "previous": "<img src='images/icones/arrow_left_uno.png' width='28%' border='none' />",
                "first": "<img src='images/icones/arrow_left.png' width='28%' border='none' />",
                "last": "<img src='images/icones/arrow_right.png' width='28%' border='none' />",
            },
            //Fixa o header e o footer para auxiliar na identificação das colunas
            fixedHeader: {
                header: true,
                footer: true
            }
        }
    }).draw();
    $("input[type='search']")[0].focus(); //coloca o foco no search
}
//..

/* 
 * MÉTODO PARA MONTAR A TABELA DE CUPOM DE FORMA COMPACTA SEM OS INPUTS DE PESQUISA E SEM PAGINAÇÕES
 */
function configTableCupom() {
    //Colocar o Tooltip Balloon destacado fora da posição do mouse hover para ter melhor visão
    $('[data-toggle="tooltip"]').tooltip();

    $('#modeloTableCupom').DataTable({
        initComplete: function () {//executa a busca nas células disponíveis no HEADER
            this.api()
                .columns()
                .every(function () {
                    var that = this;
                    $('input', this.header()).on('keyup change clear', function () {
                        if (that.search() !== this.value) {
                            that.search(this.value).draw();
                        }
                    });
                });
        },
        select: { /* Para os checkbox o JQuery exemplo está no "Index.cshtml" e corresponde ao "_PartialLancarExames.cshtml"  */
            selector: 'td, input.noSelectedLinha:not(:has(:checkbox))',
        },
        fixedColumns: {
            left: -1,  //não permite fixar a primeira coluna
            right: 1,  //fixa a última coluna dos ícones de ações/botões de ações
        },
        autoWidth: true,
        responsive: true,
        paging: false,           //para scrollY
        scrollCollapse: true,    //para scrollY
        scrollY: '50vh',         //para scrollY  //total de umas 17 linhas no grid da tabela
        scrollX: false,
        layout: {
            topStart: {
                //    search: {
                //        placeholder: 'Digite o exame e tecle <ENTER>'
                //    }
            },
            topEnd: { /* obrigatório ter mesmo sem utilizar, porque desformata o Search */
                //pageLength: {
                //    menu: [[10, 15, 20, -1], [10, 15, 20, "Todos"]],
                //}
            },
        },
        "language": {
            "search": "Selecione o Exame para do Cupom e tecle [ENTER]",
            "lengthMenu": "Mostrar _MENU_ resultados por página",
            "emptyTable": "<b style='color: red;'>Não haviam dados disponíveis para esta consulta</b>",
            "info": "Ao imprimir o Cupom, tudo será salvo!",
            "infoFiltered": "(Total de _MAX_ registros existentes na base de dados)",
            "infoThousands": ".",
            "thousands": ".",
            "decimal": ",",
            "loadingRecords": "Carregando...",
            "zeroRecords": "<b style='color: red;'>A consulta não retornou dados para o Grid</b>",
            "paginate": {
                "next": "<img src='images/icones/arrow_right_uno.png' width='28%' border='none' />",
                "previous": "<img src='images/icones/arrow_left_uno.png' width='28%' border='none' />",
                "first": "<img src='images/icones/arrow_left.png' width='28%' border='none' />",
                "last": "<img src='images/icones/arrow_right.png' width='28%' border='none' />",
            },
            //Fixa o header e o footer para auxiliar na identificação das colunas
            fixedHeader: {
                header: true,
                footer: true
            }
        }
    }).draw();
}
//..


/* MODELO COMPLETO DE CONFIGURAÇÃO PARA APOIO */

//      "language":
//      {
//      "emptyTable": "Nenhum registro encontrado",
//      "info": "Mostrando de _START_ até _END_ de _TOTAL_ registros",
//      "infoFiltered": "(Filtrados de _MAX_ registros)",
//      "infoThousands": ".",
//      "loadingRecords": "Carregando...",
//      "zeroRecords": "Nenhum registro encontrado",
//      "search": "Pesquisar",
//      "paginate": {
//      "next": "Próximo",
//      "previous": "Anterior",
//      "first": "Primeiro",
//      "last": "Último"
//      },
//      "aria": {
//      "sortAscending": ": Ordenar colunas de forma ascendente",
//      "sortDescending": ": Ordenar colunas de forma descendente"
//      },
//      "select": {
//      "rows": {
//      "_": "Selecionado %d linhas",
//      "1": "Selecionado 1 linha"
//      },
//      "cells": {
//      "1": "1 célula selecionada",
//      "_": "%d células selecionadas"
//      },
//      "columns": {
//      "1": "1 coluna selecionada",
//      "_": "%d colunas selecionadas"
//      }
//      },
//      columnDefs: [
//      {   /* alinhando os textos da colunas à esquerda dt-body-left, dt-body-center, dt-body-right (tem que ter a mesma quantidade de colunas da table) */
//      targets: [0, 1, 2, 3],
//      className: 'dt-body-left'
//      }
//      ],
//      "buttons": {
//      "copySuccess": {
//      "1": "Uma linha copiada com sucesso",
//      "_": "%d linhas copiadas com sucesso"
//      },
//      "collection": "Coleção  <span class=\"ui-button-icon-primary ui-icon ui-icon-triangle-1-s\"> <\ /span>
//      ",
//      "colvis": "Visibilidade da Coluna",
//      "colvisRestore": "Restaurar Visibilidade",
//      "copy": "Copiar",
//      "copyKeys": "Pressione ctrl ou u2318 + C para copiar os dados da tabela para a área de transferência do sistema. Para cancelar, clique nesta mensagem ou pressione Esc..",
//      "copyTitle": "Copiar para a Área de Transferência",
//      "csv": "CSV",
//      "excel": "Excel",
//      "pageLength": {
//      "-1": "Mostrar todos os registros",
//      "_": "Mostrar %d registros"
//      },
//      "pdf": "PDF",
//      "print": "Imprimir",
//      "createState": "Criar estado",
//      "removeAllStates": "Remover todos os estados",
//      "removeState": "Remover",
//      "renameState": "Renomear",
//      "savedStates": "Estados salvos",
//      "stateRestore": "Estado %d",
//      "updateState": "Atualizar"
//      },
//      "autoFill": {
//      "cancel": "Cancelar",
//      "fill": "Preencher todas as células com",
//      "fillHorizontal": "Preencher células horizontalmente",
//      "fillVertical": "Preencher células verticalmente"
//      },
//      "lengthMenu": "Exibir _MENU_ resultados por página",
//      "searchBuilder": {
//      "add": "Adicionar Condição",
//      "button": {
//      "0": "Construtor de Pesquisa",
//      "_": "Construtor de Pesquisa (%d)"
//      },
//      "clearAll": "Limpar Tudo",
//      "condition": "Condição",
//      "conditions": {
//      "date": {
//      "after": "Depois",
//      "before": "Antes",
//      "between": "Entre",
//      "empty": "Vazio",
//      "equals": "Igual",
//      "not": "Não",
//      "notBetween": "Não Entre",
//      "notEmpty": "Não Vazio"
//      },
//      "number": {
//      "between": "Entre",
//      "empty": "Vazio",
//      "equals": "Igual",
//      "gt": "Maior Que",
//      "gte": "Maior ou Igual a",
//      "lt": "Menor Que",
//      "lte": "Menor ou Igual a",
//      "not": "Não",
//      "notBetween": "Não Entre",
//      "notEmpty": "Não Vazio"
//      },
//      "string": {
//      "contains": "Contém",
//      "empty": "Vazio",
//      "endsWith": "Termina Com",
//      "equals": "Igual",
//      "not": "Não",
//      "notEmpty": "Não Vazio",
//      "startsWith": "Começa Com",
//      "notContains": "Não contém",
//      "notStartsWith": "Não começa com",
//      "notEndsWith": "Não termina com"
//      },
//      "array": {
//      "contains": "Contém",
//      "empty": "Vazio",
//      "equals": "Igual à",
//      "not": "Não",
//      "notEmpty": "Não vazio",
//      "without": "Não possui"
//      }
//      },
//      "data": "Data",
//      "deleteTitle": "Excluir regra de filtragem",
//      "logicAnd": "E",
//      "logicOr": "Ou",
//      "title": {
//      "0": "Construtor de Pesquisa",
//      "_": "Construtor de Pesquisa (%d)"
//      },
//      "value": "Valor",
//      "leftTitle": "Critérios Externos",
//      "rightTitle": "Critérios Internos"
//      },
//      "searchPanes": {
//      "clearMessage": "Limpar Tudo",
//      "collapse": {
//      "0": "Painéis de Pesquisa",
//      "_": "Painéis de Pesquisa (%d)"
//      },
//      "count": "{total}",
//      "countFiltered": "{shown} ({total})",
//      "emptyPanes": "Nenhum Painel de Pesquisa",
//      "loadMessage": "Carregando Painéis de Pesquisa...",
//      "title": "Filtros Ativos",
//      "showMessage": "Mostrar todos",
//      "collapseMessage": "Fechar todos"
//      },
//      "thousands": ".",
//      "datetime": {
//      "previous": "Anterior",
//      "next": "Próximo",
//      "hours": "Hora",
//      "minutes": "Minuto",
//      "seconds": "Segundo",
//      "amPm": [
//      "am",
//      "pm"
//      ],
//      "unknown": "-",
//      "months": {
//      "0": "Janeiro",
//      "1": "Fevereiro",
//      "10": "Novembro",
//      "11": "Dezembro",
//      "2": "Março",
//      "3": "Abril",
//      "4": "Maio",
//      "5": "Junho",
//      "6": "Julho",
//      "7": "Agosto",
//      "8": "Setembro",
//      "9": "Outubro"
//      },
//      "weekdays": [
//      "Dom",
//      "Seg",
//      "Ter",
//      "Qua",
//      "Qui",
//      "Sex",
//      "Sáb"
//      ]
//      },
//      "editor": {
//      "close": "Fechar",
//      "create": {
//      "button": "Novo",
//      "submit": "Criar",
//      "title": "Criar novo registro"
//      },
//      "edit": {
//      "button": "Editar",
//      "submit": "Atualizar",
//      "title": "Editar registro"
//      },
//      "error": {
//      "system": "Ocorreu um erro no sistema (<a target=\"\\\" rel=\"nofollow\" href=\"\\\">
//      Mais informações <\ /a>
//      )."
//      },
//      "multi": {
//      "noMulti": "Essa entrada pode ser editada individualmente, mas não como parte do grupo",
//      "restore": "Desfazer alterações",
//      "title": "Multiplos valores",
//      "info": "Os itens selecionados contêm valores diferentes para esta entrada. Para editar e definir todos os itens para esta entrada com o mesmo valor, clique ou toque aqui, caso contrário, eles manterão seus valores individuais."
//      },
//      "remove": {
//      "button": "Remover",
//      "confirm": {
//      "_": "Tem certeza que quer deletar %d linhas?",
//      "1": "Tem certeza que quer deletar 1 linha?"
//      },
//      "submit": "Remover",
//      "title": "Remover registro"
//      }
//      },
//      "decimal": ",",
//      "stateRestore": {
//      "creationModal": {
//      "button": "Criar",
//      "columns": {
//      "search": "Busca de colunas",
//      "visible": "Visibilidade da coluna"
//      },
//      "name": "Nome:",
//      "order": "Ordernar",
//      "paging": "Paginação",
//      "scroller": "Posição da barra de rolagem",
//      "search": "Busca",
//      "searchBuilder": "Mecanismo de busca",
//      "select": "Selecionar",
//      "title": "Criar novo estado",
//      "toggleLabel": "Inclui:"
//      },
//      "emptyStates": "Nenhum estado salvo",
//      "removeConfirm": "Confirma remover %s?",
//      "removeJoiner": "e",
//      "removeSubmit": "Remover",
//      "removeTitle": "Remover estado",
//      "renameButton": "Renomear",
//      "renameLabel": "Novo nome para %s:",
//      "renameTitle": "Renomear estado",
//      "duplicateError": "Já existe um estado com esse nome!",
//      "emptyError": "Não pode ser vazio!",
//      "removeError": "Falha ao remover estado!"
//      },
//      "infoEmpty": "Mostrando 0 até 0 de 0 registro(s)",
//      "processing": "Carregando...",
//      "searchPlaceholder": "Buscar registros"
//      }


