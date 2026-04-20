// Dashboard toggle e ShowProgress
var card = document.createElement("div");
//..
(function () {
    try {
        document.addEventListener("DOMContentLoaded", function () {
            var div = document.getElementById("divEscondeMostra");
            var visible = localStorage.getItem("dashboardVisible");

            if (div) {
                if (visible === "false") {
                    div.classList.add("hidden");
                } else {
                    div.classList.remove("hidden");
                }
            }
        });
    } catch (e) {
        console.warn("Erro ao acessar localStorage:", e);
    }
})();

function func_Dashboard() {
    var div = document.getElementById("divEscondeMostra");
    if (!div) return;

    var isHidden = div.classList.contains("hidden");

    if (isHidden) {
        div.classList.remove("hidden");
        div.classList.add("fading-in");
        void div.offsetWidth;
        div.style.transition = "opacity 0.5s ease-in";
        div.style.opacity = "1";
        setTimeout(function () {
            div.classList.remove("fading-in");
        }, 500);
        localStorage.setItem("dashboardVisible", true);
    } else {
        div.classList.add("hidden");
        div.style.transition = "none";
        localStorage.setItem("dashboardVisible", false);
    }
}
//..
var modalLoading, loading;
function ShowProgress() {
    modalLoading = document.createElement("DIV");
    modalLoading.className = "modalLoading";
    document.body.appendChild(modalLoading);
    loading = document.getElementsByClassName("loading")[0];
    loading.style.display = "block";
    var top = Math.max(window.innerHeight / 2 - loading.offsetHeight / 2, 0);
    var left = Math.max(window.innerWidth / 2 - loading.offsetWidth / 2, 0);
    loading.style.top = top + "px";
    loading.style.left = left + "px";
}
ShowProgress();
//..

// ENCERRA O LOADING
window.addEventListener('load', function () {
    setTimeout(function () {
        if (modalLoading && modalLoading.parentNode) {
            document.body.removeChild(modalLoading);
        }
        if (loading) {
            loading.style.display = "none";
        }
    }, 1000);
    setTimeout(function () {
        if (modalLoading) {
            modalLoading.style.pointerEvents = "none";
            modalLoading.style.display = "none";
        }
    }, 2000);
});
