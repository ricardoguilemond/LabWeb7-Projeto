using LabWebMvc.MVC.ViewModel;

namespace LabWebMvc.MVC.Areas.Validations
{
    public interface IValidacoesDeSenhas
    {
        void RecuperaLogin(ref vmSenhas objLogin);

        Task<string> CriaUsuarioSenha(vmSenhas obj, int adm = 0);

        Task<vmSenhas>? RetornaValidacaoLogin(vmLogin? vm);

        vmSenhas? RetornaLogin(string loginUsuario, string senhaUsuario);

        vmSenhas? RetornaLogin(string loginUsuario, string cpf, DateTime nascimento);
    }
}