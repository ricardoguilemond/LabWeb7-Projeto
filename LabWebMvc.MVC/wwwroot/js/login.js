// Remove o Loading na página de Login
document.addEventListener("DOMContentLoaded", function () {
    var modalLoading = document.getElementById("modalLoading");
    if (modalLoading) {
        document.body.removeChild(modalLoading);
        loading.style.display = "none";
    }
});

// Toggle de exibição da senha e fade-out da mensagem de erro
document.addEventListener("DOMContentLoaded", function () {
    var spnMostrarSenha = document.getElementById("spnMostrarSenha");
    var idSenha = document.getElementById("idSenha");

    spnMostrarSenha.addEventListener("click", function () {
        if (idSenha.type === "password") {
            idSenha.type = "text";
        } else {
            idSenha.type = "password";
        }
    });

    // Esconder mensagem de erro após 7 segundos
    const mensagem = document.getElementById("mensagemErroLogin");
    if (mensagem) {
        setTimeout(() => {
            mensagem.style.transition = "opacity 1s ease";
            mensagem.style.opacity = "0";
            setTimeout(() => {
                mensagem.style.display = "none";
            }, 1000); // espera o fade-out terminar
        }, 7000); // espera 7 segundos antes de iniciar o fade-out
    }
});
