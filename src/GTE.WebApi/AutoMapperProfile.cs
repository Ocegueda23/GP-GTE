using AutoMapper;

namespace GTE.WebApi;

/// <summary>
/// Perfil unico de AutoMapper (patron del ecosistema: el perfil vive en WebApi).
/// Los mapeos entidad-DTO se agregan aqui por feature.
/// </summary>
public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Los mapeos por feature se registran conforme se construyen los modulos.
    }
}
