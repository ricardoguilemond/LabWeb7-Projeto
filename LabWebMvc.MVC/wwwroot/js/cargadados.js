let connection = null;
let chaveImportacao = null;
let pollingInterval = null;

async function conectarSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/importProgress")
        .withAutomaticReconnect()
        .build();

    connection.on("ReceberProgresso", function (progresso) {
        atualizarProgresso(progresso);
    });

    connection.on("ReceberErro", function (erro) {
        mostrarErro(erro);
    });

    connection.on("ReceberConclusao", function (resultado) {
        mostrarResultado(resultado);
    });

    connection.on("RequererDecisao", function (decisao) {
        mostrarDecisao(decisao);
    });

    try {
        await connection.start();
    } catch (err) {
        console.warn("SignalR não disponível, usando polling.", err);
    }
}

async function iniciarImportacao() {
    await conectarSignalR();

    document.getElementById("btn-iniciar").style.display = "none";
    document.getElementById("btn-cancelar").style.display = "inline-block";
    document.getElementById("area-progresso").style.display = "block";
    document.getElementById("area-resultado").style.display = "none";
    document.getElementById("area-decisao").style.display = "none";
    document.getElementById("status-spinner").style.display = "inline-block";
    document.getElementById("status-texto").textContent = "Conectando ao servidor...";

    const connectionId = connection ? connection.connectionId : "";

    const response = await fetch("/CargaDados/Importar", {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded"
        },
        body: "connectionId=" + encodeURIComponent(connectionId)
    });

    const data = await response.json();

    if (!data.sucesso) {
        mostrarErro({ tabela: "Inicialização", erro: data.mensagem });
        return;
    }

    chaveImportacao = data.chave;

    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        iniciarPolling();
    }
}

function iniciarPolling() {
    if (pollingInterval) clearInterval(pollingInterval);

    pollingInterval = setInterval(async function () {
        if (!chaveImportacao) return;

        const response = await fetch("/CargaDados/Status?chave=" + encodeURIComponent(chaveImportacao));
        const data = await response.json();

        if (!data.sucesso) {
            clearInterval(pollingInterval);
            return;
        }

        if (data.progresso) {
            atualizarProgresso(data.progresso);
        }

        if (data.aguardandoDecisao) {
            mostrarDecisao({ tabela: data.tabelaComErro, detalhe: data.detalheErro });
        }

        if (data.concluido || data.erro) {
            clearInterval(pollingInterval);
            if (data.resultado) {
                mostrarResultado(data.resultado);
            } else if (data.mensagemErro) {
                mostrarErro({ tabela: data.tabelaComErro || "Importação", erro: data.mensagemErro });
            }
        }
    }, 1000);
}

function atualizarProgresso(progresso) {
    const barra = document.getElementById("barra-progresso");
    const porcentagem = Math.min(100, Math.max(0, progresso.porcentagemTotal || 0));

    barra.style.width = porcentagem + "%";
    barra.textContent = porcentagem + "%";

    document.getElementById("tabela-atual").textContent = progresso.tabelaAtual || "-";
    document.getElementById("status-texto").textContent = progresso.status || "Processando...";

    const faseAtual = document.getElementById("fase-atual");
    if (faseAtual) {
        faseAtual.textContent = progresso.fase || "-";
        faseAtual.className = "badge " + (progresso.fase === "Limpeza" ? "bg-warning text-dark" : "bg-success");
    }

    const spinner = document.getElementById("status-spinner");
    if (spinner) {
        spinner.style.display = (progresso.emExecucao || progresso.porcentagemTotal < 100) ? "inline-block" : "none";
    }
    document.getElementById("registros-texto").textContent =
        (progresso.registrosProcessados || 0).toLocaleString("pt-BR") +
        " / " +
        (progresso.totalRegistros || 0).toLocaleString("pt-BR") +
        " registros";
}

function mostrarErro(erro) {
    document.getElementById("btn-iniciar").style.display = "inline-block";
    document.getElementById("btn-cancelar").style.display = "none";
    document.getElementById("status-spinner").style.display = "none";

    const area = document.getElementById("area-resultado");
    area.style.display = "block";
    area.className = "alert alert-danger";
    area.innerHTML = "<strong>Erro em " + (erro.tabela || "Importação") + ":</strong> " + (erro.erro || erro.mensagem || "Erro desconhecido");
}

function mostrarDecisao(decisao) {
    document.getElementById("area-decisao").style.display = "block";
    document.getElementById("decisao-mensagem").textContent =
        "Ocorreu um erro na tabela " + (decisao.tabela || "atual") + ". " + (decisao.detalhe || "Deseja ignorar e prosseguir?");
}

async function definirDecisao(ignorar) {
    if (!chaveImportacao) return;

    await fetch("/CargaDados/Decisao", {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded"
        },
        body: "chave=" + encodeURIComponent(chaveImportacao) + "&ignorar=" + ignorar
    });

    document.getElementById("area-decisao").style.display = "none";
}

function cancelarImportacao() {
    definirDecisao(false);
    document.getElementById("btn-iniciar").style.display = "inline-block";
    document.getElementById("btn-cancelar").style.display = "none";
}

function mostrarResultado(resultado) {
    document.getElementById("btn-iniciar").style.display = "inline-block";
    document.getElementById("btn-cancelar").style.display = "none";
    document.getElementById("barra-progresso").classList.remove("progress-bar-animated");
    document.getElementById("status-spinner").style.display = "none";

    const area = document.getElementById("area-resultado");
    area.style.display = "block";
    area.className = "alert alert-success";

    let html = "<h5><i class='fa-solid fa-check-circle'></i> " + (resultado.mensagemFinal || "Concluído") + "</h5>";

    if (resultado.resultados && resultado.resultados.length > 0) {
        html += "<table class='table table-sm table-bordered mt-2'><thead><tr>" +
            "<th>Tabela</th><th>Lidos</th><th>Inseridos</th><th>Erros</th><th>Tempo</th><th>Observação</th>" +
            "</tr></thead><tbody>";

        resultado.resultados.forEach(function (r) {
            html += "<tr>" +
                "<td>" + (r.nomePostgreSQL || r.nomeFirebird) + "</td>" +
                "<td>" + (r.totalLido || 0).toLocaleString("pt-BR") + "</td>" +
                "<td>" + (r.inseridos || 0).toLocaleString("pt-BR") + "</td>" +
                "<td>" + (r.erros || 0).toLocaleString("pt-BR") + "</td>" +
                "<td>" + formatarTempo(r.tempoGasto) + "</td>" +
                "<td>" + (r.observacao || "") + "</td>" +
                "</tr>";
        });

        html += "</tbody></table>";
        html += "<p><strong>Tempo total:</strong> " + formatarTempo(resultado.tempoTotal) + "</p>";
    }

    area.innerHTML = html;
}

function formatarTempo(tempo) {
    if (!tempo) return "-";
    const h = Math.floor(tempo / 3600).toString().padStart(2, "0");
    const m = Math.floor((tempo % 3600) / 60).toString().padStart(2, "0");
    const s = Math.floor(tempo % 60).toString().padStart(2, "0");
    return h + ":" + m + ":" + s;
}
